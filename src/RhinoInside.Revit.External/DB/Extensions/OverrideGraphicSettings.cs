using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class OverrideGraphicSettingsExtensions
  {
    public static bool IsSurfaceForegroundPatternVisible(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.IsSurfaceForegroundPatternVisible;
#else
      return settings.IsProjectionFillPatternVisible;
#endif
    }

    public static ElementId SurfaceForegroundPatternId(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.SurfaceForegroundPatternId;
#else
      return settings.ProjectionFillPatternId;
#endif
    }

    public static Color SurfaceForegroundPatternColor(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.SurfaceForegroundPatternColor;
#else
      return settings.ProjectionFillColor;
#endif
    }

    public static bool IsSurfaceBackgroundPatternVisible(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.IsSurfaceBackgroundPatternVisible;
#else
      return false;
#endif
    }

    public static ElementId SurfaceBackgroundPatternId(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.SurfaceBackgroundPatternId;
#else
      return ElementIdExtension.Invalid;
#endif
    }

    public static Color SurfaceBackgroundPatternColor(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.SurfaceBackgroundPatternColor;
#else
      return Color.InvalidColorValue;
#endif
    }

    public static bool IsCutForegroundPatternVisible(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.IsCutForegroundPatternVisible;
#else
      return settings.IsCutFillPatternVisible;
#endif
    }

    public static ElementId CutForegroundPatternId(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.CutForegroundPatternId;
#else
      return settings.CutFillPatternId;
#endif
    }

    public static Color CutForegroundPatternColor(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.CutForegroundPatternColor;
#else
      return settings.CutFillColor;
#endif
    }

    public static bool IsCutBackgroundPatternVisible(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.IsCutBackgroundPatternVisible;
#else
      return false;
#endif
    }

    public static ElementId CutBackgroundPatternId(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.CutBackgroundPatternId;
#else
      return ElementIdExtension.Invalid;
#endif
    }

    public static Color CutBackgroundPatternColor(this OverrideGraphicSettings settings)
    {
#if REVIT_2019
      return settings.CutBackgroundPatternColor;
#else
      return Color.InvalidColorValue;
#endif
    }

#if !REVIT_2019
    public static OverrideGraphicSettings SetSurfaceForegroundPatternVisible(this OverrideGraphicSettings settings, bool visible)
    {
      return settings.SetProjectionFillPatternVisible(visible);
    }

    public static OverrideGraphicSettings SetSurfaceForegroundPatternId(this OverrideGraphicSettings settings, ElementId fillPatternId)
    {
      return settings.SetProjectionFillPatternId(fillPatternId);
    }

    public static OverrideGraphicSettings SetSurfaceForegroundPatternColor(this OverrideGraphicSettings settings, Color color)
    {
      return settings.SetProjectionFillColor(color);
    }

    public static OverrideGraphicSettings SetSurfaceBackgroundPatternVisible(this OverrideGraphicSettings settings, bool visible)
    {
      return settings;
    }

    public static OverrideGraphicSettings SetSurfaceBackgroundPatternId(this OverrideGraphicSettings settings, ElementId fillPatternId)
    {
      return settings;
    }

    public static OverrideGraphicSettings SetSurfaceBackgroundPatternColor(this OverrideGraphicSettings settings, Color color)
    {
      return settings;
    }

    public static OverrideGraphicSettings SetCutForegroundPatternVisible(this OverrideGraphicSettings settings, bool visible)
    {
      return settings.SetCutFillPatternVisible(visible);
    }

    public static OverrideGraphicSettings SetCutForegroundPatternId(this OverrideGraphicSettings settings, ElementId fillPatternId)
    {
      return settings.SetCutFillPatternId(fillPatternId);
    }

    public static OverrideGraphicSettings SetCutForegroundPatternColor(this OverrideGraphicSettings settings, Color color)
    {
      return settings.SetCutFillColor(color);
    }

    public static OverrideGraphicSettings SetCutBackgroundPatternVisible(this OverrideGraphicSettings settings, bool visible)
    {
      return settings;
    }

    public static OverrideGraphicSettings SetCutBackgroundPatternId(this OverrideGraphicSettings settings, ElementId fillPatternId)
    {
      return settings;
    }

    public static OverrideGraphicSettings SetCutBackgroundPatternColor(this OverrideGraphicSettings settings, Color color)
    {
      return settings;
    }
#endif
  }
}
