using System;
using System.Diagnostics;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  using Numerical;
  using Extensions;

  /// <summary>
  /// This class represents a bounding interval endpoint on the set of <see cref="System.Double"/> numbers.
  /// </summary>
  [DebuggerDisplay("{Bound}")]
  readonly struct BoundingValue
  {
    public enum Bounding
    {
      DisabledMin = -1,
      Enabled = 0,
      DisabledMax = +1
    }

    public bool IsEnabled => Value == Bound;
    public bool IsDisabled => Value != Bound;

    public readonly double Value;
    public readonly double Bound;

    #region Constructors
    BoundingValue(double value, double bound) { Value = value; Bound = bound; }

    public BoundingValue(BoundingValue value) : this(value.Value, value.Bound) { }
    public BoundingValue(double value) : this(value, Bounding.Enabled) { }
    public BoundingValue(double value, Bounding bounding)
    {
      switch (value)
      {
        case double.NegativeInfinity: Value = double.MinValue; break;
        case double.PositiveInfinity: Value = double.MaxValue; break;
        default: Value = value; break;
      }

      switch (bounding)
      {
        case Bounding.DisabledMin: Bound = double.NegativeInfinity; break;
        case Bounding.DisabledMax: Bound = double.PositiveInfinity; break;
        default:                   Bound = Value;                   break;
      }
    }

    public static implicit operator BoundingValue(double value) => new BoundingValue(value);
    public static implicit operator BoundingValue((double Value, Bounding Bounding) value) => new BoundingValue(value.Value, value.Bounding);
    #endregion

    #region Deconstuctors
    public void Deconstruct(out double value, out Bounding bounding)
    {
      value = Value;
      switch (Bound)
      {
        case double.NegativeInfinity: bounding = Bounding.DisabledMin;  break;
        case double.PositiveInfinity: bounding = Bounding.DisabledMax;  break;
        default:                      bounding = Bounding.Enabled;      break;
      }
    }

    public static implicit operator double(BoundingValue value) => value.IsEnabled ? value.Value : Arithmetic.Direction(value.Bound) * 1e9;

    public static bool operator true(BoundingValue value) => value.IsEnabled;
    public static bool operator false(BoundingValue value) => value.IsDisabled;
    #endregion

    #region System.Object
    public override string ToString() => Bound.ToString();
    public override bool Equals(object obj) => obj is BoundingValue value && value == this;
    public override int GetHashCode() => Bound.GetHashCode();
    #endregion

    #region Operators
    public static bool operator ==(BoundingValue value, BoundingValue other) => value.Bound == other.Bound;
    public static bool operator !=(BoundingValue value, BoundingValue other) => value.Bound != other.Bound;

    public static bool operator <(BoundingValue value, BoundingValue other) => value.Bound < other.Bound;
    public static bool operator >(BoundingValue value, BoundingValue other) => value.Bound > other.Bound;

    public static bool operator <=(BoundingValue value, BoundingValue other) => value.Bound <= other.Bound;
    public static bool operator >=(BoundingValue value, BoundingValue other) => value.Bound >= other.Bound;

    public static BoundingValue operator -(BoundingValue value) => new BoundingValue(-value.Value, -value.Bound);
    #endregion

    /// <summary>
    /// Compares two values to compute which is lesser and returning the other value if an input is <see cref="double.NaN"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static BoundingValue MinBound(BoundingValue value, BoundingValue other)
    {
      var a = value.Bound;
      var b = other.Bound;
      return a < b ? value.Value : other.Value;
    }

    /// <summary>
    /// Compares two values to compute the mean and returning the other value if an input is <see cref="double.NaN"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static BoundingValue MeanBound(BoundingValue value, BoundingValue other)
    {
      var a = value.Bound;
      var b = other.Bound;
      if (double.IsNaN(a)) return other;
      if (double.IsNaN(b)) return value;

      return Arithmetic.Mean(a, b);
    }

    /// <summary>
    /// Compares two values to compute which is greater and returning the other value if an input is <see cref="double.NaN"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static BoundingValue MaxBound(BoundingValue value, BoundingValue other)
    {
      var a = value.Bound;
      var b = other.Bound;
      return a > b ? value.Value : other.Value;
    }

    public static BoundingValue Min(BoundingValue value, BoundingValue other)
    {
      var a = value.Bound;
      var b = other.Bound;
      return a < b ? value : double.IsNaN(value.Value) ? value : other;
    }
    public static BoundingValue Max(BoundingValue value, BoundingValue other)
    {
      var a = value.Bound;
      var b = other.Bound;
      return a > b ? value : double.IsNaN(value.Value) ? value : other;
    }
    public static BoundingValue Mean(BoundingValue value, BoundingValue other) => new BoundingValue(Arithmetic.Mean(value.Value, other.Value), Arithmetic.Mean(value.Bound, other.Bound));
    public static BoundingValue Deviation(BoundingValue value, BoundingValue other) => new BoundingValue(Arithmetic.Deviation(value.Value, other.Value), Arithmetic.Deviation(value.Bound, other.Bound));
  }

  /// <summary>
  /// This class represents a bounding interval on the set of <see cref="System.Double"/> numbers.
  /// </summary>
  [DebuggerDisplay("{Left} .. {Right}")]
  readonly struct BoundingInterval
  {
    public static readonly BoundingInterval Empty    = (double.NaN,                           double.NaN);
    public static readonly BoundingInterval Universe = (double.NegativeInfinity, double.PositiveInfinity);

    public readonly BoundingValue Left;
    public readonly BoundingValue Right;

    public BoundingValue Min => BoundingValue.Min(Left, Right);
    public BoundingValue Max => BoundingValue.Max(Right, Left);
    public BoundingValue Mean => BoundingValue.Mean(Left, Right);
    public BoundingValue Deviation => BoundingValue.Deviation(Left, Right);

    #region Constructors
    public BoundingInterval(BoundingValue left, BoundingValue right) { Left = left; Right = right; }

    public static implicit operator BoundingInterval((BoundingValue Left, BoundingValue Right) value) => new BoundingInterval(value.Left, value.Right);
    #endregion

    #region Deconstructors
    public void Deconstruct(out BoundingValue left, out BoundingValue right)
    {
      left = Left;
      right = Right;
    }

    public static bool operator true(BoundingInterval value) => value.IsIncreasing;
    public static bool operator false(BoundingInterval value) => value.IsDecreasing;
    #endregion

    #region System.Object
    public override string ToString() => $"{Left} .. {Right}";
    public override bool Equals(object obj) => obj is BoundingInterval value && value == this;
    public override int GetHashCode()
    {
      int hashCode = -1051820395;
      hashCode = hashCode * -1521134295 + Left.GetHashCode();
      hashCode = hashCode * -1521134295 + Right.GetHashCode();
      return hashCode;
    }
    #endregion

    #region Classification
    public bool IsIncreasing => Left < Right;
    public bool IsDecreasing => Left > Right;
    public bool IsDegenerate => Left == Right;
    public bool IsProper => Left != Right;
    public bool IsEmpty => !IsDegenerate && !IsProper;
    public bool IsFinite => Left.IsEnabled && Right.IsEnabled;
    #endregion

    #region Operators
    public static bool operator ==(BoundingInterval left, BoundingInterval right) => left.Left == right.Left && left.Right == right.Right;
    public static bool operator !=(BoundingInterval left, BoundingInterval right) => left.Left != right.Left || left.Right != right.Right;

    public static BoundingInterval operator -(BoundingInterval value) => (-value.Right, -value.Left);

    public static BoundingInterval operator !(BoundingInterval value) => (value.Right, value.Left);
    public static BoundingInterval operator &(BoundingInterval left, BoundingInterval right) => (BoundingValue.MaxBound(left.Left, right.Left), BoundingValue.MinBound(left.Right, right.Right));
    public static BoundingInterval operator |(BoundingInterval left, BoundingInterval right) => (BoundingValue.MinBound(left.Left, right.Left), BoundingValue.MaxBound(left.Right, right.Right));
    #endregion


    public bool Contains(BoundingValue value, bool closed = true)
    {
      if (closed)
      {
        if (IsIncreasing) return Left  <= value && value <= Right;
        if (IsDecreasing) return Right <= value || value <= Left;
      }
      else
      {
        if (IsIncreasing) return Left  <  value && value <  Right;
        if (IsDecreasing) return Right <  value || value <  Left;
      }

      return false;
    }
  }

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
      var dotThisOther   = DotProduct(this, other);
      var dotThisNormal  = DotProduct(this, normal);
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
