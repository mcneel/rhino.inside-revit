using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  public static class TransformExtension
  {
    public static void Deconstruct
    (
      this Transform transform,
      out XYZ origin, out XYZ basisX, out XYZ basisY, out XYZ basisZ
    )
    {
      origin = transform.Origin;
      basisX = transform.BasisX;
      basisY = transform.BasisY;
      basisZ = transform.BasisZ;
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

    public static void GetCoordSystem
    (
      this Transform transform,
      out XYZ origin, out XYZ basisX, out XYZ basisY, out XYZ basisZ
    )
    {
      origin = transform.Origin;
      basisX = transform.BasisX;
      basisY = transform.BasisY;
      basisZ = transform.BasisZ;
    }

    public static void SetCoordSystem
    (
      this Transform transform,
      XYZ origin, XYZ basisX, XYZ basisY, XYZ basisZ
    )
    {
      transform.Origin = origin;
      transform.BasisX = basisX;
      transform.BasisY = basisY;
      transform.BasisZ = basisZ;
    }

    public static void SetAlignCoordSystem
    (
      this Transform transform,
      XYZ origin0, XYZ basisX0, XYZ basisY0, XYZ basisZ0,
      XYZ origin1, XYZ basisX1, XYZ basisY1, XYZ basisZ1
    )
    {
      var from = Transform.Identity;
      from.BasisX = new XYZ(basisX0.X, basisY0.X, basisZ0.X);
      from.BasisY = new XYZ(basisX0.Y, basisY0.Y, basisZ0.Y);
      from.BasisZ = new XYZ(basisX0.Z, basisY0.Z, basisZ0.Z);
      from.Origin = from.OfPoint(-origin0);

      var to = Transform.Identity;
      to.BasisX = basisX1;
      to.BasisY = basisY1;
      to.BasisZ = basisZ1;
      to.Origin = origin1;

      var planeToPlane = to * from;

      transform.Origin = planeToPlane.Origin;
      transform.BasisX = planeToPlane.BasisX;
      transform.BasisY = planeToPlane.BasisY;
      transform.BasisZ = planeToPlane.BasisZ;
    }

    /// <summary>
    /// Computes a covariance matrix out of a collection of XYZ points.
    /// </summary>
    /// <param name="points"></param>
    /// <param name="meanPoint"></param>
    /// <returns></returns>
    internal static void SetCovariance(this Transform transform, IEnumerable<XYZ> points, XYZ meanPoint = default)
    {
      var (x, y, z) = meanPoint ?? XYZExtension.ComputeMeanPoint(points);
      Sum covXx = default, covXy = default, covXz = default;
      Sum covYx = default, covYy = default, covYz = default;
      Sum covZx = default, covZy = default, covZz = default;

      foreach (var loc in points)
      {
        // Translate loc relative to mean point
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

      transform.Origin = new XYZ(x, y, z);
      transform.BasisX = new XYZ(covXx.Value, covXy.Value, covXz.Value);
      transform.BasisY = new XYZ(covYx.Value, covYy.Value, covYz.Value);
      transform.BasisZ = new XYZ(covZx.Value, covZy.Value, covZz.Value);
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
      while (--iterations > 0 && !previous.AlmostEquals(principal, tolerance))
      {
        previous = principal;
        principal = covarianceMatrix.OfVector(previous).Normalize(Upsilon);
      }

      return principal;
    }

    /// <summary>
    /// Applies the transformation to the bouding box and returns the result.
    /// </summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="value">The box to transform.</param>
    /// <returns>The transformed bounding box</returns>
    /// <remarks>
    /// Transformation of a bounding box is affected by the translational part of the transformation.
    /// </remarks>
    public static BoundingBoxXYZ OfBoundingBoxXYZ(this Transform transform, BoundingBoxXYZ value)
    {
      var other = value.Clone();
      other.Transform = transform * value.Transform;
      return other;
    }
  }
}
