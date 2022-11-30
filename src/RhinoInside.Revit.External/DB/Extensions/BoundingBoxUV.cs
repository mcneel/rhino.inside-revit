using System;
using System.Diagnostics;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BoundingBoxUVExtension
  {
    public const int AxisU = 0;
    public const int AxisV = 1;

    public const int BoundsMin = 0;
    public const int BoundsMax = 1;

    public static BoundingBoxUV Empty => new BoundingBoxUV
    (
      double.PositiveInfinity, double.PositiveInfinity,
      double.NegativeInfinity, double.NegativeInfinity
    );

    public static BoundingBoxUV All => new BoundingBoxUV
    (
      double.NegativeInfinity, double.NegativeInfinity,
      double.PositiveInfinity, double.PositiveInfinity
    );

    public static bool IsEmpty(this BoundingBoxUV value)
    {
      var (min, max) = value;
      return !(min.U <= max.U && min.V <= max.V);
    }

    public static bool IsNullOrEmpty(this BoundingBoxUV value)
    {
      return value?.IsEmpty() != false;
    }

    public static void Deconstruct
    (
      this BoundingBoxUV value,
      out UV min, out UV max
    )
    {
      min = value.Min;
      max = value.Max;
    }

    public static UV Evaluate(this BoundingBoxUV value, UV uv)
    {
      if (uv is null) return default;
      if (value.IsEmpty()) return UVExtension.NaN;

      var (u, v) = uv;
      var (min, max) = value;

      return new UV
      (
        min.U * (1.0 - u) + max.U * u,
        min.V * (1.0 - v) + max.V * v
      );
    }

    public static void Union(this BoundingBoxUV value, BoundingBoxUV uv)
    {
      if (uv.IsNullOrEmpty()) return;

      if (value.IsEmpty())
      {
        value.Min = uv.Min;
        value.Max = uv.Max;
      }
      else
      {
        UnionNotEmpty(value, uv.Min);
        UnionNotEmpty(value, uv.Max);
      }
    }

    public static void Union(this BoundingBoxUV value, UV uv)
    {
      if (value.IsEmpty())
      {
        value.Min = uv;
        value.Max = uv;
      }
      else UnionNotEmpty(value, uv);
    }

    static void UnionNotEmpty(this BoundingBoxUV value, UV uv)
    {
      Debug.Assert(!value.IsEmpty());

      var (u, v) = uv;
      var (min, max) = value;

      value.Min = new UV(Math.Min(min.U, u), Math.Min(min.V, v));
      value.Max = new UV(Math.Max(max.U, u), Math.Max(max.V, v));
    }
  }
}
