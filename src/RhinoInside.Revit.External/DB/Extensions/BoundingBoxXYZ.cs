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
      Min = XYZExtension.PositiveInfinity,
      Max = XYZExtension.NegativeInfinity,
    };

    public static BoundingBoxXYZ All => new BoundingBoxXYZ()
    {
      Min = XYZExtension.NegativeInfinity,
      Max = XYZExtension.PositiveInfinity,
    };

    public static bool IsUnset(this BoundingBoxXYZ value)
    {
      if (value is null) return true;

      var (min, max) = value;
      return !(min.X <= max.X && min.Y <= max.Y && min.Z <= max.Z);
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
      if (value.IsUnset()) return default;
      if (xyz is null) return default;

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
      if (xyz.IsUnset()) return;
      if (value.IsUnset())
      {
        value.Transform = xyz.Transform;
        value.Min = xyz.Min;
        value.Max = xyz.Max;
      }
      else
      {
        using (var transform = value.Transform)
        {
          foreach (var corner in xyz.GetLocalCorners())
            UnionLocal(value, transform.OfPoint(corner));
        }
      }
    }

    public static void Union(this BoundingBoxXYZ value, params XYZ[] xyz)
    {
      var inverse = value.Transform.Inverse;
      for(int i = 0; i < xyz.Length; i++)
        UnionLocal(value, inverse.OfPoint(xyz[i]));
    }

    static void UnionLocal(this BoundingBoxXYZ value, XYZ xyz)
    {
      if (value.IsUnset())
      {
        value.Min = xyz;
        value.Max = xyz;
      }
      else
      {
        var (x, y, z) = xyz;
        var (min, max) = value;

        value.Min = new XYZ(Math.Min(min.X, x), Math.Min(min.Y, y), Math.Min(min.Z, z));
        value.Max = new XYZ(Math.Max(max.X, x), Math.Max(max.Y, y), Math.Max(max.Z, z));
      }
    }

    static XYZ[] GetLocalCorners(this BoundingBoxXYZ value)
    {
      var (min, max) = value;

      return new XYZ[]
      {
        new XYZ(min.X, min.Y, min.Z),
        new XYZ(max.X, min.Y, min.Z),
        new XYZ(max.X, max.Y, min.Z),
        new XYZ(min.X, max.Y, min.Z),
        new XYZ(min.X, min.Y, max.Z),
        new XYZ(max.X, min.Y, max.Z),
        new XYZ(max.X, max.Y, max.Z),
        new XYZ(min.X, max.Y, max.Z)
      };
    }

    static (XYZ Min, XYZ Max) GetOutline(params XYZ[] points)
    {
      (double X, double Y, double Z) min = (double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
      (double X, double Y, double Z) max = (double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

      foreach (var point in points)
      {
        var (x, y, z) = point;
        min = (Math.Min(min.X, x), Math.Min(min.Y, y), Math.Min(min.Z, z));
        max = (Math.Min(max.X, x), Math.Min(max.Y, y), Math.Min(max.Z, z));
      }

      return
      (
        new XYZ(min.X, min.Y, min.Z),
        new XYZ(max.X, max.Y, max.Z)
      );
    }

    public static Outline ToOutLine(this BoundingBoxXYZ value)
    {
      using (var transform = value.Transform)
      {
        var corners = value.GetLocalCorners();
        for (int c = 0; c < corners.Length; ++c)
          corners[c] = transform.OfPoint(corners[c]);

        var (min, max) = GetOutline(corners);
        return new Outline(min, max);
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
