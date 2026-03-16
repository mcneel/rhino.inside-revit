using System;
using System.Collections.Generic;
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

      if (view.Document.IsLinked)
        return false;

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

    public static bool IsOpen(this View view)
    {
      if (view is null)
        throw new ArgumentNullException(nameof(view));

      if (view.Document.IsLinked)
        return false;

      using (var uiDoc = new UIDocument(view.Document))
        return uiDoc.GetOpenUIViews().Any(x => x.ViewId == view.Id);
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
        case ViewType.Internal:
        case ViewType.ProjectBrowser:
        case ViewType.SystemBrowser:
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
      if (view.IsCallout()) return false;

      return IsGraphicalViewType(view.ViewType);
    }

    /// <summary>
    /// Checks if the provided <see cref="Autodesk.Revit.DB.ViewType"/> represents a model view.
    /// </summary>
    /// <param name="viewType"></param>
    /// <returns>true if <paramref name="viewType"/> represents a model view type.</returns>
    public static bool IsModelViewType(this ViewType viewType)
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
    /// Checks if the provided <see cref="Autodesk.Revit.DB.View"/> represents a model view.
    /// </summary>
    /// <param name="view"></param>
    /// <returns>true if <paramref name="view"/> is a model view.</returns>
    public static bool IsModelView(this View view)
    {
      if (view is null) return false;
      if (view.IsTemplate) return false;

      return IsModelViewType(view.ViewType);
    }

    /// <summary>
    /// Checks if the provided <see cref="Autodesk.Revit.DB.View"/> supports annotative elements.
    /// </summary>
    /// <param name="view"></param>
    /// <returns>true if <paramref name="view"/> is an annotation view.</returns>
    public static bool IsAnnotationView(this View view)
    {
      if (view is null) return false;
      if (view.IsTemplate) return false;

      switch (view.ViewType)
      {
        case ViewType.DrawingSheet:

        case ViewType.FloorPlan:
        case ViewType.CeilingPlan:
        case ViewType.EngineeringPlan:
        case ViewType.AreaPlan:

        case ViewType.Elevation:
        case ViewType.Section:
        case ViewType.Detail:

        case ViewType.DraftingView:
        case ViewType.Legend:
          return true;

        case ViewType.ThreeD:
          return view.get_Parameter(BuiltInParameter.VIEWER_PERSPECTIVE).AsInteger() == 0;
      }

      return false;
    }

    /// <summary>
    /// Indicates if the view is a callout view.
    /// </summary>
    /// <param name="view"></param>
    /// <returns>true if <paramref name="view"/> is a callout view.</returns>
    public static bool IsCallout(this View view)
    {
#if REVIT_2022
      return view.IsCallout;
#else
      return view.get_Parameter(BuiltInParameter.SECTION_PARENT_VIEW_NAME)?.HasValue is true;
#endif
    }

#if !REVIT_2022
    /// <summary>
    /// Gets ID of the callout parent view.
    /// </summary>
    /// <param name="view"></param>
    /// <returns>ID of a view in which this callout was created or InvalidElementId if there is no parent.</returns>
    public static ElementId GetCalloutParentId(this View view)
    {
      return view.get_Parameter(BuiltInParameter.SECTION_PARENT_VIEW_NAME)?.AsElementId() ?? ElementIdExtension.Invalid;
    }
