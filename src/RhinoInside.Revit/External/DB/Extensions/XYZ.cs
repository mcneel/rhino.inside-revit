using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  /// <summary>
  /// This class represents a Summation ∑ of floating points values.
  /// </summary>
  /// <remarks>
  /// Implemented using Neumaier summation algorithm.
  /// </remarks>
  struct Sum
  {
    double sum; // An accumulator.
    double c;   // A running compensation for lost low-order bits.

    public double Value => sum + c;

    public Sum(double value, double c = 0.0) { sum = value; this.c = c; }

    public static Sum operator +(Sum sum, double value) { sum.Add(+value); return sum; }
    public static Sum operator -(Sum sum, double value) { sum.Add(-value); return sum; }

    public void Add(double value)
    {
      var t = sum + value;
      if (Math.Abs(sum) < Math.Abs(value))
        c += (value - t) + sum;
      else
        c += (sum - t) + value;
      sum = t;
    }

    public void Add(params double[] values)
    {
      for(int v = 0; v < values.Length; ++v)
        Add(values[v]);
    }
  }

  public static class XYZExtension
  {
    /// <summary>
    /// Tolerance value that represents the minimum supported tolerance.
    /// </summary>
    public const double MinTolerance = 0D;

    /// <summary>
    /// Tolerance value used when tolerance parameter is omited.
    /// </summary>
    public const double DefaultTolerance = 1e-9;

    /// <summary>
    /// The smallest number such that 1.0 + Precision != 1.0
    /// </summary>
    /// <remarks>
    /// Same as DBL_EPSILON 2.2204460492503131e-16
    /// </remarks>
    public const double Precision = double.MaxValue * double.Epsilon / 4.0;

    /// <summary>
    /// The smallest positive normalized, finite representable value of type double.
    /// </summary>
    /// <remarks>
    /// Same as +DBL_MIN +2.2250738585072014e-308 
    /// </remarks>
    public const double DenormalUpperBound = 4.0 / double.MaxValue;

    /// <summary>
    /// The biggest negative normalized, finite representable value of type double.
    /// </summary>
    /// <remarks>
    /// Same as -DBL_MIN -2.2250738585072014e-308 
    /// </remarks>
    public const double DenormalLowerBound = 4.0 / double.MinValue;

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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(DenormalUpperBound, tolerance);

      return GetLength(xyz.X, xyz.Y, xyz.Z, tolerance);
    }

    static double GetLength(double x, double y, double z, double tolerance)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < tolerance) return 0.0;

      u /= w; v /= w;

      return Math.Sqrt(1.0 + (u * u + v * v)) * w;
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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(DenormalUpperBound, tolerance);

      var length = GetLength(xyz, tolerance);
      if (length < tolerance)
        return XYZ.Zero;

      return new XYZ(xyz.X / length, xyz.Y / length, xyz.Z / length);
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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(DenormalUpperBound, tolerance);

      var lengthA = a.GetLength(tolerance);
      var lengthB = b.GetLength(tolerance);

      if (lengthA < tolerance || lengthB < tolerance)
        return XYZ.Zero;

      // Normalize a and b
      double aX = a.X / lengthA, aY = a.Y / lengthA, aZ = a.Z / lengthA;
      double bX = b.X / lengthB, bY = b.Y / lengthB, bZ = b.Z / lengthB;

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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(DenormalUpperBound, tolerance);

      var A = a.Normalize(tolerance);
      var B = b.Normalize(tolerance);

      return A.IsAlmostEqualTo(A.DotProduct(B) < 0.0 ? -B : B, tolerance);
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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(DenormalUpperBound, tolerance);

      var A = a.Normalize(tolerance);
      var B = b.Normalize(tolerance);

      return A.IsAlmostEqualTo(B, tolerance);
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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(DenormalUpperBound, tolerance);

      var A = a.Normalize(tolerance);
      var B = b.Normalize(tolerance);

      return A.DotProduct(B) < tolerance;
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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(DenormalUpperBound, tolerance);

      var length = value.GetLength(tolerance);
      if (length < tolerance)
        return XYZ.Zero;

      var normal = new XYZ(value.X / length, value.Y / length, value.Z / length);

      if (XYZ.Zero.IsAlmostEqualTo(new XYZ(normal.X, normal.Y, 0.0), tolerance))
        return new XYZ(value.Z, 0.0, -value.X);
      else
        return new XYZ(-value.Y, value.X, 0.0);
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

    /// <summary>
    /// Computes a covariance matrix out of a collection of XYZ points.
    /// </summary>
    /// <param name="points"></param>
    /// <param name="meanPoint"></param>
    /// <returns></returns>
    internal static Transform ComputeCovariance(IEnumerable<XYZ> points, XYZ meanPoint = default)
    {
      if (meanPoint is null) meanPoint = ComputeMeanPoint(points);

      double x = meanPoint.X, y = meanPoint.Y, z = meanPoint.Z;
      Sum covXx = default, covXy = default, covXz = default;
      Sum covYx = default, covYy = default, covYz = default;
      Sum covZx = default, covZy = default, covZz = default;

      foreach (var loc in points)
      {
        // Translate loc relative to centroid
        double locX = loc.X - x, locY = loc.Y - y, locZ = loc.Z - z;

        covXx.Add(locX * locX);
        covXy.Add(locX * locY);
        covXz.Add(locX * locZ);

        covYx.Add(locY * locX);
        covYy.Add(locY * locY);
        covYz.Add(locY * locZ);

        covZx.Add(locZ * locX);
        covZy.Add(locZ * locY);
        covZz.Add(locZ * locZ);
      }

      var cov = Transform.Identity;
      cov.BasisX = new XYZ(covXx.Value, covXy.Value, covXz.Value);
      cov.BasisY = new XYZ(covYx.Value, covYy.Value, covYz.Value);
      cov.BasisZ = new XYZ(covZx.Value, covZy.Value, covZz.Value);

      return cov;
    }

    public static bool TryGetInverse(this Transform transform, out Transform inverse)
    {
      if (DefaultTolerance < transform.Determinant)
      {
        try { inverse = transform.Inverse; return true; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
      }

      inverse = Transform.Identity;
      return false;
    }

    /// <summary>
    /// Computes the principal component of a covariance matrix.
    /// </summary>
    /// <param name="covarianceMatrix"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    internal static XYZ GetPrincipalComponent(this Transform covarianceMatrix, double tolerance = DefaultTolerance)
    {
      tolerance = Math.Max(Precision, tolerance);

      var previous = new XYZ(1.0, 1.0, 1.0);
      var principal = covarianceMatrix.OfVector(previous).Normalize(DenormalUpperBound);

      var iterations = 50;
      while (--iterations > 0 && !previous.IsAlmostEqualTo(principal, tolerance))
      {
        previous = principal;
        principal = covarianceMatrix.OfVector(previous).Normalize(DenormalUpperBound);
      }

      return principal;
    }
  }
}
