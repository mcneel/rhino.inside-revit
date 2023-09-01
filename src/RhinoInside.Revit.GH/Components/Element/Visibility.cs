using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  [ComponentVersion(introduced: "1.13", updated: "1.16")]
  public class ElementVisibility : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("8ED1490F-DA5D-40FA-8612-4F4B166ECE52");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public ElementVisibility() : base
    (
      name: "Element Visibility",
      nickname: "Visibility",
      description: "Check element visibility on a given View",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Visibility status",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View where to check element visibility",
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
          Name = "Element",
          NickName = "E",
          Description = "Element to check visibility status",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Visible",
          NickName = "V",
          Description = "Element visibility status",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View where the element visibility has been checked",
        }, ParamRelevance.Secondary
      ),

    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Element", out IList<Types.Element> elements)) return;
      else Params.TrySetDataList(DA, "Element", () => elements);

      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      Params.TrySetDataList
      (
        DA, "Visible", () =>
        {
          var visible = new bool[elements.Count];

          var viewValue = view.Value;
          if (!viewValue.IsTemplate)
          {
            var viewDocument = view.Document;

            // TODO : Test this, it may reduce the Outline.
            //using (viewDocument.NoSelectionScope())
            {
              // Build a list of ids that belong to `viewDocument`
              var ids = new HashSet<ARDB.ElementId>(elements.Count);
              {
                foreach (var element in elements)
                {
                  if (!viewDocument.IsEquivalent(element?.Document)) continue;
                  if (element?.Value is ARDB.Element elementValue)
                    ids.Add(element.Id);
                }
              }

              var visibleElements = GetVisibleElements(viewValue, ids);
              if (visibleElements.Count > 0)
              {
                for (int i = 0; i < elements.Count; i++)
                {
                  if (elements[i] is null) continue;
                  visible[i] = visibleElements.Contains(elements[i].Id);
                }
              }

              if (viewValue is ARDB.ViewSheet viewSheet)
              {
                foreach (var placedView in viewSheet.GetAllPlacedViews().Select(x => viewDocument.GetElement(x) as ARDB.View))
                {
                  ids.ExceptWith(visibleElements);

                  visibleElements = GetVisibleElements(placedView, ids);
                  if (visibleElements.Count > 0)
                  {
                    for (int i = 0; i < elements.Count; i++)
                    {
                      if (elements[i] is null) continue;
                      visible[i] |= visibleElements.Contains(elements[i].Id);
                    }
                  }
                }
              }
            }
          }

          return visible;
        }
      );
    }

    static ISet<ARDB.ElementId> GetVisibleElements(ARDB.View viewValue, ICollection<ARDB.ElementId> ids)
    {
      if (ids.Count > 0 && !viewValue.IsTemplate)
      {
        var viewDocument = viewValue.Document;
        var viewId = viewValue.Id;

        if (viewValue.GetClipFilter(clipped: false) is ARDB.ElementFilter viewFilter)
        {
          using (var collector = new ARDB.FilteredElementCollector(viewDocument, ids))
            ids = collector.WherePasses(viewFilter).ToElementIds();
        }

        if (ids.Count > 0)
        {
          var documentIsWorkshared = viewDocument.IsWorkshared;
          var modelClipBox = viewValue.GetModelClipBox();
          var isModelClipped = modelClipBox.GetPlaneEquations(out var modelClipPlanes, Numerical.Tolerance.Default);
          var annotationClipBox = viewValue.GetAnnotationClipBox();
          var isAnnotationClipped = annotationClipBox.GetPlaneEquations(out var annotationClipPlanes, Numerical.Tolerance.Default);
          var areViewGraphicsOverridesAllowed = viewValue.AreGraphicsOverridesAllowed();
          var isCategoryTypeHidden = new bool[]
          {
            true,
            viewValue.AreModelCategoriesHidden,
            viewValue.AreAnnotationCategoriesHidden,
            false, // ??
            false, // ??
            viewValue.AreAnalyticalModelCategoriesHidden,
            viewValue.AreImportCategoriesHidden,
            viewValue.ArePointCloudsHidden
          };

          var visibleIds = new List<ARDB.ElementId>(ids.Count);
          var viewPhaseFilter = ((viewValue.get_Parameter(ARDB.BuiltInParameter.VIEW_PHASE_FILTER)?.AsElement()) as ARDB.PhaseFilter);
          var viewPhase = viewValue.get_Parameter(ARDB.BuiltInParameter.VIEW_PHASE)?.AsElementId() ?? ElementIdExtension.Invalid;

          using (var viewVisibilityFilter = GetElementVisibilityFilter(viewValue, hidden: true))
          {
            foreach (var elementValue in ids.Select(viewDocument.GetElement))
            {
              var elementCategory = elementValue.Category;
              if (elementCategory is null) continue;

              var elementCategoryType = 0;
              {
                switch (elementValue)
                {
                  case ARDB.ImportInstance _:           elementCategoryType = 6;      break;
                  case ARDB.PointCloudInstance _:       elementCategoryType = 7;      break;
                  default: elementCategoryType = (int)  elementCategory.CategoryType; break;
                }

                if (isCategoryTypeHidden[(int) elementCategoryType]) continue;
              }

              if (documentIsWorkshared && !viewValue.IsWorksetVisible(elementValue.WorksetId)) continue;

              if (elementValue.ViewSpecific)
              {
                if (elementValue.OwnerViewId != viewId) continue;
                if (isAnnotationClipped && elementCategoryType == (int) ARDB.CategoryType.Annotation)
                {
                  if (elementValue.get_BoundingBox(viewValue) is ARDB.BoundingBoxXYZ bbox)
                  {
                    var bboxMin = bbox.Transform.OfPoint(bbox.Min);
                    var bboxMax = bbox.Transform.OfPoint(bbox.Max);

                    if (annotationClipPlanes.X.Min?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (annotationClipPlanes.X.Max?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (annotationClipPlanes.Y.Min?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (annotationClipPlanes.Y.Max?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                  }
                  else continue;
                }
              }
              else
              {
                if (viewPhaseFilter is object && viewPhase.IsValid() && elementValue.HasPhases())
                {
                  var status = elementValue.GetPhaseStatus(viewPhase);
                  if (status != ARDB.ElementOnPhaseStatus.None)
                  {
                    var presentation = viewPhaseFilter.GetPhaseStatusPresentation(status);
                    if (presentation == ARDB.PhaseStatusPresentation.DontShow) continue;
                  }
                }

                if (isModelClipped)
                {
                  if (elementValue.get_BoundingBox(viewValue) is ARDB.BoundingBoxXYZ bbox)
                  {
                    var bboxMin = bbox.Transform.OfPoint(bbox.Min);
                    var bboxMax = bbox.Transform.OfPoint(bbox.Max);

                    if (modelClipPlanes.X.Min?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (modelClipPlanes.X.Max?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (modelClipPlanes.Y.Min?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (modelClipPlanes.Y.Max?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (modelClipPlanes.Z.Min?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                    if (modelClipPlanes.Z.Max?.IsAboveOutline(bboxMin, bboxMax) is true) continue;
                  }
                  else continue;
                }
              }

              if (areViewGraphicsOverridesAllowed)
              {
                if (viewValue.GetCategoryHidden(elementCategory.Id)) continue;
                if (elementValue.IsHidden(viewValue)) continue;
                if (viewVisibilityFilter?.PassesFilter(elementValue) == true) continue;
              }

              visibleIds.Add(elementValue.Id);
            }
          }

          if (visibleIds.Count > 0)
          {
            using (var filter = CompoundElementFilter.ExclusionFilter(visibleIds, inverted: true))
            using (var collector = new ARDB.FilteredElementCollector(viewDocument, viewId).WherePasses(filter))
            {
              return collector.ToReadOnlyElementIdSet();
            }
          }
        }
      }

      return ElementIdExtension.EmptySet;
    }

    static ARDB.ElementFilter GetElementVisibilityFilter(ARDB.View view, bool hidden)
    {
      var filters = new List<ARDB.ElementFilter>();

      var viewDocument = view.Document;
      if (!viewDocument.IsFamilyDocument && view.AreGraphicsOverridesAllowed())
      {
        foreach (var filterId in view.GetFilters())
        {
          // Skip filters that do not hide elements
          if (hidden == view.GetFilterVisibility(filterId)) continue;

          switch (viewDocument.GetElement(filterId))
          {
            case ARDB.SelectionFilterElement selectionFilterElement:
              filters.Add(CompoundElementFilter.ExclusionFilter(selectionFilterElement.GetElementIds(), inverted: true));
              break;

            case ARDB.ParameterFilterElement parameterFilterElement:
              filters.Add(parameterFilterElement.ToElementFilter());
              break;
          }
        }
      }

      return filters.Count > 0 ? CompoundElementFilter.Union(filters) : default;
    }
  }
}
