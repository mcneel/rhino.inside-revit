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

    public static bool IsUnset(this BoundingBoxXYZ value)
    {
      var ((minX, minY, minZ), (maxX, maxY, maxZ)) = value;
      return !(value is object && minX <= maxX && minY <= maxY && minZ <= maxZ);
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

      Union(value, xyz.Min);
      Union(value, xyz.Max);
    }

    public static void Union(this BoundingBoxXYZ value, XYZ xyz)
    {
      var (x, y, z) = xyz;
      var (min, max) = value;

      value.Min = new XYZ(Math.Min(min.X, x), Math.Min(min.Y, y), Math.Min(min.Z, z));
      value.Max = new XYZ(Math.Max(max.X, x), Math.Max(max.Y, y), Math.Max(max.Z, z));
    }

    public static Outline ToOutLine(this BoundingBoxXYZ value) =>
      new Outline(value.Transform.OfPoint(value.Min), value.Transform.OfPoint(value.Max));

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
