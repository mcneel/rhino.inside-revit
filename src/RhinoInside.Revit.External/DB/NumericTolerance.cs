using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  using Extensions;

  public static class NumericTolerance
  {
    #region Constants
    /// <summary>
    /// Tolerance value that represents the minimum supported tolerance.
    /// </summary>
    public const double MinTolerance = 0D;

    /// <summary>
    /// Tolerance value used when tolerance parameter is omited.
    /// </summary>
    public const double DefaultTolerance = 1e-9;

    /// <summary>
    /// ε Represents the smallest positive <see cref="double"/> value that is greater than zero.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as <see cref="double.Epsilon"/> 4.94065645841247E-324
    /// </remarks>
    public const double Epsilon = double.Epsilon;

    /// <summary>
    /// υ Represents the smallest positive NORMAL <see cref="double"/> value.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as DBL_MIN +2.2250738585072014e-308
    /// </remarks>
    public const double Upsilon = 4.0 / double.MaxValue;

    /// <summary>
    /// δ Represents the smallest number such that 1.0 + <see cref="Delta"/> != 1.0.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as DBL_EPSILON 2.2204460492503131e-16
    /// </remarks>
    public const double Delta = double.MaxValue * double.Epsilon / 4.0;
    #endregion

    #region Class
    const ulong ZeroMask          = 0x0000_0000_0000_0000UL;
    const ulong SignMask          = 0x8000_0000_0000_0000UL;
    const ulong ExponentMask      = 0x7FF0_0000_0000_0000UL;
    const ulong SignificandMask   = 0x000F_FFFF_FFFF_FFFFUL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong ToBits(double value) => ((ulong) BitConverter.DoubleToInt64Bits(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsFinite(double value)
    {
      return (ToBits(value) & ~SignMask) < ExponentMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNegative(double value)
    {
      if (double.IsNaN(value)) return false;
      return (ToBits(value) & SignMask) != 0UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPositive(double value)
    {
      if (double.IsNaN(value)) return false;
      return (ToBits(value) & SignMask) == 0UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNegativeZero(double value)
    {
      return ToBits(value) == SignMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPositiveZero(double value)
    {
      return ToBits(value) == 0UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Direction(double value)
    {
      // NaN               --> value
      // IsNegative(value) --> -1.0
      // IsPositive(value) --> +1.0

      if (double.IsNaN(value)) return value;
      return ((ulong) BitConverter.DoubleToInt64Bits(value) & SignMask) == 0UL ? +1.0 : -1.0;
    }
    #endregion

    #region Norm
    /// <summary>
    /// Absolute value of {<paramref name="x"/>}.
    /// </summary>
    /// <param name="x"></param>
    /// <returns>Distance from {0}.</returns>
    /// <remarks>This method returns denormals as zero.</remarks>
    public static double Norm(double x)
    {
      x = Math.Abs(x);

      if (x < Upsilon) return 0.0;

      return x;
    }

    /// <summary>
    /// Absolute value of {<paramref name="x"/>, <paramref name="y"/>} .
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>Distance from {0, 0}.</returns>
    /// <remarks>This method returns denormals as zero.</remarks>
    public static double Norm(double x, double y)
    {
      x = Math.Abs(x); y = Math.Abs(y);

      double       u = x, v = y;
      if (x > v) { u = y; v = x; }
      if (v < Upsilon) return 0.0;

      u /= v;

      return Math.Sqrt(1.0 + (u * u)) * v;
    }

    /// <summary>
    /// Absolute value of {<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>}.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>Distance from {0, 0, 0}.</returns>
    /// <remarks>This method returns denormals as zero.</remarks>
    public static double Norm(double x, double y, double z)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double       u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < Upsilon) return 0.0;

      u /= w; v /= w;

      return Math.Sqrt(1.0 + (u * u + v * v)) * w;
    }
    #endregion

    #region AlmostEquals
    /// <summary>
    /// Compares two doubles and determines if they are equal within the specified maximum absolute error.
    /// </summary>
    /// <param name="x">First value</param>
    /// <param name="y">Second value</param>
    /// <param name="tolerance">The absolute accuracy required for being almost equal.</param>
    /// <returns>True if both doubles are almost equal up to the specified maximum absolute error, false otherwise.</returns>
    public static bool AlmostEquals(double x, double y, double tolerance = DefaultTolerance)
    {
      Debug.Assert(tolerance >= Delta);

      if (double.IsInfinity(x) || double.IsInfinity(y))
        return x == y;

      return Math.Abs(x - y) <= tolerance;
    }
    #endregion

    #region MinNumber
    /// <summary>
    /// Compares two values to compute which is lesser and returning the other value if an input is <see cref="double.NaN"/>.
    /// </summary>
    /// <param name="x">The value to compare with <paramref name="y"/>.</param>
    /// <param name="y">The value to compare with <paramref name="x"/>.</param>
    /// <returns><paramref name="x"/> if is less than <paramref name="y"/>; otherwise <paramref name="y"/></returns>
    /// <remarks>
    /// This requires <see cref="double.NaN"/> inputs to not be propagated back to the caller and for -0.0 to be treated as less than +0.0.
    /// </remarks>
    public static double MinNumber(double x, double y) =>
      x < y ? x :
      IsNegativeZero(y) || IsNegativeZero(x) ? -0.0 :
      double.IsNaN(y) ? x : y;
    #endregion

    #region MaxNumber
    /// <summary>
    /// Compares two values to compute which is greater and returning the other value if an input is <see cref="double.NaN"/>.
    /// </summary>
    /// <param name="x">The value to compare with <paramref name="y"/>.</param>
    /// <param name="y">The value to compare with <paramref name="x"/>.</param>
    /// <returns><paramref name="x"/> if is grater than <paramref name="y"/>; otherwise <paramref name="y"/></returns>
    /// <remarks>
    /// This requires <see cref="double.NaN"/> inputs to not be propagated back to the caller and for -0.0 to be treated as less than +0.0.
    /// </remarks>
    public static double MaxNumber(double x, double y) =>
      x > y ? x :
      IsPositiveZero(y) || IsPositiveZero(x) ? +0.0 :
      double.IsNaN(y) ? x : y;
    #endregion
  }

  /// <summary>
  /// This class represents a Summation ∑ of <see cref="System.Double"/> values.
  /// </summary>
  /// <remarks>
  /// Implemented using Neumaier summation algorithm.
  /// </remarks>
  [DebuggerDisplay("{Value}")]
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
      for (int v = 0; v < values.Length; ++v)
        Add(values[v]);
    }
  }

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

    public static implicit operator double(BoundingValue value) => value.IsEnabled ? value.Value : NumericTolerance.Direction(value.Bound) * 1e9;

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
    /// Compares two values to compute the median and returning the other value if an input is <see cref="double.NaN"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static BoundingValue MidBound(BoundingValue value, BoundingValue other)
    {
      var a = value.Bound;
      var b = other.Bound;
      if (double.IsNaN(a)) return other;
      if (double.IsNaN(b)) return value;

      return (a * 0.5) + (b * 0.5);
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
    public static BoundingValue Mid(BoundingValue value, BoundingValue other) => new BoundingValue((value.Value * 0.5) + (other.Value * 0.5), (value.Bound * 0.5) + (other.Bound * 0.5));
    public static BoundingValue Max(BoundingValue value, BoundingValue other)
    {
      var a = value.Bound;
      var b = other.Bound;
      return a > b ? value : double.IsNaN(value.Value) ? value : other;
    }
    public static BoundingValue Radius(BoundingValue value, BoundingValue other) => new BoundingValue((value.Value * 0.5) - (other.Value * 0.5), (value.Bound * 0.5) - (other.Bound * 0.5));
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
    public BoundingValue Mid => BoundingValue.Mid(Left, Right);
    public BoundingValue Max => BoundingValue.Max(Right, Left);

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

    public BoundingValue GetRadius() => BoundingValue.Radius(Left, Right);

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
  /// This class represents a General Form plane equation.
  /// </summary>
  readonly struct PlaneEquation
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
    public XYZ Direction => NumericTolerance.Norm(A, B) < NumericTolerance.DefaultTolerance ?
      new XYZ(C, 0.0, -A) :
      new XYZ(-B, A, 0.0);

    /// <summary>
    /// Plane Y axis according to the Arbitrary Axis Algorithm.
    /// </summary>
    /// <seealso cref="XYZExtension.PerpVector(XYZ, double)"/>
    //public XYZ Up => Normal.CrossProduct(Direction, NumericTolerance.DefaultTolerance);
    public XYZ Up => NumericTolerance.Norm(A, B) < NumericTolerance.DefaultTolerance ?
      new XYZ
      (
           (B * -A) /* - (C *  0.0) */,
           (C * C) - (A * -A),
        /* (A * 0.0) */ -(B * C)
      ) :
      new XYZ
      (
        /* (B * 0.0) */ -(C * A),
           (C * -B) /* - (A * 0.0) */,
           (A * A) - (B * -B)
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
      (A, B, C) = vector.Normalize(0D);
      D = originSignedDistance;
    }

    public PlaneEquation(XYZ point, XYZ normal)
    {
      (A, B, C) = normal.Normalize(0D);
      D = -(A * point.X + B * point.Y + C * point.Z);
    }

    public static PlaneEquation operator -(in PlaneEquation value)
    {
      return new PlaneEquation(-value.A, -value.B, -value.C, -value.D);
    }

    #region AlmostEquals
    public bool AlmostEquals(PlaneEquation other, double tolerance = NumericTolerance.DefaultTolerance)
    {
      tolerance = Math.Max(tolerance, NumericTolerance.Upsilon);

      return NumericTolerance.Norm(A - other.A, B - other.B, C - other.C) < tolerance && NumericTolerance.Norm(D, other.D) < tolerance;
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
