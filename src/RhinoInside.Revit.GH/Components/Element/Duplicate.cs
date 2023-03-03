using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using ElementTracking;
  using External.DB.Extensions;
  using Convert.Geometry;

  public class ElementDuplicate : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F4C12AA0-A87B-4209-BD7B-4A189E4F4F0E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;

    public ElementDuplicate() : base
    (
      name: "Duplicate Element",
      nickname: "Duplicate",
      description: "Duplicates document elements",
      category: "Revit",
      subCategory: "Element"
    )
    {
      TrackingMode = TrackingMode.Supersede;
    }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Destination document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Source elements",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Source elements",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = _Duplicates_,
          NickName = _Duplicates_.Substring(0, 1),
          Description = "Duplicate elements",
          Access = GH_ParamAccess.list,
        }
      ),
    };

    const string _Duplicates_ = "Duplicates";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      if (!Params.TryGetDataList(DA, "Elements", out IList<Types.Element> elements)) return;
      else Params.TrySetDataList(DA, "Elements", () => elements);

      StartTransaction(doc.Value);
      {
        var tracked    = new bool[elements.Count];
        var duplicates = new Types.Element[elements.Count];

        // Xlate Invalid elements -> None
        var None = new Types.Element();
        for (int e = 0; e < duplicates.Length; e++)
        {
          if (elements[e]?.IsValid == false)
            duplicates[e] = None;
        }

        var documents = elements.
          Select((element, index) => (index, element)).
          Where(x => x.element?.IsValid == true).
          GroupBy(x => x.element.Document);

        foreach (var document in documents)
        {
          var sourceBuiltIn = new List<(int index, Types.Element element)>();
          var sourceSystem = new List<(int index, Types.Element element)>();
          var sourceUser = new List<(int index, Types.Element element)>();

          // Classify source items.
          foreach (var item in document)
          {
            if (item.element.Id.IsBuiltInId()) sourceBuiltIn.Add(item);
            else if (item.element.Value is ARDB.Family family && !family.IsUserCreated) sourceSystem.Add(item);
            else if (item.element.Value is ARDB.FamilySymbol symbol && !symbol.Family.IsUserCreated) sourceSystem.Add(item);
            else sourceUser.Add(item);
          }

          // Xlate BuiltIn ids
          {
            foreach (var sourceElement in sourceBuiltIn)
            {
              var element = Types.Element.FromElementId(doc.Value, sourceElement.element.Id);
              duplicates[sourceElement.index] = element;
            }
          }

          // Xlate System ids
          {
            foreach (var sourceElement in sourceSystem)
            {
              var element = Types.Element.FromElementId(doc.Value, doc.Value.LookupElement(sourceElement.element.Document, sourceElement.element.Id));
              duplicates[sourceElement.index] = element;
            }
          }

          // Xlate User ids
          if (sourceUser.Count > 0)
          {
            // Create a map with unique elements to recover results of CopyElements in the input order
            var map = new SortedList<ARDB.ElementId, (string name, List<int> twins)>
            (
              sourceUser.Count, ElementIdComparer.Ascending
            );

            foreach (var (index, element) in sourceUser)
            {
              if (!map.TryGetValue(element.Id, out var entry))
                map.Add(element.Id, entry = (element.Nomen, new List<int>()));

              entry.twins.Add(index);
            }

            using (var options = new ARDB.CopyPasteOptions())
            {
              options.SetDuplicateTypeNamesAction(ARDB.DuplicateTypeAction.UseDestinationTypes);

              // Duplicate elements
              var copiedElements = ARDB.ElementTransformUtils.CopyElements
              (
                document.Key,
                map.Keys,
                doc.Value,
                default,
                options
              );

              foreach (var copiedElement in copiedElements.Zip(map, (Id, source) => (Id, source)))
              {
                var element = Types.Element.FromElementId(doc.Value, copiedElement.Id);

                if
                (
                  // element.CanBeRenamed() && // More precise but slow.
                  ElementExtension.GetNomenParameter(element.Value.GetType()) != ARDB.BuiltInParameter.INVALID &&
                  element.Nomen == copiedElement.source.Value.name
                )
                {
                  try
                  {
                    element.SetIncrementalNomen(copiedElement.source.Value.name);
                    AddRuntimeMessage
                    (
                      GH_RuntimeMessageLevel.Warning,
                      $"{(element as Grasshopper.Kernel.Types.IGH_Goo).TypeName} \"{copiedElement.source.Value.name}\" has been renamed to \"{element.Nomen}\" to avoid conflicts with the existing Element. {{{element.Id}}}"
                    );
                  }
                  catch (Exceptions.RuntimeArgumentException) { /* Invalid characters in the original name use to be view {3D} or <Building> */ }
                  catch (Autodesk.Revit.Exceptions.InvalidOperationException) { /* Some elements can't be renamed */ }
                }

                // Populate duplicates Stream for the next iteration with unique duplicates
                foreach (var index in copiedElement.source.Value.twins)
                {
                  tracked[index] = true;
                  duplicates[index] = element;
                }
              }
            }
          }
        }

        for (int i = 0; i < duplicates.Length; ++i)
          Params.WriteTrackedElement(_Duplicates_, doc.Value, tracked[i] ? duplicates[i] : None);

        DA.SetDataList(_Duplicates_, duplicates);
      }
    }
  }
}
