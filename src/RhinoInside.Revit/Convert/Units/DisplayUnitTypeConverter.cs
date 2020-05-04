using System.Diagnostics;
using Rhino;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Units
{
  static class DisplayUnitTypeConverter
  {
    static class ToUnitSystemStatic
    {
      static ToUnitSystemStatic()
      {
        foreach (var unit in DB.UnitUtils.GetValidDisplayUnits(DB.UnitType.UT_Length))
        {
          var revit = DB.UnitUtils.Convert(1.0, DB.DisplayUnitType.DUT_METERS, unit);
          var rhino = RhinoMath.UnitScale(UnitSystem.Meters, unit.ToUnitSystem());
          //Debug.Assert(Rhino.RhinoMath.EpsilonEquals(revit, rhino, Rhino.RhinoMath.ZeroTolerance), $"ToRhinoLengthUnits({unit}) fails!!");
        }
      }

      [Conditional("DEBUG")]
      internal static void Assert() { }
    }

    public static UnitSystem ToUnitSystem(this DB.DisplayUnitType value)
    {
      ToUnitSystemStatic.Assert();

      if (!DB.UnitUtils.IsValidDisplayUnit(DB.UnitType.UT_Length, value))
        throw new ConversionException($"{value} is not a length unit");

      switch (value)
      {
        case DB.DisplayUnitType.DUT_METERS: return Rhino.UnitSystem.Meters;
        case DB.DisplayUnitType.DUT_METERS_CENTIMETERS: return Rhino.UnitSystem.Meters;
        case DB.DisplayUnitType.DUT_DECIMETERS: return Rhino.UnitSystem.Decimeters;
        case DB.DisplayUnitType.DUT_CENTIMETERS: return Rhino.UnitSystem.Centimeters;
        case DB.DisplayUnitType.DUT_MILLIMETERS: return Rhino.UnitSystem.Millimeters;

        case DB.DisplayUnitType.DUT_FRACTIONAL_INCHES: return Rhino.UnitSystem.Inches;
        case DB.DisplayUnitType.DUT_DECIMAL_INCHES: return Rhino.UnitSystem.Inches;
        case DB.DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES: return Rhino.UnitSystem.Feet;
        case DB.DisplayUnitType.DUT_DECIMAL_FEET: return Rhino.UnitSystem.Feet;
#if REVIT_2021
        case DB.DisplayUnitType.DUT_DECIMAL_US_SURVEY_FEET: return Rhino.UnitSystem.Feet;
#endif
      }

      Debug.Fail($"{value} conversion is not implemented");
      return Rhino.UnitSystem.Unset;
    }

    public static DB.DisplayUnitType ToDisplayUnitType(this UnitSystem value)
    {
      switch (value)
      {
        case Rhino.UnitSystem.Meters: return DB.DisplayUnitType.DUT_METERS;
        case Rhino.UnitSystem.Decimeters: return DB.DisplayUnitType.DUT_DECIMETERS;
        case Rhino.UnitSystem.Centimeters: return DB.DisplayUnitType.DUT_CENTIMETERS;
        case Rhino.UnitSystem.Millimeters: return DB.DisplayUnitType.DUT_MILLIMETERS;

        case Rhino.UnitSystem.Inches: return DB.DisplayUnitType.DUT_DECIMAL_INCHES;
        case Rhino.UnitSystem.Feet: return DB.DisplayUnitType.DUT_DECIMAL_FEET;
      }

      Debug.Fail($"{value} conversion is not implemented");
      return DB.DisplayUnitType.DUT_UNDEFINED;
    }
  }
}

