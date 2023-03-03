using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  public static class XYZExtension
  {
    public static XYZ NaN    { get; } = null; // new XYZ(double.NaN, double.NaN, double.NaN);
    public static XYZ Zero   { get; } = XYZ.Zero;
    public static XYZ One    { get; } = new XYZ(1.0, 1.0, 1.0);

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
      if (value is null)
      {
        x = double.NaN;
        y = double.NaN;
        z = double.NaN;
      }
      else
      {
        x = value.X;
        y = value.Y;
        z = value.Z;
      }
    }

    /// <summary>
    /// The boolean value that indicates whether this vector is a zero vector.
    /// </summary>
    /// <param name="xyz"></param>
    /// <param name="tolerance"></param>
    /// <returns>The vector's length is 0.0 within the <paramref name="tolerance"/>.</returns>
    public static bool IsZeroLength(this XYZ xyz, double tolerance)
    {
      tolerance = Math.Max(tolerance, NumericTolerance.ZeroDelta);

      return NumericTolerance.IsZero3(xyz.X, xyz.Y, xyz.Z, tolerance);
    }

    /// <summary>
    /// The boolean value that indicates whether this vector is of unit length.
    /// </summary>
    /// <remarks>
    /// A unit length vector has a length of 1.0 and is considered normalized.
    /// </remarks>
    /// <param name="xyz"></param>
    /// <param name="tolerance"></param>
    /// <returns>The vector's length is 1.0 within the <paramref name="tolerance"/>.</returns>
    public static bool IsUnitLength(this XYZ xyz, double tolerance)
    {
      tolerance = Math.Max(tolerance, NumericTolerance.SqrtDelta);

      return NumericTolerance.IsUnit3(xyz.X, xyz.Y, xyz.Z, tolerance);
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

    public static bool AlmostEquals(this XYZ a, XYZ b, double tolerance = DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, NumericTolerance.Upsilon);

      return NumericTolerance.IsZero3(a.X - b.X, a.Y - b.Y, a.Z - b.Z, tolerance);
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
    /// The cross product of vector <paramref name="x"/> and vector <paramref name="y"/>.
    /// </summary>
    /// <remarks>
    /// The cross product is defined as the vector which is perpendicular to both vectors
    /// with a magnitude equal to the area of the parallelogram they span.
    /// Also known as vector product or outer product.
    /// </remarks>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance">Tolerance value to check if input vectors are zero length.</param>
    /// <returns>The vector equal to x ⨯ y.</returns>
    /// <seealso cref="https://en.wikipedia.org/wiki/Cross_product"/>
    public static XYZ CrossProduct(XYZ x, XYZ y)
    {
      var (xX, xY, xZ) = x;
      var xLength = NumericTolerance.Norm(xX, xY, xZ);
      if (xLength < Upsilon)
        return Zero;

      var (yX, yY, yZ) = y;
      var yLength = NumericTolerance.Norm(yX, yY, yZ);
      if (yLength < Upsilon)
        return Zero;

      // Normalize a and b
      xX /= xLength; xY /= xLength; xZ /= xLength;
      yX /= yLength; yY /= yLength; yZ /= yLength;

      // Compute CrossProduct of normalized vectors
      var zX = xY * yZ - xZ * yY;
      var zY = xZ * yX - xX * yZ;
      var zZ = xX * yY - xY * yX;

      // Scale result back to be xLength * yLength * sin(𝛼) in magnitude
      var xyLength = xLength * yLength;
      return new XYZ(zX * xyLength, zY * xyLength, zZ * xyLength);
    }

    /// <summary>
    /// The dot product of of vector <paramref name="x"/> and vector <paramref name="y"/>.
    /// </summary>
    /// <remarks>
    /// Geometrically equal to the cosinus of the angle span between a and b times |a| ⋅ |b|.
    /// </remarks>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance"></param>
    /// <returns>The scalar equal to x ⋅ y.</returns>
    /// <seealso cref="https://en.wikipedia.org/wiki/Dot_product"/>
    public static double DotProduct(XYZ x, XYZ y)
    {
      var (aX, aY, aZ) = x;
      var (bX, bY, bZ) = y;

      return aX * bX + aY * bY + aZ * bZ;
    }

    /// <summary>
    /// The triple product of of vector <paramref name="a"/>, vector <paramref name="b"/> and vector <paramref name="c"/>.
    /// </summary>
    /// <remarks>
    /// Geometrically equal to the signed volume of the parallelepiped formed by the three vectors.
    /// </remarks>
    /// <param name="z"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>The scalar equal to z ⋅ (x ⨯ y).</returns>
    /// <seealso cref="https://en.wikipedia.org/wiki/Triple_product"/>
    public static double TripleProduct(XYZ x, XYZ y, XYZ z)
    {
      return DotProduct(z, CrossProduct(x, y));
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
      var A = UnitXYZ.Unitize(a);
      var B = UnitXYZ.Unitize(b);

      return A.IsParallelTo(B, tolerance);
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
      var A = UnitXYZ.Unitize(a);
      var B = UnitXYZ.Unitize(b);

      return A.IsCodirectionalTo(B, tolerance);
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
      var A = UnitXYZ.Unitize(a);
      var B = UnitXYZ.Unitize(b);

      return A.IsPerpendicularTo(B, tolerance);
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

      var norm = NumericTolerance.Norm(x, y, z);
      if (norm == 0.0) return Zero;
      x /= norm; y /= norm; z /= norm;

      if (NumericTolerance.IsZero2(x, y, tolerance))
      {
        NumericTolerance.Unitize2(ref x, ref z);
        return new XYZ(z * norm, 0.0, -x * norm);
      }
      else
      {
        NumericTolerance.Unitize2(ref x, ref y);
        return new XYZ(-y * norm, x * norm, 0.0);
      }
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
