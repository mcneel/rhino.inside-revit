using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  public static class XYZExtension
  {
    public static XYZ NaN { get; } = null; // new XYZ(double.NaN, double.NaN, double.NaN);
    public static XYZ Zero { get; } = XYZ.Zero;
    public static XYZ BasisX { get; } = XYZ.BasisX;
    public static XYZ BasisY { get; } = XYZ.BasisY;
    public static XYZ BasisZ { get; } = XYZ.BasisZ;

    //public static XYZ NegativeInfinity { get; } = new XYZ(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
    //public static XYZ PositiveInfinity { get; } = new XYZ(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

    public static XYZ MinValue { get; } = new XYZ(double.MinValue, double.MinValue, double.MinValue);
    public static XYZ MaxValue { get; } = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);

    public static void Deconstruct
    (
      this XYZ value,
      out double x, out double y, out double z
    )
    {
      x = value.X;
      y = value.Y;
      z = value.Z;
    }

    internal static bool IsZeroLength(double x, double y, double z, double tolerance)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < (0.0 + tolerance) / 3.0) return true;
      if (w > (0.0 + tolerance)      ) return false;

      u /= w; v /= w;

      return Math.Sqrt(1.0 + (u * u + v * v)) * w < tolerance;
    }

    internal static bool IsUnitLength(double x, double y, double z, double tolerance)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < (1.0 - tolerance) / 3.0) return false;
      if (w > (1.0 + tolerance)      ) return false;

      u /= w; v /= w;

      return Math.Sqrt(1.0 + (u * u + v * v)) * w - 1.0 < tolerance;
    }

    /// <summary>
    /// Gets the length of this vector.
    /// </summary>
    /// <remarks>
    /// In 3-D Euclidean space, the length of the vector is the square root of the sum
    /// of the three coordinates squared.
    /// </remarks>
    /// <param name="xyz"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static double GetLength(this XYZ xyz, double tolerance = DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, NumericTolerance.Upsilon);

      var length = NumericTolerance.Norm(xyz.X, xyz.Y, xyz.Z);
      return length < tolerance ? 0.0 : length;
    }

    public static bool AlmostEquals(this XYZ a, XYZ b, double tolerance)
    {
      tolerance = Math.Max(tolerance, Upsilon);

      return NumericTolerance.Norm(a.X - b.X, a.Y - b.Y, a.Z - b.Z) < tolerance;
    }

    /// <summary>
    /// Returns a new XYZ whose coordinates are the normalized values from this vector.
    /// </summary>
    /// <remarks>
    /// Normalized indicates that the length of this vector equals one (a unit vector).
    /// </remarks>
    /// <param name="xyz"></param>
    /// <param name="tolerance"></param>
    /// <returns>The normalized XYZ or zero if the vector is almost Zero.</returns>
    public static XYZ Normalize(this XYZ xyz, double tolerance = DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, Upsilon);

      var (x, y, z) = xyz;
      var length = NumericTolerance.Norm(x, y, z);
      if (length < tolerance)
        return Zero;

      return new XYZ(x / length, y / length, z / length);
    }

    /// <summary>
    /// The cross product of vector <paramref name="a"/> and vector <paramref name="b"/>.
    /// </summary>
    /// <remarks>
    /// The cross product is defined as the vector which is perpendicular to both vectors
    /// with a magnitude equal to the area of the parallelogram they span.
    /// Also known as vector product or outer product.
    /// </remarks>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance">Tolerance value to check if input vectors are zero length.</param>
    /// <returns>The vector equal to the cross product.</returns>
    public static XYZ CrossProduct(this XYZ a, XYZ b, double tolerance = DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, Upsilon);

      var (aX, aY, aZ) = a;
      var lengthA = NumericTolerance.Norm(aX, aY, aZ);
      if (lengthA < tolerance)
        return Zero;

      var (bX, bY, bZ) = b;
      var lengthB = NumericTolerance.Norm(bX, bY, bZ);
      if (lengthB < tolerance)
        return Zero;

      // Normalize a and b
      aX /= lengthA; aY /= lengthA; aZ /= lengthA;
      bX /= lengthB; bY /= lengthB; bZ /= lengthB;

      // Compute CrossProduct of normalized vectors
      var x = aY * bZ - aZ * bY;
      var y = aZ * bX - aX * bZ;
      var z = aX * bY - aY * bX;

      // Scale result back to be lengthA * lengthB * sin(𝛼) in magnitude
      var lengthAB = lengthA * lengthB;
      return new XYZ(x * lengthAB, y * lengthAB, z * lengthAB);
    }

    /// <summary>
    /// Checks if the the given two vectors are parallel
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="a"/> and <paramref name="b"/> are parallel</returns>
    public static bool IsParallelTo(this XYZ a, XYZ b, double tolerance = DefaultTolerance)
    {
      var A = a.Normalize(tolerance);
      var B = b.Normalize(tolerance);

      return AlmostEquals(A, A.DotProduct(B) < 0.0 ? -B : B, tolerance);
    }

    /// <summary>
    /// Checks if the the given two vectors are codirectional
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="a"/> and <paramref name="b"/> are codirectional</returns>
    public static bool IsCodirectionalTo(this XYZ a, XYZ b, double tolerance = DefaultTolerance)
    {
      var A = a.Normalize(tolerance);
      var B = b.Normalize(tolerance);

      return AlmostEquals(A, B, tolerance);
    }

    /// <summary>
    /// Checks if the the given two vectors are perpendicular
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="a"/> and <paramref name="b"/> are perpendicular</returns>
    public static bool IsPerpendicularTo(this XYZ a, XYZ b, double tolerance = DefaultTolerance)
    {
      var A = a.Normalize(tolerance);
      var B = b.Normalize(tolerance);

      tolerance = Math.Max(tolerance, Upsilon);
      return NumericTolerance.Norm(A.DotProduct(B)) < tolerance;
    }

    /// <summary>
    /// Arbitrary Axis Algorithm
    /// <para>Given a vector to be used as the Z axis of a coordinate system, this algorithm generates a corresponding X axis for the coordinate system.</para>
    /// <para>The Y axis follows by application of the right-hand rule.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="tolerance"></param>
    /// <returns>X axis of the corresponding coordinate system</returns>
    public static XYZ PerpVector(this XYZ value, double tolerance = DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, Upsilon);

      var (x, y, z) = value;
      var length = NumericTolerance.Norm(x, y, z);
      if (length < tolerance)
        return Zero;

      return NumericTolerance.Norm(x / length, y / length) < tolerance ?
        new XYZ(z, 0.0, -x) :
        new XYZ(-y, x, 0.0);
    }

    /// <summary>
    /// Retrieves a box that circumscribes the point set.
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static bool TryGetBoundingBox(IEnumerable<XYZ> points, out BoundingBoxXYZ bbox)
    {
      double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
      double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;

      foreach (var point in points)
      {
        var (x, y, z) = point;

        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
        minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
        minZ = Math.Min(minZ, z); maxZ = Math.Max(maxZ, z);
      }

      if (minX <= maxX && minY <= maxY && minZ <= maxZ)
      {
        bbox = new BoundingBoxXYZ()
        {
          Min = new XYZ(minX, minY, minZ),
          Max = new XYZ(maxX, maxY, maxZ)
        };
        return true;
      }

      bbox = default;
      return false;
    }

    /// <summary>
    /// Retrieves a box that circumscribes the point set.
    /// </summary>
    /// <param name="points"></param>
    /// <param name="coordSystem"></param>
    /// <returns></returns>
    public static bool TryGetBoundingBox(IEnumerable<XYZ> points, out BoundingBoxXYZ bbox, Transform coordSystem)
    {
      if (coordSystem is null || coordSystem.IsIdentity)
        return TryGetBoundingBox(points, out bbox);

      if (!coordSystem.IsConformal)
        throw new ArgumentException("Transform is not conformal", nameof(coordSystem));

      if (!coordSystem.TryGetInverse(out var inverse))
      {
        bbox = default;
        return false;
      }

      using (inverse)
      {
        if (TryGetBoundingBox(points.Select(inverse.OfPoint), out bbox))
        {
          bbox.Transform = coordSystem;
          return true;
        }
      }

      bbox = default;
      return false;
    }

    /// <summary>
    /// Computes the mean point of a collection of XYZ points.
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    public static XYZ ComputeMeanPoint(IEnumerable<XYZ> points)
    {
      Sum meanX = default, meanY = default, meanZ = default;
      var numPoints = 0;

      foreach(var point in points)
      {
        numPoints++;
        meanX.Add(point.X);
        meanY.Add(point.Y);
        meanZ.Add(point.Z);
      }

      return new XYZ(meanX.Value / numPoints, meanY.Value / numPoints, meanZ.Value / numPoints);
    }
  }
}
