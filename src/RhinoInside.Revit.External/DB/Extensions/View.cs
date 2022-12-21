using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ViewExtension
  {
    public static bool Close(this View view)
    {
      if (view is null)
        throw new ArgumentNullException(nameof(view));

      using (var uiDocument = new UIDocument(view.Document))
      {
        if (uiDocument.GetOpenUIViews().FirstOrDefault(x => x.ViewId == view.Id) is UIView uiView)
        {
          uiView.Close();
          return true;
        }
      }

      return false;
    }

    static ViewFamily ToViewFamily(ViewType viewType)
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
#if REVIT_2021
        case ViewType.SystemsAnalysisReport:  return ViewFamily.SystemsAnalysisReport;
#else
        case (ViewType) 126:                  return (ViewFamily) 121;
#endif
      }

      return ViewFamily.Invalid;
    }

    public static ViewFamily GetViewFamily(this View view) => ToViewFamily(view.ViewType);

    /// <summary>
    /// Checks if the provided <see cref="Autodesk.Revit.DB.ViewType"/> represents a graphical view.
    /// </summary>
    /// <param name="viewType"></param>
    /// <returns>true if <paramref name="viewType"/> represents a graphical view type.</returns>
    public static bool IsGraphicalViewType(this ViewType viewType)
    {
      switch (viewType)
      {
        // ViewSheet
        case ViewType.DrawingSheet:

        // View3D
        case ViewType.ThreeD:
        case ViewType.Walkthrough:

        // ImageView
        case ViewType.Rendering:

        // ViewPlan
        case ViewType.FloorPlan:
        case ViewType.CeilingPlan:
        case ViewType.EngineeringPlan:
        case ViewType.AreaPlan:

        // ViewSection
        case ViewType.Elevation:
        case ViewType.Section:
        case ViewType.Detail:

        // ViewDrafting
        case ViewType.DraftingView:

        // View
        case ViewType.Legend:

          return true;
      }

      return false;
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
    /// Checks if the provided <see cref="Autodesk.Revit.DB.View"/> supports annotative elements.
    /// </summary>
    /// <param name="view"></param>
    /// <returns>true if <paramref name="view"/> is a graphical view.</returns>
    public static bool IsAnnotationView(this View view)
    {
      if (view is null) return false;
      if (view.IsTemplate) return false;

      switch (view.ViewType)
      {
        case ViewType.DrawingSheet:

        case ViewType.FloorPlan:
        case ViewType.CeilingPlan:
        case ViewType.AreaPlan:
        case ViewType.EngineeringPlan:

        case ViewType.Elevation:
        case ViewType.Section:
        case ViewType.Detail:

        case ViewType.DraftingView:
        case ViewType.Legend:
          return true;
      }

      return false;
    }

    #region SketchGrid
    static readonly ElementFilter OST_IOSSketchGridFilter = new ElementCategoryFilter(BuiltInCategory.OST_IOSSketchGrid);
    internal static ElementId GetSketchGridId(this View view) => view.GetDependentElements(OST_IOSSketchGridFilter).FirstOrDefault();

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

          using (geometry is object || view.Document.IsReadOnly ? default : view.Document.RollBackScope())
          {
            // SketchGrid need to be displayed at least once to have geometry.
            if (geometry is null && view.Document.IsModifiable)
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
    #endregion

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

    public static BoundingBoxXYZ GetClipBox(this View view)
    {
      switch (view.CropBox)
      {
        case BoundingBoxXYZ clipBox:

          clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisX, view.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_LEFT  )?.AsInteger() == 1);
          clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisX, view.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_RIGHT )?.AsInteger() == 1);

          clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisY, view.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_BOTTOM)?.AsInteger() == 1);
          clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisY, view.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_TOP   )?.AsInteger() == 1);

          clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisZ, view.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR   )?.AsInteger() == 1);
          clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisZ, view.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_NEAR  )?.AsInteger() == 1);

          // Intersect with Outline
          using (var outline = view.Outline)
          {
            var scale = Math.Max(view.Scale, 1);
            var (min, max) = clipBox;

            clipBox.Min = new XYZ(NumericTolerance.MinNumber(min.X, outline.Min.U * scale), NumericTolerance.MinNumber(min.Y, outline.Min.V * scale), min.Z);
            clipBox.Max = new XYZ(NumericTolerance.MaxNumber(max.X, outline.Max.U * scale), NumericTolerance.MaxNumber(max.Y, outline.Max.V * scale), max.Z);
          }

          if (view is View3D view3D && view3D.IsSectionBoxActive)
            clipBox.Intersection(view3D.GetSectionBox());

          for (int bound = BoundingBoxXYZExtension.BoundsMin; bound <= BoundingBoxXYZExtension.BoundsMax; ++bound)
          {
            for (int dim = BoundingBoxXYZExtension.AxisX; dim <= BoundingBoxXYZExtension.AxisZ; ++dim)
              clipBox.Enabled |= clipBox.get_BoundEnabled(bound, dim);
          }

          return clipBox;

        default:
          return default;
      }
    }

    static ElementFilter GetViewRangeFilter(this View view, bool clipped = false)
    {
      if (view is ViewPlan viewPlan)
      {
        using (var viewRange = viewPlan.GetViewRange())
        {
          var bottom = view.Document.GetElement(viewRange.GetLevelId(PlanViewPlane.ViewDepthPlane)) is Level bottomLevel ?
            bottomLevel.ProjectElevation + viewRange.GetOffset(PlanViewPlane.ViewDepthPlane) :
            -CompoundElementFilter.BoundingBoxLimits;

          var top = view.Document.GetElement(viewRange.GetLevelId(PlanViewPlane.TopClipPlane)) is Level topLevel ?
            topLevel.ProjectElevation + viewRange.GetOffset(PlanViewPlane.TopClipPlane) :
            +CompoundElementFilter.BoundingBoxLimits;

          using (var outline = view.Outline)
          {
            var scale = Math.Max(view.Scale, 1);
            return new BoundingBoxIntersectsFilter
            (
              new Outline
              (
                new XYZ(outline.Min.U * scale, outline.Min.V * scale, bottom),
                new XYZ(outline.Max.U * scale, outline.Max.V * scale, top)
              ),
              clipped
            );
          }
        }
      }

      return default;
    }

    static ElementFilter GetUnderlayFilter(this View view, bool clipped = false)
    {
      var bottomId = view.get_Parameter(BuiltInParameter.VIEW_UNDERLAY_BOTTOM_ID)?.AsElementId() ?? ElementId.InvalidElementId;
      if (bottomId == ElementId.InvalidElementId) return default;

      var topId  = view.get_Parameter(BuiltInParameter.VIEW_UNDERLAY_TOP_ID   )?.AsElementId() ?? ElementId.InvalidElementId;
      var bottom = (view.Document.GetElement(bottomId) as Level)?.ProjectElevation ?? -CompoundElementFilter.BoundingBoxLimits;
      var top    = (view.Document.GetElement(topId   ) as Level)?.ProjectElevation ?? +CompoundElementFilter.BoundingBoxLimits;

      using (var outline = view.Outline)
      {
        var scale = Math.Max(view.Scale, 1);

        return new BoundingBoxIntersectsFilter
        (
          new Outline
          (
            new XYZ(outline.Min.U * scale, outline.Min.V * scale, bottom),
            new XYZ(outline.Max.U * scale, outline.Max.V * scale, top)
          ),
          clipped
        );
      }
    }

    public static ElementFilter GetViewTypeFilter(this View view, bool clipped = false)
    {
      if (view is ViewSchedule)
        return new ElementCategoryFilter(view.get_Parameter(BuiltInParameter.SCHEDULE_CATEGORY).AsElementId(), clipped);

      var filter = clipped ?
        CompoundElementFilter.Intersect(GetViewRangeFilter(view, clipped), GetUnderlayFilter(view, clipped)) :
        CompoundElementFilter.Union    (GetViewRangeFilter(view, clipped), GetUnderlayFilter(view, clipped));

      return filter.IsEmpty() ? default : filter;
    }

    #region ViewSection
    static int IndexOfViewSection(ElevationMarker marker, ElementId viewId)
    {
      if (marker.HasElevations())
      {
        for (int i = 0; i < marker.MaximumViewCount; ++i)
        {
          if (marker.GetViewId(i) == viewId)
            return i;
        }
      }

      return -1;
    }

    public static ElevationMarker GetElevationMarker(this ViewSection viewSection)
    {
      using (var collector = new FilteredElementCollector(viewSection.Document))
      {
        return collector.
          OfCategory(BuiltInCategory.OST_Elev).
          OfClass(typeof(ElevationMarker)).
          OfType<ElevationMarker>().
          FirstOrDefault(x => IndexOfViewSection(x, viewSection.Id) >= 0);
      }
    }
    #endregion
  }
}
