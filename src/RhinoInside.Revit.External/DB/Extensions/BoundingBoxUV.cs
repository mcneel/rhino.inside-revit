using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BoundingBoxUVExtension
  {
    public static bool IsUnset(this BoundingBoxUV value)
    {
      return !(value is object && value.Min.U <= value.Max.U && value.Min.V <= value.Max.V);
    }

    public static UV Evaluate(this BoundingBoxUV value, UV uv)
    {
      if (value.IsUnset()) return default;
      if (uv is null) return default;

      var u = uv.U;
      var v = uv.V;

      return new UV
      (
        value.Min.U * (1.0 - u) + value.Max.U * u,
        value.Min.V * (1.0 - v) + value.Max.V * v
      );
    }

    public static void Union(this BoundingBoxUV value, BoundingBoxUV uv)
    {
      if (uv.IsUnset()) return;

      Union(value, uv.Min);
      Union(value, uv.Max);
    }

    public static void Union(this BoundingBoxUV value, UV uv)
    {
      value.Min = new UV(Math.Min(value.Min.U, uv.U), Math.Min(value.Min.V, uv.V));
      value.Max = new UV(Math.Max(value.Max.U, uv.U), Math.Max(value.Max.V, uv.V));
    }
  }

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
