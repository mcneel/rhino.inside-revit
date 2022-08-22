using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BoundingBoxXYZExtension
  {
    public static bool IsUnset(this BoundingBoxXYZ value)
    {
      return !(value is object && value.Min.X <= value.Max.X && value.Min.Y <= value.Max.Y && value.Min.Z <= value.Max.Z);
    }

    public static XYZ Evaluate(this BoundingBoxXYZ value, XYZ xyz)
    {
      if (value.IsUnset()) return default;
      if (xyz is null) return default;

      var x = xyz.X;
      var y = xyz.Y;
      var z = xyz.Z;

      return new XYZ
      (
        value.Min.X * (1.0 - x) + value.Max.X * x,
        value.Min.Y * (1.0 - y) + value.Max.Y * y,
        value.Min.Z * (1.0 - z) + value.Max.Z * z
      );
    }

    public static void Union(this BoundingBoxXYZ value, BoundingBoxXYZ xyz)
    {
      if (xyz.IsUnset()) return;

      Union(value, xyz.Min);
      Union(value, xyz.Max);
    }

    public static void Union(this BoundingBoxXYZ value, XYZ xyz)
    {
      value.Min = new XYZ(Math.Min(value.Min.X, xyz.X), Math.Min(value.Min.Y, xyz.Y), Math.Min(value.Min.Z, xyz.Z));
      value.Max = new XYZ(Math.Max(value.Max.X, xyz.X), Math.Max(value.Max.Y, xyz.Y), Math.Max(value.Max.Z, xyz.Z));
    }
  }
}
