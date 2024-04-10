using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class WallExtension
  {
    /// <summary>
    /// Return total width of the wall
    /// </summary>
    /// <returns>Total width</returns>
    internal static double GetWidth(this Wall wall)
    {
      // for some reason the base Width unit for Curtain walls is different
      return wall.WallType.Kind == WallKind.Curtain ? wall.Width * 12 : wall.Width;
    }

#if !REVIT_2022
    public static bool CanHaveProfileSketch(this Wall wall)
    {
      return wall.WallType.Kind == WallKind.Basic &&
            !wall.IsStackedWallMember &&
            (wall.Location as LocationCurve).Curve is Line;
    }

    public static Sketch CreateProfileSketch(this Wall wall)
    {
      throw new System.NotImplementedException($"{nameof(CreateProfileSketch)} is not implement.");
    }
#endif
  }
}
