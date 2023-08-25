using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RhinoInside.Revit.Numerical
{
  static class Constant
  {
    /// <summary>
    /// Represents the ratio of the circumference of a circle to its radius,
    /// specified by the constant, τ.
    /// </summary>
    /// <remarks>
    /// Same as the number of radians in one turn.
    /// </remarks>
    public const double Tau = 2.0 * Math.PI;

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
    /// Same as DBL_TRUE_MIN = 4.94065645841247E-324
    /// </remarks>
    public const double Epsilon = double.Epsilon;

    /// <summary>
    /// υ Represents the smallest positive NORMAL <see cref="double"/> value.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as DBL_MIN = 2.2250738585072014e-308
    /// </remarks>
    public const double Upsilon = 4.0 / double.MaxValue;

    /// <summary>
    /// δ Represents the smallest number such that 1.0 + <see cref="Delta"/> != 1.0.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as DBL_EPSILON = 2.2204460492503131e-16
    /// </remarks>
    public const double Delta = double.MaxValue * double.Epsilon / 4.0;
  }

  /// <summary>
  /// This class contains basic arithmetic operations on the set of <see cref="System.Double"/> numbers.
  /// </summary>
  static class Arithmetic
  {
    #region Classification
    const ulong ZeroMask = 0x0000_0000_0000_0000UL;
    const ulong SignMask = 0x8000_0000_0000_0000UL;
    const ulong ExponentMask = 0x7FF0_0000_0000_0000UL;
    const ulong SignificandMask = 0x000F_FFFF_FFFF_FFFFUL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong ToBits(double value) => (ulong) BitConverter.DoubleToInt64Bits(value);

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
    internal static bool IsNormal(double value)
    {
      return Math.Abs(value) >= Constant.Upsilon;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Direction(double value)
    {
      // NaN               --> value
      // IsNegative(value) --> -1.0
      // IsPositive(value) --> +1.0

      if (double.IsNaN(value)) return value;
      return (ToBits(value) & SignMask) == 0UL ? +1.0 : -1.0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Magnitude(double value)
    {
      return Math.Abs(value);
    }
    #endregion

    #region Interval
    /// <summary>
    /// Computes the min point of the interval [<paramref name="x"/>, <paramref name="y"/>].
    /// </summary>
    /// <param name="x">The value to compare with <paramref name="y"/>.</param>
    /// <param name="y">The value to compare with <paramref name="x"/>.</param>
    /// <returns><paramref name="x"/> if is less than <paramref name="y"/>; otherwise <paramref name="y"/></returns>
    /// <remarks>
    /// This requires <see cref="double.NaN"/> inputs to not be propagated back to the caller and for -0.0 to be treated as less than +0.0.
    /// </remarks>
    public static double Min(double x, double y) =>
      x < y           ? x                              :
      x == y          ? (IsNegativeZero(x) ? -0.0 : y) :
      double.IsNaN(y) ? x                         : y  ;

    /// <summary>
    /// Computes the max point of the interval [<paramref name="x"/>, <paramref name="y"/>].
    /// </summary>
    /// <param name="x">The value to compare with <paramref name="y"/>.</param>
    /// <param name="y">The value to compare with <paramref name="x"/>.</param>
    /// <returns><paramref name="x"/> if is grater than <paramref name="y"/>; otherwise <paramref name="y"/></returns>
    /// <remarks>
    /// This requires <see cref="double.NaN"/> inputs to not be propagated back to the caller and for -0.0 to be treated as less than +0.0.
    /// </remarks>
    public static double Max(double x, double y) =>
      x > y           ? x                              :
      x == y          ? (IsPositiveZero(x) ? +0.0 : y) :
      double.IsNaN(y) ? x                         : y  ;

    /// <summary>
    /// Computes the mean point of the interval [<paramref name="x"/>, <paramref name="y"/>].
    /// </summary>
    /// <param name="x">Value <paramref name="x"/>.</param>
    /// <param name="y">Value <paramref name="y"/>.</param>
    /// <returns>(<paramref name="x"/> + <paramref name="y"/>) / 2.0</returns>
    /// <remarks>
    /// This requires no overflow occurs.
    /// </remarks>
    public static double Mean(double x, double y)
    {
      if (x == y) return x;

      var X = Math.Abs(x);
      var Y = Math.Abs(y);

      //// Avoids oveflow
      //if (X <= double.MaxValue / 2 && Y <= double.MaxValue / 2)
      //  return (x + y) * 0.5;

      if (X < Constant.Upsilon)
        return x + (y * 0.5);

      if (Y < Constant.Upsilon)
        return (x * 0.5) + y;

      return (x * 0.5) + (y * 0.5);
    }

    /// <summary>
    /// Computes the signed “radius” of the interval [<paramref name="x"/>, <paramref name="y"/>].
    /// </summary>
    /// <param name="x">Value <paramref name="x"/>.</param>
    /// <param name="y">Value <paramref name="y"/>.</param>
    /// <returns>(<paramref name="x"/> - <paramref name="y"/>) / 2.0</returns>
    /// <remarks>
    /// This requires no overflow occurs.
    /// </remarks>
    public static double Deviation(double x, double y) => Mean(x, -y);

    /// <summary>
    /// Performs a linear interpolation on the interval [<paramref name="x"/>, <paramref name="y"/>] based on the given parameter <paramref name="t"/>.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="t"></param>
    /// <returns>The interpolated value at <paramref name="t"/>.</returns>
    public static double Lerp(double x, double y, double t)
    {
      if (x == y) return x;

      var X = Math.Abs(x);
      var Y = Math.Abs(y);

      //// Avoids oveflow
      //if (X <= double.MaxValue / 2 && Y <= double.MaxValue / 2)
      //  return x + ((y - x) * t);

      if (X < Constant.Upsilon)
        return x + (y * t);

      if (Y < Constant.Upsilon)
        return (x * (1.0 - t)) + y;

      return (x * (1.0 - t)) + (y * t);
    }

    /// <summary>
    /// Returns <paramref name="value"/> clamped to the interval [<paramref name="min"/>, <paramref name="max"/>].
    /// </summary>
    /// <param name="value">The value to be clamped.</param>
    /// <param name="min">The lower bound of the result.</param>
    /// <param name="max">The upper bound of the result.</param>
    /// <returns>Clamped <paramref name="value"/> or <see cref="double.NaN"/> if <paramref name="value"/> equals <see cref="double.NaN"/>.</returns>
    public static double Clamp(double value, double min, double max)
    {
      return value < min ? min : max < value ? max : value;
    }
    #endregion
  }

  /// <summary>
  /// This class contais Euclidean space operations.
  /// </summary>
  static class Euclidean
  {
    #region Norm
    /// <summary>
    /// Norm of {<paramref name="x"/>}.
    /// </summary>
    /// <param name="x"></param>
    /// <returns>Distance from {0}.</returns>
    /// <remarks>This method returns denormals as zero.</remarks>
    internal static double Norm(double x)
    {
      x = Math.Abs(x);

      if (x < Constant.Upsilon) return 0.0;

      return x;
    }

    /// <summary>
    /// Norm of {<paramref name="x"/>, <paramref name="y"/>} .
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>Distance from {0, 0}.</returns>
    /// <remarks>This method returns denormals as zero.</remarks>
    internal static double Norm(double x, double y)
    {
      x = Math.Abs(x); y = Math.Abs(y);

      double u = x, v = y;
      if (x > v) { u = y; v = x; }
      if (v < Constant.Upsilon) return 0.0;

      u /= v;

      return Math.Sqrt(1.0 + (u * u)) * v;
    }

    /// <summary>
    /// Norm of {<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>}.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>Distance from {0, 0, 0}.</returns>
    /// <remarks>This method returns denormals as zero.</remarks>
    internal static double Norm(double x, double y, double z)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < Constant.Upsilon) return 0.0;

      u /= w; v /= w;

      return Math.Sqrt(1.0 + (u * u + v * v)) * w;
    }
    #endregion

    #region IsZero
    internal static bool IsZero1(double x, double tolerance = 0.5 * Constant.Upsilon)
    {
      x = Math.Abs(x);

      return x <= tolerance;
    }

    internal static bool IsZero2(double x, double y, double tolerance = 0.5 * Constant.Upsilon)
    {
      x = Math.Abs(x); y = Math.Abs(y);

      double u = x, v = y;
      if (x > v) { u = y; v = x; }
      if (v < (0.0 + tolerance) / 2.0) return true;
      if (v > (0.0 + tolerance)) return false;

      u /= v;

      return Math.Sqrt(1.0 + (u * u)) * v <= tolerance;
    }

    internal static bool IsZero3(double x, double y, double z, double tolerance = 0.5 * Constant.Upsilon)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < (0.0 + tolerance) / 3.0) return true;
      if (w > (0.0 + tolerance)) return false;

      u /= w; v /= w;

      return Math.Sqrt(1.0 + (u * u + v * v)) * w <= tolerance;
    }

    internal static bool IsZero4(double x, double y, double z, double w, double tolerance = 0.5 * Constant.Upsilon)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z); w = Math.Abs(w);

      double a = x, b = y, c = z, d = w;
      if (x > d) { a = y; b = z; c = w; d = x; }
      if (y > d) { a = z; b = w; c = x; d = y; }
      if (z > d) { a = w; b = x; c = y; d = z; }
      if (d < (0.0 + tolerance) / 4.0) return true;
      if (d > (0.0 + tolerance)) return false;

      a /= d; b /= d; c /= d;

      return Math.Sqrt(1.0 + (a * a + b * b + c * c)) * d <= tolerance;
    }
    #endregion

    #region IsUnit
    internal static bool IsUnit1(double x, double tolerance = 0.5 * Constant.Delta)
    {
      x = Math.Abs(x);

      return 1.0 - x <= tolerance;
    }

    internal static bool IsUnit2(double x, double y, double tolerance = 0.5 * Constant.Delta)
    {
      x = Math.Abs(x); y = Math.Abs(y);

      double u = x, v = y;
      if (x > v) { u = y; v = x; }
      if (v < (1.0 - tolerance) / 2.0) return false;
      if (v > (1.0 + tolerance)) return false;

      u /= v;

      return 1.0 - (Math.Sqrt(1.0 + (u * u)) * v) < tolerance;
    }

    internal static bool IsUnit3(double x, double y, double z, double tolerance = 0.5 * Constant.Delta)
    {
      x = Math.Abs(x); y = Math.Abs(y); z = Math.Abs(z);

      double u = x, v = y, w = z;
      if (x > w) { u = y; v = z; w = x; }
      if (y > w) { u = z; v = x; w = y; }
      if (w < (1.0 - tolerance) / 3.0) return false;
      if (w > (1.0 + tolerance)) return false;

      u /= w; v /= w;

      return 1.0 - (Math.Sqrt(1.0 + (u * u + v * v)) * w) <= tolerance;
    }
    #endregion

    #region Normalize
    internal static bool Normalize1(ref double x)
    {
      // Infinity is not handled.
      // if (double.IsInfinity(x)) return false;
      if (x < 0.0) { x = -1.0; return true; }
      if (x > 0.0) { x = +1.0; return true; }
      // -0.0 -> -0.0
      // +0.0 -> +0.0
      //  NaN -> NaN
      return false;
    }

    internal static bool Normalize2(ref double x, ref double y)
    {
      double X = Math.Abs(x), Y = Math.Abs(y);

      double u = X, v = Y;
      if (!(X < v)) { u = Y; v = X; }
      if (!(v >= Constant.Upsilon))
      {
        if (!(v != 0.0)) return false; // { x, y } It is here for Zeros but also handles NaNs
        u *= double.MaxValue; v *= double.MaxValue;
        x *= double.MaxValue; y *= double.MaxValue;
      }

      u /= v;
      var length = Math.Sqrt(1.0 + (u * u)) * v;
      x /= length; y /= length;
      return true; // !double.IsNaN(length); Infinity is not handled.
    }

    internal static bool Normalize3(ref double x, ref double y, ref double z)
    {
      double X = Math.Abs(x), Y = Math.Abs(y), Z = Math.Abs(z);

      double u = X, v = Y, w = Z;
      if (!(X < w)) { u = Y; v = Z; w = X; }
      if (!(Y < w)) { u = Z; v = X; w = Y; }
      if (!(w >= Constant.Upsilon))
      {
        if (!(w != 0.0)) return false; // { x, y, z } It is here for Zeros but also handles NaNs
        u *= double.MaxValue; v *= double.MaxValue; w *= double.MaxValue;
        x *= double.MaxValue; y *= double.MaxValue; z *= double.MaxValue;
      }

      u /= w; v /= w;
      var length = Math.Sqrt(1.0 + (u * u + v * v)) * w;
      x /= length; y /= length; z /= length;
      return true; // !double.IsNaN(length); Infinity is not handled.
    }
    #endregion
  }

  public readonly struct Tolerance : System.Collections.Generic.IEqualityComparer<double>
  {
    public static readonly Tolerance Default = new Tolerance(Constant.DefaultTolerance);

    /// <summary>
    /// Maximum absolute error.
    /// </summary>
    public readonly double Value;
    Tolerance(double tolerance) => Value = tolerance;

    public static implicit operator Tolerance(double value) => new Tolerance(value);
    public static implicit operator double(Tolerance tolerance) => tolerance.Value;

    public static Tolerance Comparer(double tolerance) => new Tolerance(tolerance);

    /// <summary>
    /// Compares two doubles and determines if they are equal within the specified maximum absolute error.
    /// </summary>
    /// <param name="x">First value</param>
    /// <param name="y">Second value</param>
    /// <returns>True if both doubles are almost equal up to the specified maximum absolute error, false otherwise.</returns>
    public bool Equals(double x, double y) => Math.Abs(x - y) <= Value;

    public int GetHashCode(double value)
    {
      var hash = 0.1 * Math.Round(value / Value);
      if (Math.Abs(hash) < int.MaxValue) return (int) hash;
      return double.IsNaN(hash) ? int.MinValue : Math.Sign(hash) * int.MaxValue;
    }
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
}
