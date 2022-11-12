using System;
using System.Diagnostics;

namespace RhinoInside.Revit.External.DB
{
  public static class NumericTolerance
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

    #region Abs
    /// <summary>
    /// Absolute value of {<paramref name="x"/>}.
    /// </summary>
    /// <param name="x"></param>
    /// <returns>Distance from {0}.</returns>
    /// <remarks>This method returns denormals as zero.</remarks>
    public static double Abs(double x)
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
    public static double Abs(double x, double y)
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
    public static double Abs(double x, double y, double z)
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

    #region AlmostEqual
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
  }

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
      for (int v = 0; v < values.Length; ++v)
        Add(values[v]);
    }
  }
}
