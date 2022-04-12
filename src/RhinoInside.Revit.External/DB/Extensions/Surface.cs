using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class SurfaceExtension
  {
    public static bool MatchesParametricOrientation(this Surface surface)
    {
#if REVIT_2018
      return surface.OrientationMatchesParametricOrientation;
#else
      return true;
#endif
    }

#if !REVIT_2018
    public static void Project(this Plane plane, XYZ point, out UV uv, out double distance)
    {
      var v = point - plane.Origin;
      uv = new UV(v.DotProduct(plane.XVec), v.DotProduct(plane.YVec));
      distance = plane.Evaluate(uv).DistanceTo(point);
    }
#endif
  }
}
