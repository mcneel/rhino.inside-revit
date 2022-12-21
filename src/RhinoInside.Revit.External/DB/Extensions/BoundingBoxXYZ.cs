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
      out XYZ min, out XYZ max, out Transform transform, out bool[,] bounded
    )
    {
      min = value.get_Bounds(BoundsMin);
      max = value.get_Bounds(BoundsMax);
      transform = value.Transform;

      bounded = new bool[,]
      {
        {
          value.get_BoundEnabled(BoundsMin, AxisX),
          value.get_BoundEnabled(BoundsMax, AxisX)
        },
        {
          value.get_BoundEnabled(BoundsMin, AxisY),
          value.get_BoundEnabled(BoundsMax, AxisY),
        },
        {
          value.get_BoundEnabled(BoundsMin, AxisZ),
          value.get_BoundEnabled(BoundsMax, AxisZ),
        }
      };
    }

    internal static void Deconstruct
    (
      this BoundingBoxXYZ value,
      out BoundingInterval x, out BoundingInterval y, out BoundingInterval z
    )
    {
      var (min, max) = value;
      x = new BoundingInterval
      (
        (min.X, value.get_BoundEnabled(BoundsMin, AxisX) ? BoundingValue.Bounding.Enabled : BoundingValue.Bounding.DisabledMin),
        (max.X, value.get_BoundEnabled(BoundsMax, AxisX) ? BoundingValue.Bounding.Enabled : BoundingValue.Bounding.DisabledMax)
      );
      y = new BoundingInterval
      (
        (min.Y, value.get_BoundEnabled(BoundsMin, AxisY) ? BoundingValue.Bounding.Enabled : BoundingValue.Bounding.DisabledMin),
        (max.Y, value.get_BoundEnabled(BoundsMax, AxisY) ? BoundingValue.Bounding.Enabled : BoundingValue.Bounding.DisabledMax)
      );
      z = new BoundingInterval
      (
        (min.Z, value.get_BoundEnabled(BoundsMin, AxisZ) ? BoundingValue.Bounding.Enabled : BoundingValue.Bounding.DisabledMin),
        (max.Z, value.get_BoundEnabled(BoundsMax, AxisZ) ? BoundingValue.Bounding.Enabled : BoundingValue.Bounding.DisabledMax)
      );
    }

    public static BoundingBoxXYZ Empty => new BoundingBoxXYZ()
    {
      Min = XYZExtension.MaxValue,
      Max = XYZExtension.MinValue,
    };

    public static bool IsEmpty(this BoundingBoxXYZ value)
    {
      var (min, max) = value;
      return min == XYZExtension.MaxValue && max == XYZExtension.MinValue;
    }

    public static bool IsNullOrEmpty(this BoundingBoxXYZ value)
    {
      return value?.IsEmpty() != false;
    }

    public static BoundingBoxXYZ Universe => new BoundingBoxXYZ()
    {
      Min = XYZExtension.MinValue,
      Max = XYZExtension.MaxValue,
    };

    public static bool IsUniverse(this BoundingBoxXYZ value)
    {
      var (min, max) = value;
      return min == XYZExtension.MinValue && max == XYZExtension.MaxValue;
    }

    public static bool IsNullOrUniverse(this BoundingBoxXYZ value)
    {
      return value?.IsUniverse() != false;
    }

    public static bool IsNegative(this BoundingBoxXYZ value)
    {
      if (value is null) return false;

      var (min, max) = value;
      if (min.X > max.X && min.Y > max.Y && min.Z > max.Z) return true;

      return false;
    }

    public static bool IsPositive(this BoundingBoxXYZ value)
    {
      if (value is null) return false;

      var (min, max) = value;
      if (min.X < max.X && min.Y < max.Y && min.Z < max.Z) return true;

      return false;
    }

    public static bool IsZeroLength(this BoundingBoxXYZ value, double tolerance = NumericTolerance.DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, NumericTolerance.Upsilon);

      var (x, y, z) = value.Max - value.Min;
      return XYZExtension.IsZeroLength(x, y, z, tolerance);
    }

    public static double GetLength(this BoundingBoxXYZ value)
    {
      var (min, max) = value;
      var diagonal = max - min;
      var (x, y, z) = diagonal;

      if (x == 0.0 && y == 0.0 && z == 0.0) return 0.0;
      if (x <  0.0 && y <  0.0 && z <  0.0) return -NumericTolerance.Abs(x, y, z);
      if (x >  0.0 && y >  0.0 && z >  0.0) return +NumericTolerance.Abs(x, y, z);

      return double.NaN;
    }

    public static double GetArea(this BoundingBoxXYZ value)
    {
      var (min, max) = value;
      var diagonal = max - min;
      var (x, y, z) = diagonal;

      if (x == 0.0 && y == 0.0 && z == 0.0) return 0.0;
      if (x < 0.0 && y < 0.0 && z < 0.0) return -((2.0 * x * x) + (2.0 * y * y) + (2.0 * z * z));
      if (x > 0.0 && y > 0.0 && z > 0.0) return +((2.0 * x * x) + (2.0 * y * y) + (2.0 * z * z));

      return double.NaN;
    }

    public static double GetVolume(this BoundingBoxXYZ value)
    {
      var (min, max) = value;
      var diagonal = max - min;
      var (x, y, z) = diagonal;

      if (x == 0.0 && y == 0.0 && z == 0.0) return 0.0;
      if (x < 0.0 && y < 0.0 && z < 0.0) return -(x * y * z);
      if (x > 0.0 && y > 0.0 && z > 0.0) return +(x * y * z);

      return double.NaN;
    }

    public static BoundingBoxXYZ Clone(this BoundingBoxXYZ value)
    {
      var other = new BoundingBoxXYZ()
      {
        Enabled = value.Enabled,
        Transform = value.Transform,
        Min = value.Min,
        Max = value.Max,
      };

      for (int bound = BoundsMin; bound <= BoundsMax; ++bound)
        for (int dim = AxisX; dim <= AxisZ; ++dim)
          value.set_BoundEnabled(bound, dim, other.get_BoundEnabled(bound, dim));

      return other;
    }

    public static void CopyFrom(this BoundingBoxXYZ value, BoundingBoxXYZ other)
    {
      value.Enabled = other.Enabled;
      for (int bound = BoundsMin; bound <= BoundsMax; ++bound)
        for (int dim = AxisX; dim <= AxisZ; ++dim)
          value.set_BoundEnabled(bound, dim, other.get_BoundEnabled(bound, dim));

      value.Min = other.Min;
      value.Max = other.Max;
      value.Transform = other.Transform;
    }

    public static XYZ Evaluate(this BoundingBoxXYZ value, XYZ xyz)
    {
      if (xyz is null) return default;

      var (x, y, z) = xyz.Normalize(0D);
      var (min, max) = value;

      return new XYZ
      (
        min.X * (1.0 - x) + max.X * x,
        min.Y * (1.0 - y) + max.Y * y,
        min.Z * (1.0 - z) + max.Z * z
      );
    }

    public static void Intersection(this BoundingBoxXYZ value, BoundingBoxXYZ other)
    {
      var otherToValue = value.Transform * other.Transform.Inverse;

      var (aX, aY, aZ) = value;
      var (bX, bY, bZ) = otherToValue.OfBoundingBoxXYZ(other);

      var x = aX & bX;
      var y = aY & bY;
      var z = aZ & bZ;

      value.Min = new XYZ(x.Left,  y.Left,  z.Left);
      value.Max = new XYZ(x.Right, y.Right, z.Right);

      value.set_BoundEnabled(BoundsMin, AxisX, x.Left.IsEnabled);
      value.set_BoundEnabled(BoundsMax, AxisX, x.Right.IsEnabled);
      value.set_BoundEnabled(BoundsMin, AxisY, y.Left.IsEnabled);
      value.set_BoundEnabled(BoundsMax, AxisY, y.Right.IsEnabled);
      value.set_BoundEnabled(BoundsMin, AxisZ, z.Left.IsEnabled);
      value.set_BoundEnabled(BoundsMax, AxisZ, z.Right.IsEnabled);
    }

    public static void Union(this BoundingBoxXYZ value, BoundingBoxXYZ other)
    {
      var otherToValue = value.Transform * other.Transform.Inverse;

      var (aX, aY, aZ) = value;
      var (bX, bY, bZ) = otherToValue.OfBoundingBoxXYZ(other);

      var x = aX | bX;
      var y = aY | bY;
      var z = aZ | bZ;

      value.Min = new XYZ(x.Left, y.Left, z.Left);
      value.Max = new XYZ(x.Right, y.Right, z.Right);
    }

    public static void Union(this BoundingBoxXYZ value, params XYZ[] xyz)
    {
      if (xyz.Length > 0)
      {
        using (var inverse = value.Transform.Inverse)
        {
          var ((minX, minY, minZ), (maxX, maxY, maxZ)) = value;

          for (int p = 0; p < xyz.Length; p++)
          {
            var (x, y, z) = xyz[p];
            minX = NumericTolerance.MinNumber(minX, x); maxX = NumericTolerance.MaxNumber(maxX, x);
            minY = NumericTolerance.MinNumber(minY, y); maxY = NumericTolerance.MaxNumber(maxY, y);
            minZ = NumericTolerance.MinNumber(minZ, z); maxZ = NumericTolerance.MaxNumber(maxZ, z);
          }

          value.Min = new XYZ(minX, minY, minZ);
          value.Max = new XYZ(maxX, maxY, maxZ);
        }
      }
    }

    public static Outline ToOutLine(this BoundingBoxXYZ value)
    {
      var points = value.GetCorners();

      (double X, double Y, double Z) min = (double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
      (double X, double Y, double Z) max = (double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

      foreach (var point in points)
      {
        var (x, y, z) = point;
        min = (NumericTolerance.MinNumber(min.X, x), NumericTolerance.MinNumber(min.Y, y), NumericTolerance.MinNumber(min.Z, z));
        max = (NumericTolerance.MaxNumber(max.X, x), NumericTolerance.MaxNumber(max.Y, y), NumericTolerance.MaxNumber(max.Z, z));
      }

      return new Outline
      (
        new XYZ(min.X, min.Y, min.Z),
        new XYZ(max.X, max.Y, max.Z)
      );
    }

    public static XYZ[] GetCorners(this BoundingBoxXYZ value)
    {
      using (var transform = value.Transform)
      {
        var (X, Y, Z) = value;
        return new XYZ[]
        {
          transform.OfPoint(new XYZ(X.Left,  Y.Left,  Z.Left)),
          transform.OfPoint(new XYZ(X.Right, Y.Left,  Z.Left)),
          transform.OfPoint(new XYZ(X.Right, Y.Right, Z.Left)),
          transform.OfPoint(new XYZ(X.Left,  Y.Right, Z.Left)),

          transform.OfPoint(new XYZ(X.Left,  Y.Left,  Z.Right)),
          transform.OfPoint(new XYZ(X.Right, Y.Left,  Z.Right)),
          transform.OfPoint(new XYZ(X.Right, Y.Right, Z.Right)),
          transform.OfPoint(new XYZ(X.Left,  Y.Right, Z.Right)),
        };
      }
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

      using (var transform = value.Transform)
      {
        var (origin, basisX, basisY, basisZ) = transform;
        var (min, max) = value;

        if (clipped |= value.get_BoundEnabled(BoundsMin, AxisX))
          planes.X.Min = new PlaneEquation(origin + min.X * basisX,  basisX);

        if (clipped |= value.get_BoundEnabled(BoundsMax, AxisX))
          planes.X.Max = new PlaneEquation(origin + max.X * basisX, -basisX);

        if (clipped |= value.get_BoundEnabled(BoundsMin, AxisY))
          planes.Y.Min = new PlaneEquation(origin + min.Y * basisY,  basisY);

        if (clipped |= value.get_BoundEnabled(BoundsMax, AxisY))
          planes.Y.Max = new PlaneEquation(origin + max.Y * basisY, -basisY);

        if (clipped |= value.get_BoundEnabled(BoundsMin, AxisZ))
          planes.Z.Min = new PlaneEquation(origin + min.Z * basisZ,  basisZ);

        if (clipped |= value.get_BoundEnabled(BoundsMax, AxisZ))
          planes.Z.Max = new PlaneEquation(origin + max.Z * basisZ, -basisZ);
      }

      return clipped;
    }
  }

  public static class OutlineExtension
  {
    public static void Deconstruct(this Outline outline, out XYZ min, out XYZ max)
    {
      min = outline.MinimumPoint;
      max = outline.MaximumPoint;
    }

    public static XYZ CenterPoint(this Outline outline)
    {
      var (min, max) = outline;
      return min + ((max - min) * 0.5);
    }
  }
}
