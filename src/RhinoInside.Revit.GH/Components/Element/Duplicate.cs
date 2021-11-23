using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using ElementTracking;
  using External.DB.Extensions;

  public class ElementDuplicate : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("F4C12AA0-A87B-4209-BD7B-4A189E4F4F0E");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "D";

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
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Elements",
          NickName = "E",
          Description = "Elements to Duplicate",
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
        }
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

      StartTransaction(doc.Value);
      {
        var duplicates = new Types.Element[elements.Count];

        var input = new (int index, Types.Element element)[elements.Count];
        for (int e = 0; e < elements.Count; ++e)
          input[e] = (e, elements[e]);

        var documents = input.Where(x => x.element?.IsValid == true).GroupBy(x => x.element.Document);

        foreach (var document in documents)
        {
          var sourceBuiltIn = new List<(int index, Types.Element element)>();
          var sourceNonBuiltIn = new List<(int index, Types.Element element)>();

          // Classify source entries in two lists, builtin and non builtin elements.
          foreach (var entry in document)
          {
            if (entry.element.Id.IsBuiltInId()) sourceBuiltIn.Add(entry);
            else                                sourceNonBuiltIn.Add(entry);
          }

          // Xlate BuiltIn ids
          {
            foreach (var copiedElement in sourceBuiltIn)
            {
              var element = Types.Element.FromElementId(doc.Value, copiedElement.element.Id);
              duplicates[copiedElement.index] = element;
            }
          }

          // Xlate non BuiltIn ids
          if (sourceNonBuiltIn.Count > 0)
          {
            // Create a map with unique elements to recover results of CopyElements in the correct order
            var map = new SortedList<ARDB.ElementId, (string name, List<int> twins)>
            (
              sourceNonBuiltIn.Count, ElementIdComparer.Ascending
            );

            foreach (var sourceElement in sourceNonBuiltIn)
            {
              if (!map.TryGetValue(sourceElement.element.Id, out var entry))
                map.Add(sourceElement.element.Id, entry = (sourceElement.element.Name, new List<int>()));

              entry.twins.Add(sourceElement.index);
            }

            // Duplicate elements
            var copiedElements = ARDB.ElementTransformUtils.CopyElements
            (
              document.Key,
              map.Keys,
              doc.Value,
              default,
              default
            );

            foreach (var copiedElement in copiedElements.Zip(map, (Id, source) => (Id, source)))
            {
              var element = Types.Element.FromElementId(doc.Value, copiedElement.Id);

              try { element.SetIncrementalName(copiedElement.source.Value.name); }
              catch (ArgumentException) { /* Invalid characters in the original name use to be view {3D} */ }

              // Populate duplicates Stream for the next iteration with unique duplicates
              foreach (var index in copiedElement.source.Value.twins)
                duplicates[index] = element;
            }
          }
        }

        Params.TrySetDataList(DA, "Elements", () => elements);

        for (int i = 0; i < duplicates.Length; ++i)
          Params.WriteTrackedElement(_Duplicates_, doc.Value, duplicates[i]);

        DA.SetDataList(_Duplicates_, duplicates);
      }
    }
  }
}
