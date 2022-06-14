using System;
using System.Diagnostics;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  /// <summary>
  /// Represents a General Form plane equation.
  /// </summary>
  struct PlaneEquation
  {
    public readonly double A;
    public readonly double B;
    public readonly double C;
    public readonly double D;

    /// <summary>
    /// Point on plane closest to world-origin.
    /// </summary>
    public XYZ Point => new XYZ(A * -D, B * -D, C * -D);

    /// <summary>
    /// Plane X axis according to the Arbitrary Axis Algorithm.
    /// </summary>
    /// <seealso cref="XYZExtension.PerpVector(XYZ, double)"/>
    public XYZ Direction => XYZExtension.GetLength(A, B, 0.0) < NumericTolerance.DefaultTolerance ?
      new XYZ( C, 0.0,  -A):
      new XYZ(-B,   A, 0.0);

    /// <summary>
    /// Plane Y axis according to the Arbitrary Axis Algorithm.
    /// </summary>
    /// <seealso cref="XYZExtension.PerpVector(XYZ, double)"/>
    //public XYZ Up => Normal.CrossProduct(Direction, NumericTolerance.DefaultTolerance);
    public XYZ Up => XYZExtension.GetLength(A, B, 0.0) < NumericTolerance.DefaultTolerance ?
      new XYZ
      (
           (B *  -A) /* - (C *  0.0) */,
           (C *   C)    - (A * -A),
        /* (A * 0.0) */ - (B *  C)
      ):
      new XYZ
      (
        /* (B * 0.0) */ - (C *   A),
           (C *  -B) /* - (A * 0.0) */,
           (A *   A)    - (B *  -B)
      );

    /// <summary>
    /// Plane Z axis according to the Arbitrary Axis Algorithm.
    /// </summary>
    public XYZ Normal => new XYZ(A, B, C);

    /// <summary>
    /// Signed distance from world origin.
    /// </summary>
    public double Elevation => -D;

    PlaneEquation(double a, double b, double c, double d)
    {
      Debug.Assert(XYZExtension.IsUnitLength(a, b, c, NumericTolerance.Upsilon));

      A = a;
      B = b;
      C = c;
      D = d;
    }

    public PlaneEquation(XYZ vector, double originSignedDistance)
    {
      var abc = vector.Normalize(0D);
      A = abc.X;
      B = abc.Y;
      C = abc.Z;
      D = originSignedDistance;
    }

    public PlaneEquation(XYZ point, XYZ normal)
    {
      var abc = normal.Normalize(0D);
      A = abc.X;
      B = abc.Y;
      C = abc.Z;
      D = -(abc.X * point.X + abc.Y * point.Y + abc.Z * point.Z);
    }

    public static PlaneEquation operator -(PlaneEquation value)
    {
      return new PlaneEquation(-value.A, -value.B, -value.C, -value.D);
    }

    public double AbsoluteDistanceTo(XYZ point) => Math.Abs(SignedDistanceTo(point));
    public double SignedDistanceTo(XYZ point) => A * point.X + B * point.Y + C * point.Z + D;

    public XYZ Project(XYZ point) => point - SignedDistanceTo(point) * Normal;

    (double X, double Y, double Z) MinOutlineCoords(XYZ min, XYZ max) =>
    (
      (A <= 0.0) ? max.X : min.X,
      (B <= 0.0) ? max.Y : min.Y,
      (C <= 0.0) ? max.Z : min.Z
    );

    (double X, double Y, double Z) MaxOutlineCoords(XYZ min, XYZ max) =>
    (
      (A >= 0.0) ? max.X : min.X,
      (B >= 0.0) ? max.Y : min.Y,
      (C >= 0.0) ? max.Z : min.Z
    );

    public XYZ MinOutlineCorner(XYZ min, XYZ max)
    {
      var (x, y, z) = MinOutlineCoords(min, max);
      return new XYZ(x, y, z);
    }

    public XYZ MaxOutlineCorner(XYZ min, XYZ max)
    {
      var (x, y, z) = MaxOutlineCoords(min, max);
      return new XYZ(x, y, z);
    }

    public bool IsAboveOutline(XYZ min, XYZ max)
    {
      var (x, y, z) = MaxOutlineCoords(min, max);
      return A * x + B * y + C * z < -D;
    }

    public bool IsBelowOutline(XYZ min, XYZ max)
    {
      var (x, y, z) = MinOutlineCoords(min, max);
      return A * x + B * y + C * z > -D;
    }
  }

  public static class PlaneExtensions
  {
    public static XYZ Evaluate(this Plane plane, UV uv)
    {
      return plane.Origin + uv.U * plane.XVec + uv.V * plane.YVec;
    }

    public static XYZ Evaluate(this Plane plane, UV uv, double distance)
    {
      return plane.Origin + uv.U * plane.XVec + uv.V * plane.YVec + distance * plane.Normal;
    }

    public static double AbsoluteDistanceTo(this Plane plane, XYZ point) => Math.Abs(SignedDistanceTo(plane, point));
    public static double SignedDistanceTo(this Plane plane, XYZ point)
    {
      return new PlaneEquation(plane.Origin, plane.Normal).SignedDistanceTo(point);
    }

#if !REVIT_2018
    public static void Project(this Plane plane, XYZ point, out UV uv, out double distance)
    {
      var v = point - plane.Origin;
      uv = new UV(v.DotProduct(plane.XVec), v.DotProduct(plane.YVec));
      distance = plane.SignedDistanceTo(point);
    }
#endif
  }
}
