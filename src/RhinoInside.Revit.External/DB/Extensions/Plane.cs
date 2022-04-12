using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class PlaneExtensions
  {
    public static XYZ Evaluate(this Plane plane, UV uv)
    {
      return plane.Origin + uv.U * plane.XVec + uv.V * plane.YVec;
    }

    public static XYZ Evaluate(this Plane plane, UV uv, double distance)
    {
      return plane.Origin + uv.U * plane.XVec + uv.V * plane.YVec + distance * plane.Normal;
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
