using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class WallExtension
  {
    /// <summary>
    /// Returns orientation vector of the wall corrected for wall flip
    /// </summary>
    /// <returns>Orientation vector</returns>
    public static XYZ GetOrientationVector(this Wall wall)
    {
      return wall.Flipped ? -wall.Orientation : wall.Orientation;
    }

    /// <summary>
    /// Return total width of the wall
    /// </summary>
    /// <returns>Total width</returns>
    public static double GetWidth(this Wall wall)
    {
      // for some reason the base Width unit for Curtain walls is different
      return wall.WallType.Kind == WallKind.Curtain ? wall.Width * 12 : wall.Width;
    }

    /// <summary>
    /// Return LocationCurve of the wall
    /// </summary>
    /// <returns>Wall Location Curve</returns>
    public static LocationCurve GetLocationCurve(this Wall wall) => wall.Location as LocationCurve;

    /// <summary>
    /// Return center curve of the wall
    /// </summary>
    /// <param name="wall"></param>
    /// <returns></returns>
    public static Curve GetCenterCurve(this Wall wall)
    {
      // TODO: stacked walls center line is not correct
      return wall.GetLocationCurve().Curve;
    }
  }
}
