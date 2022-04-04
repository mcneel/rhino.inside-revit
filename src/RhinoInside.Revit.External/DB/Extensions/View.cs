using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ViewExtension
  {
    static ViewFamily ToViewFamily(this ViewType viewType)
    {
      switch (viewType)
      {
        case ViewType.FloorPlan:              return ViewFamily.FloorPlan;
        case ViewType.CeilingPlan:            return ViewFamily.CeilingPlan;
        case ViewType.Elevation:              return ViewFamily.Elevation;
        case ViewType.ThreeD:                 return ViewFamily.ThreeDimensional;
        case ViewType.Schedule:               return ViewFamily.Schedule;
        case ViewType.DrawingSheet:           return ViewFamily.Sheet;
        case ViewType.ProjectBrowser:         return ViewFamily.Invalid;
        case ViewType.Report:                 return ViewFamily.Invalid;
        case ViewType.DraftingView:           return ViewFamily.Drafting;
        case ViewType.Legend:                 return ViewFamily.Legend;
        case ViewType.SystemBrowser:          return ViewFamily.Invalid;
        case ViewType.EngineeringPlan:        return ViewFamily.StructuralPlan;
        case ViewType.AreaPlan:               return ViewFamily.AreaPlan;
        case ViewType.Section:                return ViewFamily.Section;
        case ViewType.Detail:                 return ViewFamily.Detail;
        case ViewType.CostReport:             return ViewFamily.CostReport;
        case ViewType.LoadsReport:            return ViewFamily.LoadsReport;
        case ViewType.PresureLossReport:      return ViewFamily.PressureLossReport;
        case ViewType.ColumnSchedule:         return ViewFamily.GraphicalColumnSchedule;
        case ViewType.PanelSchedule:          return ViewFamily.PanelSchedule;
        case ViewType.Walkthrough:            return ViewFamily.Walkthrough;
        case ViewType.Rendering:              return ViewFamily.ImageView;
#if REVIT_2020
        case ViewType.SystemsAnalysisReport:  return ViewFamily.SystemsAnalysisReport;
#endif
      }

      return ViewFamily.Invalid;
    }

    public static ViewFamily GetViewFamily(this View view) => view.ViewType.ToViewFamily();

    /// <summary>
    /// Checks if the provided <see cref="Autodesk.Revit.DB.ViewType"/> represents a graphical view.
    /// </summary>
    /// <param name="viewType"></param>
    /// <returns>true if <paramref name="viewType"/> represents a graphical view type.</returns>
    public static bool IsGraphicalViewType(this ViewType viewType)
    {
      switch (viewType)
      {
        case ViewType.Undefined:
        case ViewType.Schedule:
        case ViewType.ProjectBrowser:
        case ViewType.SystemBrowser:
        case ViewType.Internal:
          return false;
      }

      return true;
    }

    /// <summary>
    /// Checks if the provided <see cref="Autodesk.Revit.DB.View"/> represents a graphical view.
    /// </summary>
    /// <param name="view"></param>
    /// <returns>true if <paramref name="view"/> is a graphical view.</returns>
    public static bool IsGraphicalView(this View view)
    {
      if (view is null) return false;
      if (view.IsTemplate) return false;

      return IsGraphicalViewType(view.ViewType);
    }

    /// <summary>
    /// The bounds of the view in paper space (in pixels).
    /// </summary>
    /// <param name="view"></param>
    /// <param name="DPI"></param>
    /// <returns>Empty <see cref="Rectangle"/> on empty views.</returns>
    public static Rectangle GetOutlineRectangle(this View view, int DPI = 72)
    {
      using (var outline = view.Outline)
      {
        return new Rectangle
        (
          left    : (int) Math.Round(outline.Min.U * 12.0 * DPI),
          top     : (int) Math.Round(outline.Min.V * 12.0 * DPI),
          right   : (int) Math.Round(outline.Max.U * 12.0 * DPI),
          bottom  : (int) Math.Round(outline.Max.V * 12.0 * DPI)
        );
      }
    }

    /// <summary>
    /// Finds the id of the first available level associated with this plan view.
    /// </summary>
    /// <param name="view"></param>
    /// <returns>InvalidElementId if no associated level is found.</returns>
    /// <seealso cref="Autodesk.Revit.DB.Level.FindAssociatedPlanViewId"/>
    public static ElementId FindAssociatedLevelId(this ViewPlan view)
    {
      if (view.get_Parameter(BuiltInParameter.PLAN_VIEW_LEVEL)?.AsString() is string levelName)
      {
        using (var collector = new FilteredElementCollector(view.Document))
        {
          return collector.OfClass(typeof(Level)).
                 WhereParameterEqualsTo(BuiltInParameter.DATUM_TEXT, levelName).
                 FirstElementId();
        }
      }

      return ElementId.InvalidElementId;
    }

    internal static ElementId GetSketchGridId(this View view)
    {
      return view.
        GetDependentElements(new ElementCategoryFilter(BuiltInCategory.OST_IOSSketchGrid)).
        FirstOrDefault();
    }

    internal static bool TryGetSketchGridSurface
    (
      this View view,
      out string name,
      out Surface surface,
      out BoundingBoxUV bboxUV,
      out double gridSpacing
    )
    {
      if
      (
        view.SketchPlane is object &&
        view.GetSketchGridId() is ElementId sketchGridId &&
        view.Document.GetElement(sketchGridId) is Element sketchGrid
      )
      {
        using (var options = new Options() { View = view })
        {
          var geometry = sketchGrid.get_Geometry(options);

          using (geometry is object ? default : view.Document.RollBackScope())
          {
            // SketchGrid need to be displayed at least once to have geometry.
            if (geometry is null)
            {
              view.ShowActiveWorkPlane();
              geometry = sketchGrid.get_Geometry(options);
            }

            if (geometry?.FirstOrDefault() is Solid solid && solid.Faces.Size == 1)
            {
              if (solid.Faces.get_Item(0) is Face face)
              {
                name = sketchGrid.Name;
                gridSpacing = sketchGrid.get_Parameter(BuiltInParameter.SKETCH_GRID_SPACING_PARAM)?.AsDouble() ?? double.NaN;

                surface = face.GetSurface();
                bboxUV = face.GetBoundingBox();
                return true;
              }
            }
          }
        }
      }

      name = default;
      surface = default;
      bboxUV = default;
      gridSpacing = double.NaN;
      return false;
    }
  }
}
