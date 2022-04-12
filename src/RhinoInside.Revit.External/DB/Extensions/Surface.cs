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
  }
}
