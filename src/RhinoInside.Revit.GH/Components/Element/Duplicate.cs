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
      else Params.TrySetDataList(DA, "Elements", () => elements);

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

            foreach (var (index, element) in sourceNonBuiltIn)
            {
              if (!map.TryGetValue(element.Id, out var entry))
                map.Add(element.Id, entry = (element.Nomen, new List<int>()));

              entry.twins.Add(index);
            }

            using (var options = new ARDB.CopyPasteOptions())
            {
              options.SetDuplicateTypeNamesHandler(default(DuplicateTypeNamesHandler));

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
                  ElementExtension.GetNomenParameter(element.GetType()) != ARDB.BuiltInParameter.INVALID &&
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
                  catch (ArgumentException) { /* Invalid characters in the original name use to be view {3D} */ }
                }

                // Populate duplicates Stream for the next iteration with unique duplicates
                foreach (var index in copiedElement.source.Value.twins)
                  duplicates[index] = element;
              }
            }
          }
        }

        for (int i = 0; i < duplicates.Length; ++i)
          Params.WriteTrackedElement(_Duplicates_, doc.Value, duplicates[i]);

        DA.SetDataList(_Duplicates_, duplicates);
      }
    }

    struct DuplicateTypeNamesHandler : ARDB.IDuplicateTypeNamesHandler
    {
      public ARDB.DuplicateTypeAction OnDuplicateTypeNamesFound(ARDB.DuplicateTypeNamesHandlerArgs args) =>
        ARDB.DuplicateTypeAction.UseDestinationTypes;
    }
  }

  [ComponentVersion(introduced: "1.6")]
  public class ElementClone : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("0EA8D61A-5FED-471D-A69D-B695DFBA5581");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public ElementClone() : base
    (
      name: "Clone Element",
      nickname: "Clone",
      description: "Clone document element on several locations",
      category: "Revit",
      subCategory: "Element"
    )
    { }

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
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to duplicate",
        }
      ),
      new ParamDefinition
      (
        new Param_Plane
        {
          Name = "Location",
          NickName = "L",
          Description = "Location to place the new element. Point and plane are accepted",
          Access = GH_ParamAccess.list,
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "Target View",
        },
        ParamRelevance.Secondary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _Clones_,
          NickName = _Clones_.Substring(0, 1),
          Access = GH_ParamAccess.list,
        }
      ),
    };

    const string _Clones_ = "Clones";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      if (!Params.GetDataList(DA, "Location", out IList<Plane?> locations) || locations is null) return;
      if (!Params.TryGetData(DA, "View", out Types.View view)) return;

      var clones = new List<Types.GraphicalElement>(locations.Count);
      foreach (var location in locations)
      {
        var clone = Types.GraphicalElement.FromElement
        (
          ReconstructElement<ARDB.Element>
          (
            doc.Value, _Clones_,
            x =>
            {
              if (location.HasValue && location.Value.IsValid)
              {
                if
                (
                  x?.GetType()   == element.Value.GetType() &&
                  x.Category.Id  == element.Category.Id &&
                  x.ViewSpecific == element.ViewSpecific &&
                  x.OwnerViewId == (view?.Id ?? element.Value.OwnerViewId)
                )
                {
                  if (x.GetTypeId() != element.Type.Id)
                    x = x.Document.GetElement(x.ChangeTypeId(element.Type.Id)) ?? x;

                  x.CopyParametersFrom(element.Value, ExcludeUniqueProperties);
                  return x;
                }

                if (view?.IsValid == true && element.ViewSpecific != true)
                {
                  switch (FailureProcessingMode)
                  {
                    case ARDB.FailureProcessingResult.Continue:
                      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Cannot clone the specified element into '{view.Nomen}' view. {{{element.Id}}}");
                      return null;
                    case ARDB.FailureProcessingResult.ProceedWithCommit:
                      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Cannot paste the specified element into '{view.Nomen}' view. {{{element.Id}}}");
                      break;
                    case ARDB.FailureProcessingResult.WaitForUserInput:
                      using (var failure = new ARDB.FailureMessage(ARDB.BuiltInFailures.CopyPasteFailures.CannotPasteInView))
                      {
                        failure.SetFailingElement(view.Id);
                        failure.SetAdditionalElement(element.Id);
                        doc.Value.PostFailure(failure);
                      }
                      return null;

                    default: throw new Exceptions.RuntimeException();
                  }
                }

                return view is object && element.ViewSpecific == true ?
                  element.Value.CloneElement(view.Value) :
                  element.Value.CloneElement(doc.Value);
              }

              return default;
            }
          )
        ) as Types.GraphicalElement;

        if (location.HasValue && location.Value.IsValid)
        {
          if (clone is object && !clone.Location.EpsilonEquals(location.Value, GeometryObjectTolerance.Model.VertexTolerance))
          {
            using ((clone as Types.InstanceElement)?.DisableJoinsScope())
            {
              var pinned = clone.Pinned;
              clone.Pinned = false;
              clone.Location = location.Value;
              clone.Pinned = pinned;
            }
          }
        }

        clones.Add(clone);
      }
      DA.SetDataList(_Clones_, clones);
    }
  }
}
