using System;
using System.Linq;
using Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.UI.Extensions;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ViewExtension
  {
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
    /// The bounds of the view in paper space (in pixels).
    /// </summary>
    /// <param name="view"></param>
    /// <param name="DPI"></param>
    /// <returns><see cref="System.Drawing.Rectangle.Empty"/> on empty views.</returns>
    public static System.Drawing.Rectangle GetOutlineRectangle(this View view, int DPI = 72)
    {
      using (var outline = view.Outline)
      {
        var left   = (int) Math.Round(outline.Min.U * 12.0 * DPI);
        var top    = (int) Math.Round(outline.Min.V * 12.0 * DPI);
        var right  = (int) Math.Round(outline.Max.U * 12.0 * DPI);
        var bottom = (int) Math.Round(outline.Max.V * 12.0 * DPI);

        return new System.Drawing.Rectangle(0, 0, right - left, bottom - top);
      }
    }

    /// <summary>
    /// The bounds of the view in in screen (in pixels).
    /// </summary>
    /// <param name="view"></param>
    /// <returns><see cref="System.Drawing.Rectangle.Empty"/> if UI View is not currently open.</returns>
    public static System.Drawing.Rectangle GetWindowRectangle(this View view)
    {
      if(view.TryGetOpenUIView(out var uiView))
        return uiView.GetWindowRectangle().ToRectangle();

      return System.Drawing.Rectangle.Empty;
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
        view.GetSketchGridId() is ElementId sketchGridId &&
        view.Document.GetElement(sketchGridId) is Element sketchGrid
      )
      {
        name = sketchGrid.Name;
        gridSpacing = sketchGrid.get_Parameter(BuiltInParameter.SKETCH_GRID_SPACING_PARAM)?.AsDouble() ?? double.NaN;

        using (var options = new Options() { View = view })
        {
          var geometry = sketchGrid.get_Geometry(options);
          if (geometry.FirstOrDefault() is Solid solid && solid.Faces.Size == 1)
          {
            if (solid.Faces.get_Item(0) is Face face)
            {
              surface = face.GetSurface();
              bboxUV = face.GetBoundingBox();
              return true;
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
