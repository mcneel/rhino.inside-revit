using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  public static class XYZExtension
  {
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
      tolerance = Math.Max(Upsilon, tolerance);

      var length = GetLength(xyz.X, xyz.Y, xyz.Z);
      return length < tolerance ? 0.0 : length;
    }

    internal static double GetLength(double x, double y, double z)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < Upsilon) return 0.0;

      u /= w; v /= w;

      return Math.Sqrt(1.0 + (u * u + v * v)) * w;
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

    public static bool AlmostEquals(this XYZ a, XYZ b, double tolerance)
    {
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(Upsilon, tolerance);

      return GetLength(a.X - b.X, a.Y - b.Y, a.Z - b.Z) < tolerance;
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
      tolerance = Math.Max(Upsilon, tolerance);

      var length = GetLength(xyz.X, xyz.Y, xyz.Z);
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
      tolerance = Math.Max(Upsilon, tolerance);

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
      tolerance = Math.Max(Upsilon, tolerance);

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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(Upsilon, tolerance);

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
      // We follow a denormals-are-zero by default
      tolerance = Math.Max(Upsilon, tolerance);

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
      tolerance = Math.Max(Upsilon, tolerance);

      var length = value.GetLength(tolerance);
      if (length < tolerance)
        return XYZ.Zero;

      var normal = new XYZ(value.X / length, value.Y / length, value.Z / length);

      if (new XYZ(normal.X, normal.Y, 0.0).GetLength(tolerance) < tolerance)
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
      tolerance = Math.Max(Delta, tolerance);

      var previous = new XYZ(1.0, 1.0, 1.0);
      var principal = covarianceMatrix.OfVector(previous).Normalize(Upsilon);

      var iterations = 50;
      while (--iterations > 0 && !AlmostEquals(previous, principal, tolerance))
      {
        previous = principal;
        principal = covarianceMatrix.OfVector(previous).Normalize(Upsilon);
      }

      return principal;
    }
  }

  public static class TransformExtension
  {
    public static void SetOrientation
    (
      this Transform transform,
      XYZ origin0, XYZ basisX0, XYZ basisY0, XYZ basisZ0,
      XYZ origin1, XYZ basisX1, XYZ basisY1, XYZ basisZ1
    )
    {
      var t0 = Transform.CreateTranslation(-origin0);

      var f0 = Transform.Identity;
      f0.BasisX = new XYZ(basisX0.X, basisY0.X, basisZ0.X);
      f0.BasisY = new XYZ(basisX0.Y, basisY0.Y, basisZ0.Y);
      f0.BasisZ = new XYZ(basisX0.Z, basisY0.Z, basisZ0.Z);

      var f1 = Transform.Identity;
      f0.BasisX = basisX1;
      f0.BasisY = basisY1;
      f0.BasisZ = basisZ1;

      var t1 = Transform.CreateTranslation( origin1);

      var planeToPlane = t1 *f1 * f0 * t0;

      transform.Origin = planeToPlane.Origin;
      transform.BasisX = planeToPlane.BasisX;
      transform.BasisY = planeToPlane.BasisY;
      transform.BasisZ = planeToPlane.BasisZ;
    }
  }
}
