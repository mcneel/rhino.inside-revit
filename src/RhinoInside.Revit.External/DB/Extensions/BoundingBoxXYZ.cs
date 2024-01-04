using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using Numerical;

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
      var ((minX, minY, minZ), (maxX, maxY, maxZ)) = value;

      var xDecreasing = maxX < minX;
      x = new BoundingInterval
      (
        (minX, value.get_BoundEnabled(BoundsMin, AxisX) ? BoundingValue.Bounding.Enabled : xDecreasing ? BoundingValue.Bounding.DisabledMax : BoundingValue.Bounding.DisabledMin),
        (maxX, value.get_BoundEnabled(BoundsMax, AxisX) ? BoundingValue.Bounding.Enabled : xDecreasing ? BoundingValue.Bounding.DisabledMin : BoundingValue.Bounding.DisabledMax)
      );

      var yDecreasing = maxY < minY;
      y = new BoundingInterval
      (
        (minY, value.get_BoundEnabled(BoundsMin, AxisY) ? BoundingValue.Bounding.Enabled : yDecreasing ? BoundingValue.Bounding.DisabledMax : BoundingValue.Bounding.DisabledMin),
        (maxY, value.get_BoundEnabled(BoundsMax, AxisY) ? BoundingValue.Bounding.Enabled : yDecreasing ? BoundingValue.Bounding.DisabledMin : BoundingValue.Bounding.DisabledMax)
      );

      var zDecreasing = maxZ < minZ;
      z = new BoundingInterval
      (
        (minZ, value.get_BoundEnabled(BoundsMin, AxisZ) ? BoundingValue.Bounding.Enabled : zDecreasing ? BoundingValue.Bounding.DisabledMax : BoundingValue.Bounding.DisabledMin),
        (maxZ, value.get_BoundEnabled(BoundsMax, AxisZ) ? BoundingValue.Bounding.Enabled : zDecreasing ? BoundingValue.Bounding.DisabledMin : BoundingValue.Bounding.DisabledMax)
      );
    }

    public static BoundingBoxXYZ Empty => new BoundingBoxXYZ()
    {
      Enabled = false,
      Min = XYZExtension.MaxValue,
      Max = XYZExtension.MinValue,
    };

    public static bool IsEmpty(this BoundingBoxXYZ value)
    {
      if (!value.IsNegative()) return false;
      if (!value.Enabled) return true;

      for (var dim = AxisX; dim <= AxisZ; ++dim)
      {
        if (value.get_BoundEnabled(BoundsMin, dim) || value.get_BoundEnabled(BoundsMax, dim))
          return false;
      }

      return true;
    }

    public static bool IsNullOrEmpty(this BoundingBoxXYZ value)
    {
      return value?.IsEmpty() != false;
    }

    public static BoundingBoxXYZ Universe => new BoundingBoxXYZ()
    {
      Enabled = false,
      Min = XYZExtension.MinValue,
      Max = XYZExtension.MaxValue,
    };

    public static bool IsUniverse(this BoundingBoxXYZ value)
    {
      if (!value.IsPositive()) return false;
      if (!value.Enabled) return true;

      for (var dim = AxisX; dim <= AxisZ; ++dim)
      {
        if (value.get_BoundEnabled(BoundsMin, dim) || value.get_BoundEnabled(BoundsMax, dim))
          return false;
      }

      return true;
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

    public static bool IsZeroDiagonal(this BoundingBoxXYZ value, double tolerance = Constant.DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, Constant.Upsilon);

      return Math.Abs(GetDiagonal(value)) < tolerance;
    }

    public static double GetDiagonal(this BoundingBoxXYZ value)
    {
      var (min, max) = value;
      var diagonal = max - min;
      var (x, y, z) = diagonal;

      if (x == 0.0 && y == 0.0 && z == 0.0) return 0.0;
      if (x < 0.0 && y < 0.0 && z < 0.0) return -Euclidean.Norm(x, y, z);
      if (x > 0.0 && y > 0.0 && z > 0.0) return +Euclidean.Norm(x, y, z);

      return double.NaN;
    }

    public static double GetLength(this BoundingBoxXYZ value)
    {
      var (X, Y, Z) = value;
      var x = X.Deviation;
      var y = Y.Deviation;
      var z = Z.Deviation;

      if (x == 0.0 && y == 0.0 && z == 0.0) return 0.0;
      if (x < 0.0 && y < 0.0 && z < 0.0) return (x + y + z) * 8.0;
      if (x > 0.0 && y > 0.0 && z > 0.0) return (x + y + z) * 8.0;

      return double.NaN;
    }

    public static double GetArea(this BoundingBoxXYZ value)
    {
      var (X, Y, Z) = value;
      var x = X.Deviation;
      var y = Y.Deviation;
      var z = Z.Deviation;

      if (x == 0.0 && y == 0.0 && z == 0.0) return 0.0;
      if (x < 0.0 && y < 0.0 && z < 0.0) return ((x * y) + (y * z) + (z * x)) * 8.0;
      if (x > 0.0 && y > 0.0 && z > 0.0) return ((x * y) + (y * z) + (z * x)) * 8.0;

      return double.NaN;
    }

    public static double GetVolume(this BoundingBoxXYZ value)
    {
      var (X, Y, Z) = value;
      var x = X.Deviation;
      var y = Y.Deviation;
      var z = Z.Deviation;

      if (x == 0.0 && y == 0.0 && z == 0.0) return 0.0;
      if (x < 0.0 && y < 0.0 && z < 0.0) return (x * y * z) * 8.0;
      if (x > 0.0 && y > 0.0 && z > 0.0) return (x * y * z) * 8.0;

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

      if (value.Enabled)
      {
        for (int bound = BoundsMin; bound <= BoundsMax; ++bound)
          for (int dim = AxisX; dim <= AxisZ; ++dim)
            other.set_BoundEnabled(bound, dim, value.get_BoundEnabled(bound, dim));
      }

      return other;
    }

    public static void CopyFrom(this BoundingBoxXYZ value, BoundingBoxXYZ other)
    {
      value.Enabled = other.Enabled;
      value.Min = other.Min;
      value.Max = other.Max;
      value.Transform = other.Transform;

      if (value.Enabled)
      {
        for (int bound = BoundsMin; bound <= BoundsMax; ++bound)
          for (int dim = AxisX; dim <= AxisZ; ++dim)
            value.set_BoundEnabled(bound, dim, other.get_BoundEnabled(bound, dim));
      }
    }

    public static XYZ Evaluate(this BoundingBoxXYZ value, UnitXYZ xyz)
    {
      if (xyz.IsNaN) return XYZExtension.NaN;

      var (x, y, z) = xyz;
      var (min, max) = value;

      return new XYZ
      (
        Arithmetic.Mix(min.X, max.X, x),
        Arithmetic.Mix(min.Y, max.Y, y),
        Arithmetic.Mix(min.Z, max.Z, z)
      );
    }

    public static void Intersection(this BoundingBoxXYZ value, BoundingBoxXYZ other)
    {
      if (other.IsUniverse() || value.IsEmpty()) return;
      if (other.IsEmpty())
      {
        value.Enabled = false;
        value.Min = XYZExtension.MaxValue;
        value.Max = XYZExtension.MinValue;
        return;
      }

      var A = value.Clone();
      A.Intersection(other.GetCorners());
      var a = A.GetVolume();

      var B = other.Clone();
      B.Intersection(value.GetCorners());
      var b = B.GetVolume();

      value.CopyFrom(a < b ? A : B);
    }

    public static void Intersection(this BoundingBoxXYZ value, params XYZ[] xyz)
    {
      if (xyz.Length > 0)
      {
        using (var inverse = value.Transform.Inverse)
        {
          var minX = double.PositiveInfinity; var minY = double.PositiveInfinity; var minZ = double.PositiveInfinity;
          var maxX = double.NegativeInfinity; var maxY = double.NegativeInfinity; var maxZ = double.NegativeInfinity;

          for (int p = 0; p < xyz.Length; p++)
          {
            var (x, y, z) = inverse.OfPoint(xyz[p]);
            minX = Arithmetic.Min(minX, x); maxX = Arithmetic.Max(maxX, x);
            minY = Arithmetic.Min(minY, y); maxY = Arithmetic.Max(maxY, y);
            minZ = Arithmetic.Min(minZ, z); maxZ = Arithmetic.Max(maxZ, z);
          }

          var (X, Y, Z) = value;
          X &= (minX, maxX);
          Y &= (minY, maxY);
          Z &= (minZ, maxZ);

          value.Min = new XYZ(X.Left, Y.Left, Z.Left);
          value.Max = new XYZ(X.Right, Y.Right, Z.Right);

          var enabled = false;
          value.Enabled = true;
          value.set_BoundEnabled(BoundsMin, AxisX, enabled |= X.Left.IsEnabled);
          value.set_BoundEnabled(BoundsMax, AxisX, enabled |= X.Right.IsEnabled);
          value.set_BoundEnabled(BoundsMin, AxisY, enabled |= Y.Left.IsEnabled);
          value.set_BoundEnabled(BoundsMax, AxisY, enabled |= Y.Right.IsEnabled);
          value.set_BoundEnabled(BoundsMin, AxisZ, enabled |= Z.Left.IsEnabled);
          value.set_BoundEnabled(BoundsMax, AxisZ, enabled |= Z.Right.IsEnabled);
          value.Enabled = enabled;
        }
      }
    }

    public static void Union(this BoundingBoxXYZ value, BoundingBoxXYZ other)
    {
      if (other.IsEmpty() || value.IsUniverse()) return;
      if (other.IsUniverse())
      {
        value.Enabled = false;
        value.Min = XYZExtension.MinValue;
        value.Max = XYZExtension.MaxValue;
        return;
      }

      var A = value.Clone();
      A.Union(other.GetCorners());
      var a = A.GetVolume();

      var B = other.Clone();
      B.Union(value.GetCorners());
      var b = B.GetVolume();

      value.CopyFrom(a < b ? A : B);
    }

    public static void Union(this BoundingBoxXYZ value, params XYZ[] xyz)
    {
      if (xyz.Length > 0)
      {
        using (var inverse = value.Transform.Inverse)
        {
          var minX = double.PositiveInfinity; var minY = double.PositiveInfinity; var minZ = double.PositiveInfinity;
          var maxX = double.NegativeInfinity; var maxY = double.NegativeInfinity; var maxZ = double.NegativeInfinity;

          for (int p = 0; p < xyz.Length; p++)
          {
            var (x, y, z) = inverse.OfPoint(xyz[p]);
            minX = Arithmetic.Min(minX, x); maxX = Arithmetic.Max(maxX, x);
            minY = Arithmetic.Min(minY, y); maxY = Arithmetic.Max(maxY, y);
            minZ = Arithmetic.Min(minZ, z); maxZ = Arithmetic.Max(maxZ, z);
          }

          var (X, Y, Z) = value;
          X |= (minX, maxX);
          Y |= (minY, maxY);
          Z |= (minZ, maxZ);

          value.Min = new XYZ(X.Left, Y.Left, Z.Left);
          value.Max = new XYZ(X.Right, Y.Right, Z.Right);

          var enabled = false;
          value.Enabled = true;
          value.set_BoundEnabled(BoundsMin, AxisX, enabled |= X.Left.IsEnabled);
          value.set_BoundEnabled(BoundsMax, AxisX, enabled |= X.Right.IsEnabled);
          value.set_BoundEnabled(BoundsMin, AxisY, enabled |= Y.Left.IsEnabled);
          value.set_BoundEnabled(BoundsMax, AxisY, enabled |= Y.Right.IsEnabled);
          value.set_BoundEnabled(BoundsMin, AxisZ, enabled |= Z.Left.IsEnabled);
          value.set_BoundEnabled(BoundsMax, AxisZ, enabled |= Z.Right.IsEnabled);
          value.Enabled = enabled;
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
        min = (Arithmetic.Min(min.X, x), Arithmetic.Min(min.Y, y), Arithmetic.Min(min.Z, z));
        max = (Arithmetic.Max(max.X, x), Arithmetic.Max(max.Y, y), Arithmetic.Max(max.Z, z));
      }

      return new Outline
      (
        new XYZ(min.X, min.Y, min.Z),
        new XYZ(max.X, max.Y, max.Z)
      );
    }

    public static BoundingBoxUV ToBoundingBoxUV(this BoundingBoxXYZ value, int axis = AxisZ)
    {
      var (min, max) = value;
      switch (axis)
      {
        case AxisX: return new BoundingBoxUV(min.Y, min.Z, max.Y, max.Z);
        case AxisY: return new BoundingBoxUV(min.Z, min.X, max.Z, max.X);
        case AxisZ: return new BoundingBoxUV(min.X, min.Y, max.X, max.Y);
        default: throw new ArgumentOutOfRangeException(nameof(axis));
      }
    }

    public static XYZ GetCenter(this BoundingBoxXYZ value)
    {
      using (var transform = value.Transform)
      {
        var (min, max) = value;
        return (transform.OfPoint(min) * 0.5) + (transform.OfPoint(max) * 0.5);
      }
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

      if (value.Enabled)
      {
        using (var transform = value.Transform)
        {
          if (!transform.IsConformal)
            throw new ArgumentException("Transform is not conformal", nameof(value));

          var (origin, basisX, basisY, basisZ) = transform;
          var (min, max) = value;

          if (value.get_BoundEnabled(BoundsMin, AxisX))
          {
            clipped = true;
            planes.X.Min = new PlaneEquation(origin + min.X * basisX, (UnitXYZ) basisX);
          }

          if (value.get_BoundEnabled(BoundsMax, AxisX))
          {
            clipped = true;
            planes.X.Max = new PlaneEquation(origin + max.X * basisX, (UnitXYZ) (-basisX));
          }

          if (value.get_BoundEnabled(BoundsMin, AxisY))
          {
            clipped = true;
            planes.Y.Min = new PlaneEquation(origin + min.Y * basisY, (UnitXYZ) basisY);
          }

          if (value.get_BoundEnabled(BoundsMax, AxisY))
          {
            clipped = true;
            planes.Y.Max = new PlaneEquation(origin + max.Y * basisY, (UnitXYZ) (-basisY));
          }

          if (value.get_BoundEnabled(BoundsMin, AxisZ))
          {
            clipped = true;
            planes.Z.Min = new PlaneEquation(origin + min.Z * basisZ, (UnitXYZ) basisZ);
          }

          if (value.get_BoundEnabled(BoundsMax, AxisZ))
          {
            clipped = true;
            planes.Z.Max = new PlaneEquation(origin + max.Z * basisZ, (UnitXYZ) (-basisZ));
          }
        }
      }

      return clipped;
    }

    internal static bool GetPlaneEquations
    (
      this BoundingBoxXYZ value,
      out
      (
        (PlaneEquation? Min, PlaneEquation? Max) X,
        (PlaneEquation? Min, PlaneEquation? Max) Y,
        (PlaneEquation? Min, PlaneEquation? Max) Z
      ) planes,
      double offset
    )
    {
      if (GetPlaneEquations(value, out planes))
      {
        if (planes.X.Min.HasValue) planes.X.Min = new PlaneEquation(planes.X.Min.Value.Normal, planes.X.Min.Value.Offset - offset);
        if (planes.X.Max.HasValue) planes.X.Max = new PlaneEquation(planes.X.Max.Value.Normal, planes.X.Max.Value.Offset + offset);

        if (planes.Y.Min.HasValue) planes.Y.Min = new PlaneEquation(planes.Y.Min.Value.Normal, planes.Y.Min.Value.Offset - offset);
        if (planes.Y.Max.HasValue) planes.Y.Max = new PlaneEquation(planes.Y.Max.Value.Normal, planes.Y.Max.Value.Offset + offset);

        if (planes.Z.Min.HasValue) planes.Z.Min = new PlaneEquation(planes.Z.Min.Value.Normal, planes.Z.Min.Value.Offset - offset);
        if (planes.Z.Max.HasValue) planes.Z.Max = new PlaneEquation(planes.Z.Max.Value.Normal, planes.Z.Max.Value.Offset + offset);

        return true;
      }

      return false;
    }

    public static bool IsInside(this BoundingBoxXYZ value, XYZ point)
    {
      var (min, max, transform, bounded) = value;
      var (x, y, z) = transform.Inverse.OfPoint(point);

      if (bounded[BoundsMin, AxisX] && x < min.X) return false;
      if (bounded[BoundsMax, AxisX] && x > max.X) return false;
      if (bounded[BoundsMin, AxisY] && y < min.X) return false;
      if (bounded[BoundsMax, AxisY] && y > max.X) return false;
      if (bounded[BoundsMin, AxisZ] && z < min.X) return false;
      if (bounded[BoundsMax, AxisZ] && z > max.X) return false;

      return true;
    }
  }

  public static class OutlineExtension
  {
    public static Outline Empty => new Outline(XYZExtension.MaxValue, XYZExtension.MinValue);

    public static void Deconstruct(this Outline outline, out XYZ min, out XYZ max)
    {
      min = outline.MinimumPoint;
      max = outline.MaximumPoint;
    }

    public static XYZ CenterPoint(this Outline outline)
    {
      var (min, max) = outline;
      return (min * 0.5) + (max * 0.5);
    }
  }
}
