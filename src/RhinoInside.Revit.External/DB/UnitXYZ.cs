using System;
using System.Diagnostics;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  using Numerical;
  using Extensions;

  /// <summary>
  /// This class represents a unit length vector.
  /// </summary>
  /// <remarks>
  /// Sometimes called versor.
  /// </remarks>
  public readonly struct UnitXYZ
  {
    public readonly XYZ Direction;

    public static UnitXYZ NaN { get; } = default;
    public static UnitXYZ BasisX { get; } = new UnitXYZ(XYZ.BasisX);
    public static UnitXYZ BasisY { get; } = new UnitXYZ(XYZ.BasisY);
    public static UnitXYZ BasisZ { get; } = new UnitXYZ(XYZ.BasisZ);

    public static implicit operator bool(UnitXYZ unit) => unit.Direction is object;
    public static XYZ operator *(UnitXYZ unit, double magnitude) => unit.Direction * magnitude;
    public static XYZ operator *(double magnitude, UnitXYZ unit) => magnitude * unit.Direction;

    UnitXYZ(XYZ xyz) => Direction = xyz;
    UnitXYZ(double x, double y, double z) => Direction = new XYZ(x, y, z);

    public static implicit operator XYZ(UnitXYZ unit) => unit.Direction;
    public static explicit operator UnitXYZ(XYZ xyz)
    {
      Debug.Assert(xyz.IsUnitVector(), $"Input {nameof(xyz)} is not a unit length vector.");
      return new UnitXYZ(xyz);
    }

    public void Deconstruct(out double x, out double y, out double z) => (x, y, z) = Direction;

    public bool AlmostEquals(UnitXYZ other, double tolerance = Constant.DefaultTolerance)
    {
      var (aX, aY, aZ) = this;
      var (bX, bY, bZ) = other;

      return Arithmetic.IsZero3(aX - bX, aY - bY, aZ - bZ, tolerance);
    }

    public static UnitXYZ operator -(UnitXYZ source) => new UnitXYZ(-source.Direction);

    public static double DotProduct(XYZ a, XYZ b)
    {
      var (aX, aY, aZ) = a;
      var (bX, bY, bZ) = b;

      return aX * bX + aY * bY + aZ * bZ;
    }
    public double DotProduct(XYZ other) => DotProduct(this, other);

    public static double TripleProduct(UnitXYZ a, UnitXYZ b, UnitXYZ c)
    {
      var (aX, aY, aZ) = a;
      var (bX, bY, bZ) = b;
      var (cX, cY, cZ) = c;

      // (a ⨯ b)
      var xyX = aY * bZ - aZ * bY;
      var xyY = aZ * bX - aX * bZ;
      var xyZ = aX * bY - aY * bX;

      // (a ⨯ b) ⋅ c
      return cX * xyX + cY * xyY + cZ * xyZ;
    }
    public double TripleProduct(UnitXYZ a, UnitXYZ b) => TripleProduct(a, b, this);

    public static XYZ CrossProduct(UnitXYZ a, UnitXYZ b)
    {
      var (aX, aY, aZ) = a;
      var (bX, bY, bZ) = b;

      var x = aY * bZ - aZ * bY;
      var y = aZ * bX - aX * bZ;
      var z = aX * bY - aY * bX;

      return new XYZ(x, y, z);
    }
    public XYZ CrossProduct(UnitXYZ other) => CrossProduct(this, other);

    public static bool Orthonormal(UnitXYZ x, UnitXYZ y, out UnitXYZ z, double tolerance = Constant.DefaultTolerance)
    {
      if (x.IsPerpendicularTo(y, tolerance))
      {
        z = (UnitXYZ) CrossProduct(x, y);
        return true;
      }
      else
      {
        z = default;
        return false;
      }
    }

    public static bool Orthonormalize(XYZ u, XYZ v, out UnitXYZ x, out UnitXYZ y, out UnitXYZ z)
    {
      x = u.ToUnitXYZ();
      y = v.ToUnitXYZ();
      z = CrossProduct(x, y).ToUnitXYZ();
      if (!z) return false;

      y = (UnitXYZ) CrossProduct(z, x);
      return true;
    }

    public double AngleTo(UnitXYZ other)
    {
      var (uX, uY, uZ) = this;
      var (vX, vY, vZ) = other;

      return 2.0 * Math.Atan2
      (
        Arithmetic.Norm(uX - vX, uY - vY, uZ - vZ),
        Arithmetic.Norm(uX + vX, uY + vY, uZ + vZ)
      );
    }

    public double AngleOnPlaneTo(UnitXYZ other, UnitXYZ normal)
    {
      var dotThisOther = DotProduct(this, other);
      var dotThisNormal = DotProduct(this, normal);
      var dotOtherNormal = DotProduct(other, normal);

      var x = dotThisOther - dotOtherNormal * dotThisNormal;
      var y = normal.TripleProduct(this, other);

      var angle = Math.Atan2(y, x);
      return angle < 0.0 ? angle + Constant.Tau : angle;
    }

    /// <summary>
    /// Checks if the the given vector is parallel to this one.
    /// </summary>
    /// <param name="other"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="this"/> and <paramref name="other"/> are parallel</returns>
    public bool IsParallelTo(UnitXYZ other, double tolerance = Constant.DefaultTolerance)
    {
      var (thisX, thisY, thisZ) = this;
      var (otherX, otherY, otherZ) = other;

      return Arithmetic.IsZero3(thisX - otherX, thisY - otherY, thisZ - otherZ, tolerance) ||
             Arithmetic.IsZero3(thisX + otherX, thisY + otherY, thisZ + otherZ, tolerance);
    }

    /// <summary>
    /// Checks if the the given vector is codirectional to this one.
    /// </summary>  
    /// <param name="other"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="this"/> and <paramref name="other"/> are codirectional</returns>
    public bool IsCodirectionalTo(UnitXYZ other, double tolerance = Constant.DefaultTolerance)
    {
      var (thisX, thisY, thisZ) = this;
      var (otherX, otherY, otherZ) = other;

      return Arithmetic.IsZero3(thisX - otherX, thisY - otherY, thisZ - otherZ, tolerance);
    }

    /// <summary>
    /// Checks if the the given vector is perpendicular perpendicular to this one.
    /// </summary>
    /// <param name="other"></param>
    /// <param name="tolerance"></param>
    /// <returns>true if <paramref name="this"/> and <paramref name="other"/> are perpendicular</returns>
    public bool IsPerpendicularTo(UnitXYZ other, double tolerance = Constant.DefaultTolerance)
    {
      return Math.Abs(DotProduct(this, other)) < tolerance;
    }

    /// <summary>
    /// Coordinate System X axis according to the Arbitrary Axis Algorithm.
    /// </summary>
    /// <seealso cref="UnitXYZ.Right(double)"/>
    /// <param name="tolerance"></param>
    /// <returns>X axis of the corresponding coordinate system</returns>
    public UnitXYZ Right(double tolerance = Constant.DefaultTolerance)
    {
      var (x, y, z) = Direction;

      var normXY = Arithmetic.Norm(x, y);
      if (normXY < tolerance)
      {
        // To save CrossProduct and a Unitize3
        //return Unitize(CrossProduct(BasisY, this));
        Arithmetic.Normalize2(ref z, ref x);
        return new UnitXYZ(z, 0.0, -x);
      }
      else
      {
        // To save CrossProduct and a Unitize3
        //return Unitize(CrossProduct(BasisZ, this));
        return new UnitXYZ(-y / normXY, x / normXY, 0.0);
      }
    }

    /// <summary>
    /// Coordinate System Y axis according to the Arbitrary Axis Algorithm.
    /// </summary>
    /// <seealso cref="UnitXYZ.Right(double)"/>
    /// <param name="tolerance"></param>
    /// <returns>Y axis of the corresponding coordinate system</returns>
    public UnitXYZ Up(double tolerance = Constant.DefaultTolerance)
    {
      // To save creating the Right XYZ and some operations on the cross product.
      // return new UnitXYZ(CrossProduct(this, Right(tolerance)));

      var (thisX, thisY, thisZ) = Direction;
      var rightX = thisX; var rightY = thisY; var rightZ = thisZ;

      var normXY = Arithmetic.Norm(rightX, rightY);
      if (normXY < tolerance)
      {
        Arithmetic.Normalize2(ref rightZ, ref rightX);

        rightY = rightX;
        rightX = rightZ;
        rightZ = -rightY;
        //rightY = 0.0;

        // Compute the cross product.
        // Since a and b are unit and perpendicular there is no need to unitize.
        return new UnitXYZ
        (
          thisY * rightZ /*- thisZ * rightY*/,
          thisZ * rightX - thisX * rightZ,
          /*thisX * rightY*/ -thisY * rightX
        );
      }
      else
      {
        rightZ = rightX;
        rightX = -rightY / normXY;
        rightY = rightZ / normXY;
        //rightZ = 0.0;

        // Compute the cross product
        // Since a and b are unit and perpendicular there is no need to unitize.
        return new UnitXYZ
        (
          /*thisY * rightZ*/ -thisZ * rightY,
          thisZ * rightX /*- thisX * rightZ*/,
          thisX * rightY - thisY * rightX
        );
      }
    }
  }

  /// <summary>
  /// This class represents a General Form plane equation.
  /// </summary>
  readonly struct PlaneEquation
  {
    double A => Normal.Direction.X;
    double B => Normal.Direction.Y;
    double C => Normal.Direction.Z;
    double D => Offset;

    /// <summary>
    /// Plane normal direction.
    /// </summary>
    public readonly UnitXYZ Normal;

    /// <summary>
    /// Signed distance of world origin.
    /// </summary>
    public readonly double Offset;

    /// <summary>
    /// Signed distance from world origin.
    /// </summary>
    public double Elevation => -Offset;

    /// <summary>
    /// Point on plane closest to world-origin.
    /// </summary>
    public XYZ Point => new XYZ(A * -D, B * -D, C * -D);

    public PlaneEquation(UnitXYZ direction, double offset)
    {
      Normal = direction;
      Offset = offset;
    }

    public PlaneEquation(XYZ point, UnitXYZ direction)
    {
      Normal = direction;
      Offset = -(direction.Direction.X * point.X + direction.Direction.Y * point.Y + direction.Direction.Z * point.Z);
    }

    public static PlaneEquation operator -(in PlaneEquation value)
    {
      return new PlaneEquation(-value.Normal, -value.Offset);
    }

    #region AlmostEquals
    public bool AlmostEquals(PlaneEquation other)
    {
      return Arithmetic.IsZero4(A - other.A, B - other.B, C - other.C, D - other.D, Constant.DefaultTolerance);
    }
    #endregion

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

    public bool IsBelowOutline(XYZ min, XYZ max)
    {
      var (x, y, z) = MinOutlineCoords(min, max);
      return A * x + B * y + C * z > -D;
    }

    public bool IsAboveOutline(XYZ min, XYZ max)
    {
      var (x, y, z) = MaxOutlineCoords(min, max);
      return A * x + B * y + C * z < -D;
    }
  }
}
