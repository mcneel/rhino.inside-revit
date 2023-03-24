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

#if REVIT_2021
    const ViewType   ViewType_SystemsAnalysisReport   = ViewType.SystemsAnalysisReport;
    const ViewFamily ViewFamily_SystemsAnalysisReport = ViewFamily.SystemsAnalysisReport;
#else
    const ViewType   ViewType_SystemsAnalysisReport   = (ViewType) 126;
    const ViewFamily ViewFamily_SystemsAnalysisReport = (ViewFamily) 121;
#endif

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
        case ViewType_SystemsAnalysisReport:  return ViewFamily_SystemsAnalysisReport;
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
        var (min, max) = outline;
        return new Rectangle
        (
          left    : (int) Math.Round(min.U * 12.0 * DPI),
          top     : (int) Math.Round(min.V * 12.0 * DPI),
          right   : (int) Math.Round(max.U * 12.0 * DPI),
          bottom  : (int) Math.Round(max.V * 12.0 * DPI)
        );
      }
    }

    /// <summary>
    /// The bounds of the view in model space (in feet).
    /// </summary>
    /// <param name="view"></param>
    /// <param name="DPI"></param>
    /// <returns>Empty <see cref="BoundingBoxUV"/> on empty views.</returns>
    public static BoundingBoxUV GetModelOutline(this View view)
    {
      if (!view.CanBePrinted)
        return BoundingBoxUVExtension.Empty;

      if (view is View3D view3D)
      {
        if (view.CropBoxActive)
        {
          using (var cropBox = view.CropBox)
            return cropBox.ToBoundingBoxUV();
        }
        else if (view3D.IsPerspective)
        {
          var radius = 0.1;
          return new BoundingBoxUV(-radius, -radius * 3.0 / 4.0, +radius, +radius * 3.0 / 4.0);
        }
      }

      {
        var scale = Math.Max(view.Scale, 1);
        var outline = view.Outline;
        outline.Min *= scale;
        outline.Max *= scale;
        return outline;
      }
    }

    public static BoundingBoxXYZ GetAnnotationClipBox(this View view)
    {
      if (view is null || view.IsTemplate)
        return BoundingBoxXYZExtension.Empty;

      using (var shapeManager = view.GetCropRegionShapeManager())
      {
        if (!shapeManager.CanHaveAnnotationCrop)
          return BoundingBoxXYZExtension.Universe;

        switch (view.CropBox)
        {
          case BoundingBoxXYZ clipBox:

            var scale = Math.Max(view.Scale, 1);
            var (min, max) = clipBox;

            clipBox.Min = new XYZ(clipBox.Min.X - shapeManager.LeftAnnotationCropOffset * scale, clipBox.Min.Y - shapeManager.BottomAnnotationCropOffset * scale, min.Z);
            clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisX, true);
            clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisY, true);
            clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisZ, false);

            clipBox.Max = new XYZ(clipBox.Max.X + shapeManager.RightAnnotationCropOffset * scale, clipBox.Max.Y + shapeManager.TopAnnotationCropOffset * scale, max.Z);
            clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisX, true);
            clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisY, true);
            clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisZ, false);

            clipBox.Enabled = view.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE)?.AsBoolean() ?? false;
            return clipBox;

          default:
            return default;
        }
      }
    }

    public static BoundingBoxXYZ GetModelClipBox(this View view)
    {
      if (view is null || view.IsTemplate)
        return BoundingBoxXYZExtension.Empty;

      if (!view.ViewType.IsGraphicalViewType())
        return BoundingBoxXYZExtension.Universe;

      switch (view.CropBox)
      {
        case BoundingBoxXYZ clipBox:

          switch (view)
          {
            case View3D view3D:
              if (view3D.IsSectionBoxActive)
                clipBox.Intersection(view3D.GetSectionBox());
              break;

            case ViewSection _:
              clipBox.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisZ, true);
              break;
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
        var interval = viewPlan.GetViewRangeInterval();
        if (interval.Left.IsEnabled || interval.Right.IsEnabled)
        {
          return new BoundingBoxIntersectsFilter
          (
            new Outline
            (
              new XYZ(-CompoundElementFilter.BoundingBoxLimits, -CompoundElementFilter.BoundingBoxLimits, interval.Left),
              new XYZ(+CompoundElementFilter.BoundingBoxLimits, +CompoundElementFilter.BoundingBoxLimits, interval.Right)
            ),
            view.Document.Application.VertexTolerance,
            clipped
          );
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

      if (bottom != -CompoundElementFilter.BoundingBoxLimits || top != +CompoundElementFilter.BoundingBoxLimits)
      {
        return new BoundingBoxIntersectsFilter
        (
          new Outline
          (
            new XYZ(-CompoundElementFilter.BoundingBoxLimits, -CompoundElementFilter.BoundingBoxLimits, bottom),
            new XYZ(+CompoundElementFilter.BoundingBoxLimits, +CompoundElementFilter.BoundingBoxLimits, top)
          ),
          view.Document.Application.VertexTolerance,
          clipped
        );
      }

      return default;
    }

    public static ElementFilter GetModelClipFilter(this View view, bool clipped = false)
    {
      var filter = default(ElementFilter);

      if (view.IsGraphicalView())
      {
        if (view is ViewSheet || view is ViewDrafting || view.ViewType == ViewType.Legend)
          return clipped ? CompoundElementFilter.Universe : CompoundElementFilter.Empty; // No model elements here

        filter = clipped ?
        CompoundElementFilter.Intersect(GetViewRangeFilter(view, clipped), GetUnderlayFilter(view, clipped)) :
        CompoundElementFilter.Union(GetViewRangeFilter(view, clipped), GetUnderlayFilter(view, clipped));

        var modelClipBox = view.GetModelClipBox();
        if (modelClipBox.Enabled)
          filter = CompoundElementFilter.Intersect(filter, new BoundingBoxIntersectsFilter(modelClipBox.ToOutLine(), view.Document.Application.VertexTolerance, clipped));
      }
      else if (view is TableView table)
      {
        if (table.TargetId.IsValid())
          return CompoundElementFilter.ExclusionFilter(table.TargetId, inverted: true);

        if (view is ViewSchedule && view.get_Parameter(BuiltInParameter.SCHEDULE_CATEGORY)?.AsElementId() is ElementId scheduleCategoryId)
          return new ElementCategoryFilter(scheduleCategoryId, clipped);

        return CompoundElementFilter.Universe;
      }
      else if (view is ImageView)
      {
        return clipped ? CompoundElementFilter.Universe : CompoundElementFilter.Empty;
      }
      else
      {
        switch (view.ViewType)
        {
          case ViewType.ProjectBrowser:
          case ViewType.SystemBrowser:
            return clipped ? CompoundElementFilter.Universe : CompoundElementFilter.Empty;

          case ViewType.Report:
          case ViewType.CostReport:
          case ViewType.LoadsReport:
          case ViewType.PresureLossReport:
          case ViewType_SystemsAnalysisReport:
            return clipped ? CompoundElementFilter.Empty : CompoundElementFilter.Universe;
        }

        return CompoundElementFilter.Empty;
      }

      return filter;
    }

    public static ElementFilter GetClipFilter(this View view, bool clipped = false)
    {
      var modelClipFilter = GetModelClipFilter(view, clipped);
      var annotationClipFilter = new ElementOwnerViewFilter(view.Id, clipped);

      return clipped ?
        CompoundElementFilter.Intersect(modelClipFilter, annotationClipFilter) :
        CompoundElementFilter.Union    (modelClipFilter, annotationClipFilter);
    }

    #region Viewer
    public static ViewOrientation3D GetSavedOrientation(this View view)
    {
      if (view is View3D view3D)
        return view3D.GetSavedOrientation();

      return new ViewOrientation3D(view.Origin, view.UpDirection, -view.ViewDirection);
    }

    public static void SetSavedOrientation(this View view, ViewOrientation3D newViewOrientation3D)
    {
      if (view is View3D view3D)
      {
        view3D.SetOrientation(newViewOrientation3D);
        if (ElementNaming.IsValidName(view3D.Name))
          view3D.SaveOrientation();
      }
      else if (view.GetViewer() is Element viewer)
      {
        var viewOrigin = view.Origin;
        var viewBasisY = (UnitXYZ) view.UpDirection;
        var viewBasisX = (UnitXYZ) view.RightDirection;
        UnitXYZ.Orthonormal(viewBasisX, viewBasisY, out var viewBasisZ);

        var newOrigin = newViewOrientation3D.EyePosition;
        var newBasisY = newViewOrientation3D.UpDirection.ToUnitXYZ();
        var newBasisZ = -newViewOrientation3D.ForwardDirection.ToUnitXYZ();
        UnitXYZ.Orthonormal(newBasisY, newBasisZ, out var newBasisX);

        {
          var modified = false;
          var pinned = viewer.Pinned;

          try
          {
            if (!viewBasisZ.IsCodirectionalTo(newBasisZ))
            {
              var axisDirection = viewBasisZ.CrossProduct(newBasisZ);
              if (axisDirection.IsZeroLength()) axisDirection = viewBasisY;

              viewer.Pinned = !(modified = true);
              using (var axis = Line.CreateUnbound(viewOrigin, axisDirection))
                ElementTransformUtils.RotateElement(viewer.Document, viewer.Id, axis, viewBasisZ.AngleTo(newBasisZ));

              viewOrigin = view.Origin;
              viewBasisX = (UnitXYZ) view.RightDirection;
            }

            if (!viewBasisX.IsCodirectionalTo(newBasisX))
            {
              viewer.Pinned = !(modified = true);
              using (var axis = Line.CreateUnbound(viewOrigin, newBasisZ))
                ElementTransformUtils.RotateElement(viewer.Document, viewer.Id, axis, viewBasisX.AngleOnPlaneTo(newBasisX, newBasisZ));

              viewOrigin = view.Origin;
            }

            {
              var trans = (newOrigin - viewOrigin);
              if (!trans.IsZeroLength())
              {
                viewer.Pinned = !(modified = true);
                ElementTransformUtils.MoveElement(viewer.Document, viewer.Id, trans);
              }
            }
          }
          finally
          {
            if (modified)
              viewer.Pinned = pinned;
          }
        }
      }
    }

    static readonly BuiltInCategory[] ViewerCategories =
    {
      BuiltInCategory.OST_Viewers,
      BuiltInCategory.OST_Cameras,
      BuiltInCategory.INVALID,
    };

    static ElementFilter GetViewerFilter(ElementId viewId, bool inverted = false)
    {
      return CompoundElementFilter.Intersect
      (
        new ElementIsElementTypeFilter(inverted: true),
        new ElementMulticategoryFilter(ViewerCategories),
        new ElementClassFilter(typeof(View), inverted: true),
        new ElementParameterFilter
        (
          new FilterElementIdRule
          (
            new ParameterValueProvider(new ElementId(BuiltInParameter.ID_PARAM)),
            new FilterNumericEquals(),
            viewId
          ), inverted
        )
      );
    }

    internal static bool IsViewer(Element element) => GetViewerFilter(element.Id, inverted: true).PassesFilter(element);

    public static Element GetViewer(this View view)
    {
      var dependents = view.GetDependentElements(GetViewerFilter(view.Id));
      switch (dependents.Count)
      {
        case 0:   return null;
        case 1:   return view.Document.GetElement(dependents[0]);
        default:  throw new NotSupportedException();
      }
    }
    #endregion

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

  public static class ViewPlanExtension
  {
    internal static BoundingInterval GetViewRangeInterval(this ViewPlan viewPlan)
    {
      var genLevel = viewPlan.GenLevel;
      using (var viewRange = viewPlan.GetViewRange()) return
      (
        GetLevelElevation(viewRange, genLevel, PlanViewPlane.ViewDepthPlane),
        GetLevelElevation(viewRange, genLevel, PlanViewPlane.TopClipPlane)
      );
    }

    static BoundingValue GetLevelElevation(PlanViewRange viewRange, Level level, PlanViewPlane plane)
    {
      var levelId = viewRange.GetLevelId(plane);
      if (levelId == PlanViewRange.Current) { } // just use the current
      else if (levelId == PlanViewRange.LevelBelow) level = level.Document.GetNearestBaseLevel(level.ProjectElevation, out var _);
      else if (levelId == PlanViewRange.LevelAbove) level = level.Document.GetNearestTopLevel (level.ProjectElevation, out var _);
      else if (levelId == PlanViewRange.Unlimited)
      {
        switch (plane)
        {
          case PlanViewPlane.CutPlane:        return new BoundingValue(level.ProjectElevation, BoundingValue.Bounding.DisabledMax);
          case PlanViewPlane.TopClipPlane:    return new BoundingValue(level.ProjectElevation, BoundingValue.Bounding.DisabledMax);
          case PlanViewPlane.BottomClipPlane: return new BoundingValue(level.ProjectElevation, BoundingValue.Bounding.DisabledMin);
          case PlanViewPlane.ViewDepthPlane:  return new BoundingValue(level.ProjectElevation, BoundingValue.Bounding.DisabledMin);
          case PlanViewPlane.UnderlayBottom:  return new BoundingValue(level.ProjectElevation, BoundingValue.Bounding.DisabledMin);
        }
      }
      else level = level.Document.GetElement(levelId) as Level;

      return new BoundingValue(level.ProjectElevation + viewRange.GetOffset(plane));
    }
  }
}