#endif

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
        view.SketchPlane is SketchPlane sketchPlane &&
        view.GetSketchGridId() is ElementId sketchGridId &&
        view.Document.GetElement(sketchGridId) is Element sketchGrid
      )
      {
        name = sketchGrid.Name;
        gridSpacing = sketchGrid.get_Parameter(BuiltInParameter.SKETCH_GRID_SPACING_PARAM)?.AsDouble() ?? double.NaN;
        if (gridSpacing <= 0.0) gridSpacing = double.NaN;

        // If it is visible it will have geometry.
        using (var options = new Options() { View = view })
        {
          if
          (
            sketchGrid.get_Geometry(options) is GeometryElement geometry &&
            geometry.FirstOrDefault() is Solid solid && solid.Faces.Size == 1 &&
            solid.Faces.get_Item(0) is Face face
          )
          {
            surface = face.GetSurface();
            bboxUV = face.GetBoundingBox();
            return true;
          }
        }

        // Else use the view outline
        var plane = sketchPlane.GetPlane();
        var coordSystem = Transform.Identity;
        coordSystem.SetCoordSystem(plane.Origin, (UnitXYZ) plane.XVec, (UnitXYZ) plane.YVec, (UnitXYZ) plane.Normal);

        if (XYZExtension.TryGetBoundingBox(view.GetModelClipBox().GetCorners(), out var bboxXYZ, coordSystem))
        {
          surface = plane;
          var (min, max) = bboxXYZ;
          bboxUV = new BoundingBoxUV(min.X, min.Y, max.X, max.Y);
          return true;
        }
      }

      name = default;
      surface = default;
      bboxUV = default;
      gridSpacing = double.NaN;
      return false;
    }
    #endregion

    #region Outline
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

      if (!view.ViewType.IsModelViewType())
        return BoundingBoxXYZExtension.Universe;

      switch (view.CropBox)
      {
        case BoundingBoxXYZ clipBox:

          switch (view)
          {
            case View3D view3D:
              if (view3D.IsSectionBoxActive)
                clipBox = BoundingBoxXYZExtension.MinIntersection(clipBox, view3D.GetSectionBox());
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
    #endregion

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
      return ElementFilters.Intersect
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

    static ElementFilter GetViewportFilter(string viewportSheetNumber, string viewportViewName) => ElementFilters.Intersect
    (
      new ElementIsElementTypeFilter(inverted: true),
      new ElementClassFilter(typeof(Viewport)),
      new ElementParameterFilter
      (
        ElementFilters.FilterStringRule
        (
          new ParameterValueProvider(new ElementId(BuiltInParameter.VIEWPORT_SHEET_NUMBER)),
          new FilterStringEquals(),
          viewportSheetNumber
        )
      ),
      new ElementParameterFilter
      (
        ElementFilters.FilterStringRule
        (
          new ParameterValueProvider(new ElementId(BuiltInParameter.VIEWPORT_VIEW_NAME)),
          new FilterStringEquals(),
          viewportViewName
        )
      )
    );

    public static Viewport GetViewport(this View view)
    {
      var sheetNumber = view.get_Parameter(BuiltInParameter.VIEWPORT_SHEET_NUMBER).AsString();
      if (sheetNumber is null) return null;

      var viewName = view.get_Parameter(BuiltInParameter.VIEW_NAME).AsString();
      if (viewName is null) return null;

      var dependents = view.GetDependentElements(GetViewportFilter(sheetNumber, viewName));
      switch (dependents.Count)
      {
        case 0: return null;
        case 1: return view.Document.GetElement(dependents[0]) as Viewport;
        default: throw new NotSupportedException();
      }
    }
    #endregion

    #region Phasing
    public static void SetDefaultPhaseFilter(this View view, bool lastPhase = true)
    {
      if (!view.Document.IsFamilyDocument)
      {
        if (view.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER) is Parameter viewPhaseFilter && !viewPhaseFilter.IsReadOnly)
        {
          if (!(viewPhaseFilter.AsElement() is PhaseFilter { IsDefault: true }))
          {
            using (var collector = new FilteredElementCollector(view.Document).OfClass(typeof(PhaseFilter)))
            {
              if (collector.Cast<PhaseFilter>().Where(x => x.IsDefault).FirstOrDefault() is PhaseFilter phaseFilter)
                viewPhaseFilter.Update(phaseFilter.Id);
            }
          }
        }

        if (lastPhase)
        {
          if
          (
            view.get_Parameter(BuiltInParameter.VIEW_PHASE) is Parameter viewPhase && !viewPhase.IsReadOnly &&
            view.Document.Phases.Cast<Phase>().LastOrDefault() is Phase phase
          )
            viewPhase.Update(phase.Id);
        }
      }
    }
    #endregion

    #region FilteredElementCollector
    internal static T GetVisibleElement<T>(this View view, ElementId id) where T : Element
    {
      // Check it exists and is of the expected type
      if (view.Document.GetElement(id) is T element)
      {
        // Check if is visible in the view.
        if(view.GetVisibleElements(new ElementId[] { element.Id }, accurate: true).Contains(element.Id))
          return element;
      }

      return null;
    }

    internal static RevitLinkInstance GetVisibleLink<T>(this View view, ElementId id, out Document linkDocument)
    {
      // Check it exists and is of the expected type
      if (view.Document.GetElement(id) is RevitLinkInstance instance)
      {
        linkDocument = instance.GetLinkDocument();
        if (linkDocument is null) return null;

        // Check if is visible in the view.
        if (view.GetVisibleElements(new ElementId[] { instance.Id }, accurate: false).Contains(instance.Id))
          return instance;
      }

      linkDocument = null;
      return null;
    }

    public static FilteredElementCollector GetVisibleElementsCollector(this View view)
    {
      if (FilteredElementCollector.IsViewValidForElementIteration(view.Document, view.Id))
        return new FilteredElementCollector(view.Document, view.Id);

      return FilteredElementCollectorExtension.Empty(view.Document);
    }

    public static FilteredElementCollector GetVisibleElementsCollector(this View view, ElementId linkId)
    {
      if (linkId is null)
        throw new ArgumentNullException(nameof(linkId));

      if (FilteredElementCollector.IsViewValidForElementIteration(view.Document, view.Id))
      {
#if REVIT_2024
        return new FilteredElementCollector(view.Document, view.Id, linkId);
#else
        // If link instance is visible in view.
        if
        (
          view.GetVisibleLink<RevitLinkInstance>(linkId, out var linkDocument) is RevitLinkInstance link &&
          link.GetTransform().TryGetInverse(out var inverse)
        )
        {
          var linkedElementIds = default(ICollection<ElementId>);
          using (linkDocument.RollBackScope())
          {
            var offset = inverse.OfPoint(XYZExtension.Zero);

            var elementsToCopy = new HashSet<ElementId>(default(ElementIdEqualityComparer)) { view.Id };
            if (view.GenLevel?.Id is ElementId genLevelId && genLevelId.IsValid()) elementsToCopy.Add(genLevelId);
            if (view is ViewPlan viewPlanSource)
            {
              using (var viewRange = viewPlanSource.GetViewRange())
              {
                for (var plane = PlanViewPlane.CutPlane; plane <= PlanViewPlane.UnderlayBottom; ++plane)
                {
                  var levelId = viewRange.GetLevelId(plane);
                  if (levelId.IsBuiltInId()) continue;
                  elementsToCopy.Add(levelId);
                }
              }

              var underlayBaseLevelId = viewPlanSource.GetUnderlayBaseLevel();
              if (!underlayBaseLevelId.IsBuiltInId()) elementsToCopy.Add(underlayBaseLevelId);
              var underlayTopLevelId = viewPlanSource.GetUnderlayTopLevel();
              if (!underlayTopLevelId.IsBuiltInId()) elementsToCopy.Add(underlayTopLevelId);
            }

            // Inverse transform copied elements.
            var copiedElementIds = view.Document.CopyElements(elementsToCopy, linkDocument);
            foreach (var copiedElement in copiedElementIds.Values.Select(linkDocument.GetElement))
            {
              switch (copiedElement)
              {
                case View copiedView:

                  if (copiedView is View3D view3D)
                  {
                    if (view3D.IsLocked) view3D.Unlock();
                    if (view3D.IsSectionBoxActive) view3D.SetSectionBox(inverse.OfBoundingBoxXYZ(view3D.GetSectionBox()));
                  }

                  copiedView.SetLocation
                  (
                    inverse.OfPoint(copiedView.Origin),
                    inverse.OfVector(copiedView.RightDirection).ToUnitXYZ(),
                    inverse.OfVector(copiedView.UpDirection).ToUnitXYZ()
                  );

                  break;

                case Level copiedLevel:
                  copiedLevel.Elevation += offset.Z;
                  break;
              }
            }

            linkDocument.Regenerate();

            using (var collector = new FilteredElementCollector(linkDocument, copiedElementIds[view.Id]))
              linkedElementIds = collector.ToElementIds();
          }

          return new FilteredElementCollector(linkDocument, linkedElementIds);
        }
#endif
      }

      return FilteredElementCollectorExtension.Empty(view.Document);
    }
    #endregion

    #region ElementFilter
    static ElementFilter GetViewRangeFilter(this View view, bool clipped = false)
    {
      if (view is ViewPlan viewPlan)
      {
        var interval = viewPlan.GetViewRangeInterval();
        if (interval.Left.IsEnabled || interval.Right.IsEnabled)
        {
          return ElementFilters.BoundingBoxIntersectsFilter
          (
            new Outline
            (
              new XYZ(-ElementFilters.BoundingBoxLimits, -ElementFilters.BoundingBoxLimits, interval.Left),
              new XYZ(+ElementFilters.BoundingBoxLimits, +ElementFilters.BoundingBoxLimits, interval.Right)
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

      var topId = view.get_Parameter(BuiltInParameter.VIEW_UNDERLAY_TOP_ID)?.AsElementId() ?? ElementId.InvalidElementId;
      var bottom = (view.Document.GetElement(bottomId) as Level)?.ProjectElevation ?? -ElementFilters.BoundingBoxLimits;
      var top = (view.Document.GetElement(topId) as Level)?.ProjectElevation ?? +ElementFilters.BoundingBoxLimits;

      if (bottom != -ElementFilters.BoundingBoxLimits || top != +ElementFilters.BoundingBoxLimits)
      {
        return ElementFilters.BoundingBoxIntersectsFilter
        (
          new Outline
          (
            new XYZ(-ElementFilters.BoundingBoxLimits, -ElementFilters.BoundingBoxLimits, bottom),
            new XYZ(+ElementFilters.BoundingBoxLimits, +ElementFilters.BoundingBoxLimits, top)
          ),
          view.Document.Application.VertexTolerance,
          clipped
        );
      }

      return default;
    }

    public static ElementFilter GetModelFilter(this View view, bool clipped = false)
    {
      if (view is ViewSheet || view is ViewDrafting || view.ViewType == ViewType.Legend)
        return clipped ? ElementFilters.Universe : ElementFilters.Empty; // No model elements here

      var filter = clipped ?
      ElementFilters.Intersect(GetViewRangeFilter(view, clipped), GetUnderlayFilter(view, clipped)) :
      ElementFilters.Union(GetViewRangeFilter(view, clipped), GetUnderlayFilter(view, clipped));

      var modelClipBox = view.GetModelClipBox();
      filter = ElementFilters.Intersect
      (
        filter,
        modelClipBox.Enabled && view.CropBoxActive ?
        ElementFilters.BoundingBoxIntersectsFilter(modelClipBox.ToOutLine(), view.Document.Application.VertexTolerance, clipped):
        ElementFilters.ElementHasBoundingBoxFilter
      );

      return filter;
    }

    public static ElementFilter GetModelClipFilter(this View view, bool clipped = false)
    {
      var filter = default(ElementFilter);

      if (view.IsModelView())
      {
        filter = GetModelFilter(view, clipped);
      }
      else if (view is TableView table)
      {
        if (table.TargetId.IsValid())
          return ElementFilters.ExclusionFilter(table.TargetId, inverted: true);

        if (view is ViewSchedule viewSchedule && viewSchedule.Definition is ScheduleDefinition definition)
          return new ElementCategoryFilter(definition.CategoryId, clipped);

        return ElementFilters.Universe;
      }
      else if (view is ImageView)
      {
        return clipped ? ElementFilters.Universe : ElementFilters.Empty;
      }
      else
      {
        switch (view.ViewType)
        {
          case ViewType.ProjectBrowser:
          case ViewType.SystemBrowser:
            return clipped ? ElementFilters.Universe : ElementFilters.Empty;

          case ViewType.Report:
          case ViewType.CostReport:
          case ViewType.LoadsReport:
          case ViewType.PresureLossReport:
          case ViewType_SystemsAnalysisReport:
            return clipped ? ElementFilters.Empty : ElementFilters.Universe;
        }

        return ElementFilters.Empty;
      }

      return filter;
    }

    public static ElementFilter GetClipFilter(this View view, bool clipped = false)
    {
      var modelClipFilter = GetModelClipFilter(view, clipped);
      var annotationClipFilter = ElementFilters.Union
      (
        new ElementOwnerViewFilter(view.Id, clipped),
        ElementFilters.ElementClassFilter
        (
          typeof(DatumPlane),
          typeof(RevitLinkInstance)
        )
      );

      return clipped ?
        ElementFilters.Intersect(modelClipFilter, annotationClipFilter) :
        ElementFilters.Union(modelClipFilter, annotationClipFilter);
    }

    public static ElementFilter GetElementCategoryFilter(this View view, CategoryType categoryType)
    {
      var categories = view.Document.Settings.Categories.Cast<Category>().Where
      (
        x =>
        {
          if (x.CategoryType != categoryType) return false;
          switch (categoryType)
          {
            case CategoryType.Model:
              switch (x.Id.ToBuiltInCategory())
              {
                case BuiltInCategory.OST_ImportObjectStyles: if (view.AreImportCategoriesHidden) return false; break;
                case BuiltInCategory.OST_PointClouds: if (view.ArePointCloudsHidden) return false; break;
              }
              break;

            case CategoryType.Annotation:
              if (view.AreAnnotationCategoriesHidden) return false; break;

            case CategoryType.AnalyticalModel:
              if (view.AreAnalyticalModelCategoriesHidden) return false; break;
          }
          if (!view.CanCategoryBeHidden(x.Id) || view.GetCategoryHidden(x.Id)) return false;
          return true;
        }
      ).
      Select(x => x.Id).
      ToArray();

      return new ElementMulticategoryFilter(categories);
    }

    public static ElementFilter GetElementVisibilityFilter(this View view, Document document = null, bool hidden = false)
    {
      var filters = new List<ElementFilter>();

      var viewDocument = view.Document;
      if (!viewDocument.IsFamilyDocument && view.AreGraphicsOverridesAllowed())
      {
        var linked = document is object && !document.Equals(viewDocument);
        foreach (var filterId in view.GetFilters())
        {
          // Skip filters that do not hide elements
          if (hidden == view.GetFilterVisibility(filterId)) continue;

          switch (viewDocument.GetElement(filterId))
          {
            case SelectionFilterElement selectionFilterElement:
              if (!linked)
                filters.Add(ElementFilters.ExclusionFilter(selectionFilterElement.GetElementIds(), inverted: true));
              break;

            case ParameterFilterElement parameterFilterElement:
              filters.Add(parameterFilterElement.ToElementFilter());
              break;
          }
        }
      }

      return filters.Count > 0 ? ElementFilters.Union(filters) : default;
    }

    internal static ISet<ElementId> GetVisibleElements(this View view, ICollection<ElementId> ids, bool accurate = true)
    {
      if (ids.Count > 0 && FilteredElementCollector.IsViewValidForElementIteration(view.Document, view.Id))
      {
        var viewDocument = view.Document;
        var viewId = view.Id;

        if (view.GetClipFilter(clipped: false) is ElementFilter clipFilter)
        {
          using (var collector = new FilteredElementCollector(viewDocument, ids))
            ids = collector.WherePasses(clipFilter).ToElementIds();
        }

        if (ids.Count > 0)
        {
          var documentIsWorkshared = viewDocument.IsWorkshared;
          var modelClipBox = view.GetModelClipBox();
          var isModelClipped = modelClipBox.GetPlaneEquations(out var modelClipPlanes, Numerical.Tolerance.Default);
          var annotationClipBox = view.GetAnnotationClipBox();
          var isAnnotationClipped = annotationClipBox.GetPlaneEquations(out var annotationClipPlanes, Numerical.Tolerance.Default);
          var areViewGraphicsOverridesAllowed = view.AreGraphicsOverridesAllowed();
          var isCategoryTypeHidden = new bool[]
          {
            true,
            view.AreModelCategoriesHidden,
            view.AreAnnotationCategoriesHidden,
            false, // ??
            false, // ??
            view.AreAnalyticalModelCategoriesHidden,
            view.AreImportCategoriesHidden,
            view.ArePointCloudsHidden
          };

          var visibleIds = new List<ElementId>(ids.Count);
          var viewPhaseFilter = ((view.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER)?.AsElement()) as PhaseFilter);
          var viewPhase = view.get_Parameter(BuiltInParameter.VIEW_PHASE)?.AsElementId() ?? ElementIdExtension.Invalid;

          using (var elementVisibilityFilter = view.GetElementVisibilityFilter(viewDocument, hidden: true))
          {
            foreach (var elementValue in ids.Select(viewDocument.GetElement))
            {
              var elementCategory = elementValue.Category;
              if (elementCategory is null) continue;

              var elementCategoryType = 0;
              {
                switch (elementValue)
                {
                  case ImportInstance _:           elementCategoryType = 6;      break;
                  case PointCloudInstance _:       elementCategoryType = 7;      break;
                  default: elementCategoryType = (int)  elementCategory.CategoryType; break;
                }

                if (isCategoryTypeHidden[(int) elementCategoryType]) continue;
              }

              if (documentIsWorkshared && !view.IsWorksetVisible(elementValue.WorksetId)) continue;

              if (elementValue.ViewSpecific)
              {
                if (elementValue.OwnerViewId != viewId) continue;
                if (isAnnotationClipped && elementCategoryType == (int) CategoryType.Annotation)
                {
                  if (elementValue.get_BoundingBox(view) is BoundingBoxXYZ bbox)
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
                  if (status != ElementOnPhaseStatus.None)
                  {
                    var presentation = viewPhaseFilter.GetPhaseStatusPresentation(status);
                    if (presentation == PhaseStatusPresentation.DontShow) continue;
                  }
                }

                if (isModelClipped)
                {
                  if (elementValue.get_BoundingBox(view) is BoundingBoxXYZ bbox)
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
                if (view.GetCategoryHidden(elementCategory.Id)) continue;
                if (elementValue.IsHidden(view)) continue;
                if (elementVisibilityFilter?.PassesFilter(elementValue) is true) continue;
              }

              visibleIds.Add(elementValue.Id);
            }
          }

          if (visibleIds.Count > 0)
          {
            if (accurate)
            {
              using (var filter = ElementFilters.ExclusionFilter(visibleIds, inverted: true))
              using (var collector = new FilteredElementCollector(viewDocument, viewId).WherePasses(filter))
              {
                return collector.ToReadOnlyElementIdSet();
              }
            }
            else return new SortedSet<ElementId>(visibleIds, ElementIdComparer.NoNullsAscending);
          }
        }
      }

      return ElementIdExtension.EmptySet;
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

  public static class ViewSectionExtension
  {
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
  }
}
