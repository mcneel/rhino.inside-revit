using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Rhino;
using Rhino.DocObjects;

namespace RhinoInside.Revit.Convert.Units
{
  [DebuggerDisplay("{Antecedent} ∶ {Consequent} ({Quotient})")]
  readonly struct Ratio : IEquatable<Ratio>
  {
    public static readonly Ratio MinValue = new Ratio(double.MinValue);
    public static readonly Ratio MaxValue = new Ratio(double.MaxValue);
    public static readonly Ratio Epsilon = new Ratio(double.Epsilon);
    public static readonly Ratio NegativeInfinity = new Ratio(-1.0, 0.0);
    public static readonly Ratio PositiveInfinity = new Ratio(+1.0, 0.0);
    public static readonly Ratio NaN = default;

    public static bool IsNegative(Ratio ratio) => IsNegative(ratio.Quotient);
    public static bool IsPositive(Ratio ratio) => IsPositive(ratio.Quotient);
    public static bool IsFinite(Ratio ratio) => IsFinite(ratio.Quotient);
    public static bool IsInfinity(Ratio ratio) => double.IsInfinity(ratio.Quotient);
    public static bool IsPositiveInfinity(Ratio ratio) => double.IsPositiveInfinity(ratio.Quotient);
    public static bool IsNegativeInfinity(Ratio ratio) => double.IsNegativeInfinity(ratio.Quotient);
    public static bool IsNaN(Ratio ratio) => double.IsNaN(ratio.Quotient);

    #region Double Internals
    const ulong SignMask        = 0x8000_0000_0000_0000UL;
    const ulong ExponentMask    = 0x7FF0_0000_0000_0000UL;
    const ulong SignificandMask = 0x000F_FFFF_FFFF_FFFFUL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsFinite(double value)
    {
      return ((ulong) BitConverter.DoubleToInt64Bits(value) & ~SignMask) < ExponentMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsNegative(double value)
    {
      return ((ulong) BitConverter.DoubleToInt64Bits(value) & SignMask) != 0UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsPositive(double value)
    {
      return ((ulong) BitConverter.DoubleToInt64Bits(value) & SignMask) == 0UL;
    }
    #endregion

    #region Quotient & Remainder
    public double Quotient => Antecedent / Consequent;
    public double Remainder => Antecedent % Consequent;
    public double EuclideanQuotient => Math.Round(Antecedent / Consequent);
    public double EuclideanRemainder => Math.IEEERemainder(Antecedent, Consequent);

    public Ratio(double value)
    {
      Antecedent = value;
      Consequent = 1.0;
    }
    #endregion

    #region Terms
    public readonly double Antecedent;
    public readonly double Consequent;

    public Ratio(double antecedent, double consequent)
    {
      Antecedent = antecedent;
      Consequent = consequent;
    }

    public void Deconstruct(out double antecedent, out double consequent)
    {
      antecedent = Antecedent;
      consequent = Consequent;
    }
    #endregion

    #region Casting operators
    public static explicit operator double(Ratio ratio) => ratio.Quotient;
    public static explicit operator Ratio(double value) => new Ratio(value);

    public static implicit operator Ratio((double A, double B) ratio) => new Ratio(ratio.A, ratio.B);
    public static implicit operator (double A, double B)(Ratio ratio) => (ratio.Antecedent, ratio.Consequent);
    #endregion

    #region Static methods
    public static Ratio Rationalize(double value)
    {
      if (double.IsNaN(value)) return NaN;

      const int MaximumBits = sizeof(ulong) * 8;
      const ulong MaximumDenominator = 2UL ^ 52;
      const ulong MaximumExponent = 1UL << (MaximumBits - 1);

      ulong a, x, d, n = 1;
      var sign = IsNegative(value) ? -1.0 : +1.0;

      value *= sign;
      {
        var f = value;
        while (n < MaximumExponent && f != Math.Floor(f))
        {
          n <<= 1; f *= 2.0;
        }

        if (n == 1)
          return new Ratio(sign * value); // Integers and Infinities are returned here including +-0.0

        d = (ulong) Math.Round(f);
      }

      var h0 = 0UL; var h1 = 1UL; var h2 = 0UL;
      var k0 = 1UL; var k1 = 0UL; var k2 = 0UL;

      for (int i = 0; i < MaximumBits; i++)
      {
        a = (n != 0) ? d / n : 0;
        if ((i != 0) && (a == 0)) break;

        x = d;
        d = n;
        n = n != 0 ? x % n : x;

        x = a;
        if (k1 * a + k0 >= MaximumDenominator)
        {
          x = (MaximumDenominator - k0) / k1;
          if (x * 2 >= a || k1 >= MaximumDenominator) i = int.MaxValue;
          else break;
        }

        h2 = x * h1 + h0; h0 = h1; h1 = h2;
        k2 = x * k1 + k0; k0 = k1; k1 = k2;

        if (((double) h1 / (double) k1) == value)
          break;
      }

      var antecedent = (double) h1;
      var consequent = (double) k1;

      Debug.Assert(antecedent / consequent == value);

      return new Ratio(sign * antecedent, consequent);
    }

    public static Ratio Reciprocal(Ratio ratio) => new Ratio(ratio.Consequent, ratio.Antecedent);
    public static Ratio Abs(Ratio ratio) => new Ratio(Math.Abs(ratio.Antecedent), Math.Abs(ratio.Consequent));
    public static Ratio Negate(Ratio ratio) => new Ratio(-ratio.Antecedent, ratio.Consequent);
    public static Ratio Pow(Ratio ratio, double exponent) => new Ratio(Math.Pow(ratio.Antecedent, exponent), Math.Pow(ratio.Consequent, exponent));
    #endregion

    #region IEquatable
    public override bool Equals(object other) => other is Ratio ratio && Equals(ratio);
    public bool Equals(Ratio other) => Quotient == other.Quotient;

    public override int GetHashCode() => Quotient.GetHashCode();

    public static bool operator ==(Ratio left, Ratio right) => left.Quotient == right.Quotient;
    public static bool operator !=(Ratio left, Ratio right) => left.Quotient != right.Quotient;
    #endregion

    #region Operators
    public static Ratio operator ~(Ratio ratio) => Reciprocal(ratio);
    public static Ratio operator !(Ratio ratio) => Rationalize(ratio.Quotient);

    public static Ratio operator --(Ratio ratio) => new Ratio(ratio.Antecedent - ratio.Consequent, ratio.Consequent);
    public static Ratio operator ++(Ratio ratio) => new Ratio(ratio.Antecedent + ratio.Consequent, ratio.Consequent);

    public static Ratio operator *(Ratio left, Ratio right) =>
      new Ratio(left.Antecedent * right.Antecedent, left.Consequent * right.Consequent);
    public static Ratio operator /(Ratio left, Ratio right) =>
      new Ratio(left.Antecedent * right.Consequent, left.Consequent * right.Antecedent);

    public static double operator *(double value, Ratio ratio)
    {
      if (ratio.Antecedent == ratio.Consequent)
        return value;

      // Multiply value by resulting ratio considering magnitude.
      if (Math.Abs(ratio.Antecedent) < Math.Abs(value))
        return ratio.Antecedent * (value / ratio.Consequent);
      else
        return value * (ratio.Antecedent / ratio.Consequent);
    }

    public static double operator /(double value, Ratio ratio)
    {
      if (ratio.Antecedent == ratio.Consequent)
        return value;

      // Multiply value by resulting ratio considering magnitude.
      if (Math.Abs(ratio.Consequent) < Math.Abs(value))
        return ratio.Consequent * (value / ratio.Antecedent);
      else
        return value * (ratio.Consequent / ratio.Antecedent);
    }

    public static double operator *(Ratio ratio, double value)
    {
      if (ratio.Antecedent == ratio.Consequent)
        return value;

      // Multiply value by resulting ratio considering magnitude.
      if (Math.Abs(ratio.Antecedent) < Math.Abs(value))
        return ratio.Antecedent * (value / ratio.Consequent);
      else
        return value * (ratio.Antecedent / ratio.Consequent);
    }

    public static double operator /(Ratio ratio, double value)
    {
      if (ratio.Antecedent == ratio.Consequent)
        return 1.0 / value;

      // Multiply value by resulting ratio considering magnitude.
      if (Math.Abs(ratio.Consequent) > Math.Abs(value))
        return ratio.Antecedent / (value * ratio.Consequent);
      else
        return (ratio.Antecedent / ratio.Consequent) / value;
    }
    #endregion
  }

  [DebuggerDisplay("{ToString(), nq} ({(double) this} m)")]
  readonly struct UnitScale : IEquatable<UnitScale>
  {
    #region BuiltIn Ratios
    static readonly Ratio[] metersPerUnitRatio          = new Ratio[]
    {
      (                      0,                        1 ), // None,
      (                      1,                1_000_000 ), // Microns,
      (                      1,                    1_000 ), // Millimeters,
      (                      1,                      100 ), // Centimeters,
      (                      1,                        1 ), // Meters,
      (                  1_000,                        1 ), // Kilometers,
      (                    254,           10_000_000_000 ), // Microinches,
      (                    254,               10_000_000 ), // Mils,
      (                    254,                   10_000 ), // Inches,
      (                  3_048,                   10_000 ), // Feet,
      (              1_609_344,                    1_000 ), // Miles,
      (                   -0.0,                        1 ), // CustomUnits,
      (                      1,           10_000_000_000 ), // Angstroms,,
      (                      1,            1_000_000_000 ), // Nanometers,
      (                      1,                       10 ), // Decimeters,
      (                     10,                        1 ), // Dekameters,
      (                    100,                        1 ), // Hectometers,
      (              1_000_000,                        1 ), // Megameters,
      (          1_000_000_000,                        1 ), // Gigameters,
      (                  9_144,                   10_000 ), // Yards,
      (                    254,                   72_000 ), // PrinterPoints,
      (                    254,                    6_000 ), // PrinterPicas,
      (                  1_852,                        1 ), // NauticalMiles,
      (        149_597_870_700,                        1 ), // AstronomicalUnits,
      (  9_460_730_472_580_800,                        1 ), // LightYears,
      ( 96_939_420_213_600_000,                  Math.PI ), // Parsecs,
      (                      0,                        0 )  // Unset
    };
    #endregion

    #region BuiltIn Scales
    public static readonly UnitScale Unset             = default;
    public static readonly UnitScale None              = new UnitScale(default, (Ratio) 0.0);

    public static readonly UnitScale Angstroms         = new UnitScale(UnitSystem.Angstroms);
    public static readonly UnitScale Nanometers        = new UnitScale(UnitSystem.Nanometers);
    public static readonly UnitScale Microns           = new UnitScale(UnitSystem.Microns);
    public static readonly UnitScale Millimeters       = new UnitScale(UnitSystem.Millimeters);
    public static readonly UnitScale Centimeters       = new UnitScale(UnitSystem.Centimeters);
    public static readonly UnitScale Decimeters        = new UnitScale(UnitSystem.Decimeters);
    public static readonly UnitScale Meters            = new UnitScale(UnitSystem.Meters);
    public static readonly UnitScale Dekameters        = new UnitScale(UnitSystem.Dekameters);
    public static readonly UnitScale Hectometers       = new UnitScale(UnitSystem.Hectometers);
    public static readonly UnitScale Kilometers        = new UnitScale(UnitSystem.Kilometers);
    public static readonly UnitScale Megameters        = new UnitScale(UnitSystem.Megameters);
    public static readonly UnitScale Gigameters        = new UnitScale(UnitSystem.Gigameters);

    public static readonly UnitScale Microinches       = new UnitScale(UnitSystem.Microinches);
    public static readonly UnitScale Mils              = new UnitScale(UnitSystem.Mils);
    public static readonly UnitScale Inches            = new UnitScale(UnitSystem.Inches);
    public static readonly UnitScale Feet              = new UnitScale(UnitSystem.Feet);
    public static readonly UnitScale Yards             = new UnitScale(UnitSystem.Yards);
    public static readonly UnitScale Miles             = new UnitScale(UnitSystem.Miles);
    public static readonly UnitScale NauticalMiles     = new UnitScale(UnitSystem.NauticalMiles);

    public static readonly UnitScale PrinterPoints     = new UnitScale(UnitSystem.PrinterPoints);
    public static readonly UnitScale PrinterPicas      = new UnitScale(UnitSystem.PrinterPicas);

    public static readonly UnitScale AstronomicalUnits = new UnitScale(UnitSystem.AstronomicalUnits);
    public static readonly UnitScale Parsecs           = new UnitScale(UnitSystem.Parsecs);
    public static readonly UnitScale LightYears        = new UnitScale(UnitSystem.LightYears);

    internal static readonly UnitScale UsSurveyFeet   = new UnitScale("Us Survey Foot", (1_200.0, 3_937.0));
    internal static readonly UnitScale Internal       = Feet;
    #endregion

    #region Fields & Properties
    public readonly string Name;
    public readonly Ratio Ratio;
    #endregion

    #region CustomUnits
    static readonly string CustomUnitsName = $"{UnitSystem.CustomUnits}";
    UnitScale(double metersPerUnit) : this(CustomUnitsName, Ratio.Rationalize(Math.Abs(metersPerUnit))) { }

    UnitScale(string name, Ratio metersPerUnit)
    {
      Name = name;
      Ratio = metersPerUnit;
    }
    UnitScale(Ratio metersPerUnit)
    {
      Name = CustomUnitsName;
      Ratio = metersPerUnit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator UnitScale(double metersPerUnit) => new UnitScale(metersPerUnit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator double(UnitScale self) => (double) self.Ratio;
    #endregion

    #region UnitSystem
    UnitScale(UnitSystem system, Ratio metersPerUnit = default, string name = default)
    {
      switch (system)
      {
        case UnitSystem.Unset:        this = Unset;                                             break;
        case UnitSystem.None:         this = None;                                              break;
        case UnitSystem.CustomUnits:  Ratio = metersPerUnit;                    Name = name;    break;
        default:                      Ratio = metersPerUnitRatio[(int) system]; Name = default; break;
      }
    }

    public static explicit operator UnitScale(UnitSystem unitSystem) => new UnitScale(unitSystem);

    public static explicit operator UnitSystem(UnitScale self)
    {
      if (self.Name is object)            return UnitSystem.CustomUnits;

      double metersPerUnit =              self.Ratio.Quotient;
      if (!Ratio.IsFinite(metersPerUnit)) return UnitSystem.Unset;
      if (metersPerUnit == 0.0)           return UnitSystem.None;

      for (var u = UnitSystem.None + 1; u < UnitSystem.Parsecs + 1; ++u)
      {
        var m = metersPerUnitRatio[(int) u].Quotient;
        if (m == metersPerUnit) return u;
      }

      return UnitSystem.CustomUnits;
    }
    #endregion

    #region IEquatable
    public override bool Equals(object other) => other is UnitScale scale && Equals(scale);
    public bool Equals(UnitScale other) => this == other;

    public override int GetHashCode() => BitConverter.DoubleToInt64Bits(Ratio.Quotient).GetHashCode();

    public static bool operator ==(UnitScale left, UnitScale right) =>
      left.Name == right.Name &&
      BitConverter.DoubleToInt64Bits(left.Ratio.Quotient) ==
      BitConverter.DoubleToInt64Bits(right.Ratio.Quotient);

    public static bool operator !=(UnitScale left, UnitScale right) =>
      left.Name != right.Name ||
      BitConverter.DoubleToInt64Bits(left.Ratio.Quotient) !=
      BitConverter.DoubleToInt64Bits(right.Ratio.Quotient);
    #endregion

    public static UnitScale operator *(UnitScale left, UnitScale right) => new UnitScale(left.Ratio * right.Ratio);
    public static UnitScale operator /(UnitScale left, UnitScale right) => new UnitScale(left.Ratio / right.Ratio);

    public static double operator *(double value, UnitScale scale) => Math.Abs(value) * scale.Ratio;
    public static double operator /(double value, UnitScale scale) => Math.Abs(value) / scale.Ratio;
    public static double operator *(UnitScale scale, double value) => scale.Ratio * Math.Abs(value);
    public static double operator /(UnitScale scale, double value) => scale.Ratio / Math.Abs(value);

    public static double Convert(double value, UnitScale from, UnitScale to)
    {
      if (from.Ratio == to.Ratio)
        return value;

      // Deconstruct scales to ratios
      var (F, f) = from.Ratio;
      var (T, t) = to.Ratio;

      // F ⨯ Reciprocal(T)
      var num = F * t;
      var den = f * T;

      // Multiply value by resulting ratio considering magnitude.
      if (Math.Abs(num) < Math.Abs(value))
        return num * (value / den);
      else
        return value * (num / den);
    }

    public override string ToString() => Name ?? ((UnitSystem) this).ToString();

    public void Deconstruct(out UnitSystem unitSystem, out double metersPerUnit, out string name)
    {
      unitSystem = (UnitSystem) this;
      metersPerUnit = (double) this;
      name = ToString();
    }

    #region RhinoDoc Interop
    internal static UnitScale GetUnitScale(RhinoDoc doc, ActiveSpace space)
    {
      if (doc is null) return None;
      if (space == ActiveSpace.None)
        space = doc.Views.ModelSpaceIsActive ? ActiveSpace.ModelSpace : ActiveSpace.PageSpace;

      var system = space == ActiveSpace.ModelSpace ? doc.ModelUnitSystem : doc.PageUnitSystem;
      return  system == UnitSystem.CustomUnits && doc.GetCustomUnitSystem(space == ActiveSpace.ModelSpace, out var name, out var meters) ?
        new UnitScale(system, Ratio.Rationalize(meters), name) :
        new UnitScale(system);
    }
    internal static void SetUnitSystem(RhinoDoc doc, ActiveSpace space, UnitScale value, bool scale = true)
    {
      if (doc is null)
        throw new ArgumentNullException(nameof(doc));

      if (space == ActiveSpace.None)
        space = doc.Views.ModelSpaceIsActive ? ActiveSpace.ModelSpace : ActiveSpace.PageSpace;

      var (system, meters, name) = value;
      if (system == UnitSystem.CustomUnits)
        doc.SetCustomUnitSystem(space == ActiveSpace.ModelSpace, name, meters, scale);
      else if (space == ActiveSpace.ModelSpace)
        doc.ModelUnitSystem = system;
      else if (space == ActiveSpace.PageSpace)
        doc.PageUnitSystem = system;
    }

    public static UnitScale GetActiveScale (RhinoDoc doc) => GetUnitScale(doc, ActiveSpace.None);
    public static UnitScale GetModelScale  (RhinoDoc doc) => GetUnitScale(doc, ActiveSpace.ModelSpace);
    public static UnitScale GetPageScale   (RhinoDoc doc) => GetUnitScale(doc, ActiveSpace.PageSpace);

    public static void SetActiveUnitSystem(RhinoDoc doc, UnitScale value, bool scale /*= true*/) =>
      SetUnitSystem(doc, ActiveSpace.None, value, scale);
    public static void SetModelUnitSystem(RhinoDoc doc, UnitScale value, bool scale /*= true*/) =>
      SetUnitSystem(doc, ActiveSpace.ModelSpace, value, scale);
    public static void SetPageUnitSystem(RhinoDoc doc, UnitScale value, bool scale /*= true*/) =>
      SetUnitSystem(doc, ActiveSpace.PageSpace, value, scale);
    #endregion
  }
}
