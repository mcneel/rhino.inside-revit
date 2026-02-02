using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class SurfaceExtension
  {
    /// <summary>
    /// Indicates whether this Surface's orientation is the same as or opposite to its parametric orientation.
    /// </summary>
    /// <param name="surface"></param>
    /// <returns></returns>
    public static bool MatchesParametricOrientation(this Surface surface)
    {
#if REVIT_2018
      return surface.OrientationMatchesParametricOrientation;
#else
      return true;
#endif
    }

#if REVIT_2019
    public static double DistanceTo(this Surface surface, XYZ xyz, out UV uv)
    {
      try { surface.Project(xyz, out uv, out var distance); return distance; }
      catch
      {
        uv = null;
        return double.NaN;
      }
    }
#endif
  }
}
