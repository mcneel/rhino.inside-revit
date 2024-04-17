using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
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
      if (Numerical.Constant.DefaultTolerance < transform.Determinant)
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
      out XYZ origin, out UnitXYZ basisX, out UnitXYZ basisY, out UnitXYZ basisZ
    )
    {
      if (!transform.IsConformal)
        throw new ArgumentException("Transform is not conformal", nameof(transform));

      origin = transform.Origin;
      basisX = (UnitXYZ) transform.BasisX;
      basisY = (UnitXYZ) transform.BasisY;
      basisZ = (UnitXYZ) transform.BasisZ;
    }

    public static void SetCoordSystem
    (
      this Transform transform,
      XYZ origin, UnitXYZ basisX, UnitXYZ basisY, UnitXYZ basisZ
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
      XYZ origin0, UnitXYZ basisX0, UnitXYZ basisY0, UnitXYZ basisZ0,
      XYZ origin1, UnitXYZ basisX1, UnitXYZ basisY1, UnitXYZ basisZ1
    )
    {
      var from = Transform.Identity;

      from.BasisX = new XYZ(basisX0.Direction.X, basisY0.Direction.X, basisZ0.Direction.X);
      from.BasisY = new XYZ(basisX0.Direction.Y, basisY0.Direction.Y, basisZ0.Direction.Y);
      from.BasisZ = new XYZ(basisX0.Direction.Z, basisY0.Direction.Z, basisZ0.Direction.Z);
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
      Numerical.Sum covXx = default, covXy = default, covXz = default;
      Numerical.Sum covYx = default, covYy = default, covYz = default;
      Numerical.Sum covZx = default, covZy = default, covZz = default;

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
    internal static UnitXYZ GetPrincipalComponent(this Transform covarianceMatrix, double tolerance = Numerical.Constant.DefaultTolerance)
    {
      tolerance = Math.Max(Numerical.Constant.Delta, tolerance);

      var previous  = XYZExtension.One.ToUnitXYZ();
      var principal = covarianceMatrix.OfVector(previous).ToUnitXYZ();

      var iterations = 50;
      while (--iterations > 0 && !principal.IsNaN && !previous.AlmostEquals(principal, tolerance))
      {
        previous = principal;
        principal = covarianceMatrix.OfVector(previous).ToUnitXYZ();
      }

      return principal;
    }

    /// <summary>
    /// Applies the transformation to a bouding box and returns the result.
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

    /// <summary>
    /// Applies the transformation to a plane equation and returns the result.
    /// </summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="value">The box to transform.</param>
    /// <returns>The transformed bounding box</returns>
    /// <remarks>
    /// Transformation of a bounding box is affected by the translational part of the transformation.
    /// </remarks>
    internal static PlaneEquation OfPlaneEquation(this Transform transform, PlaneEquation equation)
    {
      return new PlaneEquation
      (
        transform.OfPoint(equation.Point),
        transform.OfVector(equation.Normal).ToUnitXYZ()
      );
    }

    /// <summary>
    /// Applies the transformation to a point and returns the result.
    /// </summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="value">The point to transform.</param>
    /// <returns>The transformed point</returns>
    /// <remarks>
    /// Transformation of a point is affected by the translational part of the transformation.
    /// </remarks>
    public static Point OfPoint(this Transform transform, Point value) => Point.Create(transform.OfPoint(value.Coord), value.GraphicsStyleId);

    /// <summary>
    /// Applies the transformation to a curve and returns the result.
    /// </summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="value">The curve to transform.</param>
    /// <returns>The transformed curve</returns>
    /// <remarks>
    /// Transformation of a curve is affected by the translational part of the transformation.
    /// </remarks>
    public static Curve OfCurve(this Transform transform, Curve value) => value.CreateTransformed(transform);

    /// <summary>
    /// Applies the transformation to a mesh and returns the result.
    /// </summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="value">The mesh to transform.</param>
    /// <returns>The transformed mesh</returns>
    /// <remarks>
    /// Transformation of a mesh is affected by the translational part of the transformation.
    /// </remarks>
    public static Mesh OfMesh(this Transform transform, Mesh value) => value.get_Transformed(transform);

    /// <summary>
    /// Applies the transformation to a solid and returns the result.
    /// </summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="value">The solid to transform.</param>
    /// <returns>The transformed solid</returns>
    /// <remarks>
    /// Transformation of a solid is affected by the translational part of the transformation.
    /// </remarks>
    public static Solid OfSolid(this Transform transform, Solid value) => SolidUtils.CreateTransformed(value, transform);

    /// <summary>
    /// Applies the transformation to a geometry and returns the result.
    /// </summary>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="value">The geometry to transform.</param>
    /// <returns>The transformed geometry</returns>
    /// <remarks>
    /// Transformation of a geometry is affected by the translational part of the transformation.
    /// </remarks>
    public static GeometryObject OfGeometry(this Transform transform, GeometryObject value)
    {
      switch (value)
      {
        case Point point: return OfPoint(transform, point);
        case Curve curve: return OfCurve(transform, curve);
        case PolyLine pline: return pline.GetTransformed(transform);
        case Mesh mesh: return OfMesh(transform, mesh);
        case Solid solid: return OfSolid(transform, solid);
      }

      throw new NotImplementedException($"{nameof(OfGeometry)} is not implemented for {value.GetType()}.");
    }
  }
}
