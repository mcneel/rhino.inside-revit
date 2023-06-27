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
}
