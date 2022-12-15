using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BoundingBoxXYZExtension
  {
    public const int AxisX = 0;
    public const int AxisY = 1;
    public const int AxisZ = 2;

    public const int BoundsMin = 0;
    public const int BoundsMax = 1;

    public static BoundingBoxXYZ Empty => new BoundingBoxXYZ()
    {
      Min = XYZExtension.MaxValue,
      Max = XYZExtension.MinValue,
    };

    public static BoundingBoxXYZ All => new BoundingBoxXYZ()
    {
      Min = XYZExtension.MinValue,
      Max = XYZExtension.MaxValue,
    };

    public static bool IsEmpty(this BoundingBoxXYZ value)
    {
      var (min, max) = value;
      return !(min.X <= max.X && min.Y <= max.Y && min.Z <= max.Z);
    }

    public static bool IsNullOrEmpty(this BoundingBoxXYZ value)
    {
      return value?.IsEmpty() != false;
    }

    public static void Deconstruct
    (
      this BoundingBoxXYZ value,
      out XYZ min, out XYZ max
    )
    {
      min = value.get_Bounds(BoundsMin);
      max = value.get_Bounds(BoundsMax);
    }

    public static void Deconstruct
    (
      this BoundingBoxXYZ value,
      out XYZ min, out XYZ max, out Transform transform
    )
    {
      min = value.get_Bounds(BoundsMin);
      max = value.get_Bounds(BoundsMax);
      transform = value.Transform;
    }

    public static XYZ Evaluate(this BoundingBoxXYZ value, XYZ xyz)
    {
      if (xyz is null) return default;
      if (value.IsEmpty()) return XYZExtension.NaN;

      var (x, y, z) = xyz;
      var (min, max) = value;

      return new XYZ
      (
        min.X * (1.0 - x) + max.X * x,
        min.Y * (1.0 - y) + max.Y * y,
        min.Z * (1.0 - z) + max.Z * z
      );
    }

    public static void Union(this BoundingBoxXYZ value, BoundingBoxXYZ xyz)
    {
      if (xyz.IsNullOrEmpty()) return;
      if (value.IsEmpty())
      {
        value.Transform = xyz.Transform;
        value.Min = xyz.Min;
        value.Max = xyz.Max;
      }
      else Union(value, xyz.GetCorners());
    }

    public static void Union(this BoundingBoxXYZ value, params XYZ[] xyz)
    {
      if (xyz.Length > 0)
      {
        using (var inverse = value.Transform.Inverse)
        {
          int p = 0;
          if (value.IsEmpty())
          {
            var point = inverse.OfPoint(xyz[p++]);
            value.Min = point;
            value.Max = point;
          }

          for (; p < xyz.Length; p++)
            UnionLocalNotEmpty(value, inverse.OfPoint(xyz[p]));
        }
      }
    }

    static void UnionLocalNotEmpty(this BoundingBoxXYZ value, XYZ xyz)
    {
      var (x, y, z) = xyz;
      var (min, max) = value;

      value.Min = new XYZ(Math.Min(min.X, x), Math.Min(min.Y, y), Math.Min(min.Z, z));
      value.Max = new XYZ(Math.Max(max.X, x), Math.Max(max.Y, y), Math.Max(max.Z, z));
    }

    public static Outline ToOutLine(this BoundingBoxXYZ value)
    {
      var points = value.GetCorners();

      (double X, double Y, double Z) min = (double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
      (double X, double Y, double Z) max = (double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

      foreach (var point in points)
      {
        var (x, y, z) = point;
        min = (Math.Min(min.X, x), Math.Min(min.Y, y), Math.Min(min.Z, z));
        max = (Math.Max(max.X, x), Math.Max(max.Y, y), Math.Max(max.Z, z));
      }

      return new Outline
      (
        new XYZ(min.X, min.Y, min.Z),
        new XYZ(max.X, max.Y, max.Z)
      );
    }

    public static XYZ[] GetCorners(this BoundingBoxXYZ value)
    {
      var (min, max, transform) = value;
      var (minX, minY, minZ) = min;
      var (maxX, maxY, maxZ) = max;

      return new XYZ[]
      {
        transform.OfPoint(new XYZ(minX, minY, minZ)),
        transform.OfPoint(new XYZ(maxX, minY, minZ)),
        transform.OfPoint(new XYZ(maxX, maxY, minZ)),
        transform.OfPoint(new XYZ(minX, maxY, minZ)),
        transform.OfPoint(new XYZ(minX, minY, maxZ)),
        transform.OfPoint(new XYZ(maxX, minY, maxZ)),
        transform.OfPoint(new XYZ(maxX, maxY, maxZ)),
        transform.OfPoint(new XYZ(minX, maxY, maxZ))
      };
    }

    internal static bool GetPlaneEquations
    (
      this BoundingBoxXYZ value,
      out
      (
        (PlaneEquation? Min, PlaneEquation? Max) X,
        (PlaneEquation? Min, PlaneEquation? Max) Y,
        (PlaneEquation? Min, PlaneEquation? Max) Z
      ) planes
    )
    {
      bool clipped = false;
      planes = default;

      var (min, max, transform) = value;
      var (origin, basisX, basisY, basisZ) = transform;

      using (transform)
      {
        if (clipped |= value.get_BoundEnabled(BoundsMin, AxisX))
          planes.X.Min = new PlaneEquation(origin + min.X * basisX, basisX);

        if (clipped |= value.get_BoundEnabled(BoundsMax, AxisX))
          planes.X.Max = new PlaneEquation(origin + max.X * basisX, -basisX);

        if (clipped |= value.get_BoundEnabled(BoundsMin, AxisY))
          planes.Y.Min = new PlaneEquation(origin + min.Y * basisY, basisY);

        if (clipped |= value.get_BoundEnabled(BoundsMax, AxisY))
          planes.Y.Max = new PlaneEquation(origin + max.Y * basisY, -basisY);

        if (clipped |= value.get_BoundEnabled(BoundsMin, AxisZ))
          planes.Z.Min = new PlaneEquation(origin + min.Z * basisZ, basisZ);

        if (clipped |= value.get_BoundEnabled(BoundsMax, AxisZ))
          planes.Z.Max = new PlaneEquation(origin + max.Z * basisZ, -basisZ);
      }

      return clipped;
    }
  }
}
