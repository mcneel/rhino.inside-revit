using System.Diagnostics;
using RhinoInside.Revit.External.DB.Extensions;
using static System.Math;
using static Rhino.RhinoMath;
using DB = Autodesk.Revit.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.Convert.Units
{
  static class DisplayUnitTypeConverter
  {
#if DEBUG
    static DisplayUnitTypeConverter()
    {
#if REVIT_2021
      var lengthUnits = DB.UnitUtils.GetValidUnits(DBXS.SpecType.Measurable.Length);
#else
      var lengthUnits = DB.UnitUtils.GetValidDisplayUnits(DB.UnitType.UT_Length);
#endif
      // Verify all conversions are implementd.
      foreach (var unit in lengthUnits)
        ToUnitSystem(unit);
    }
#endif

    public static Rhino.UnitSystem ToUnitSystem(this DBXS.UnitType value)
    {
      if (!DBXS.SpecType.Measurable.Length.IsValidUnitType(value))
        throw new ConversionException($"{value} is not a length unit");

      if (value == DBXS.UnitType.Meters) return Rhino.UnitSystem.Meters;
      if (value == DBXS.UnitType.MetersCentimeters) return Rhino.UnitSystem.Meters;
      if (value == DBXS.UnitType.Decimeters) return Rhino.UnitSystem.Decimeters;
      if (value == DBXS.UnitType.Centimeters) return Rhino.UnitSystem.Centimeters;
      if (value == DBXS.UnitType.Millimeters) return Rhino.UnitSystem.Millimeters;

      if (value == DBXS.UnitType.Inches) return Rhino.UnitSystem.Inches;
      if (value == DBXS.UnitType.FractionalInches) return Rhino.UnitSystem.Inches;
      if (value == DBXS.UnitType.Feet) return Rhino.UnitSystem.Feet;
      if (value == DBXS.UnitType.FeetFractionalInches) return Rhino.UnitSystem.Feet;
      if (value == DBXS.UnitType.UsSurveyFeet) return Rhino.UnitSystem.Feet;

      Debug.Fail($"{value} conversion is not implemented");
      return Rhino.UnitSystem.Unset;
    }

    public static DBXS.UnitType ToUnitType(this Rhino.UnitSystem value)
    {
      switch (value)
      {
        case Rhino.UnitSystem.Kilometers:   return DBXS.UnitType.Kilometers;
        case Rhino.UnitSystem.Hectometers:  return DBXS.UnitType.Hectometers;
        case Rhino.UnitSystem.Meters:       return DBXS.UnitType.Meters;
        case Rhino.UnitSystem.Decimeters:   return DBXS.UnitType.Decimeters;
        case Rhino.UnitSystem.Centimeters:  return DBXS.UnitType.Centimeters;
        case Rhino.UnitSystem.Millimeters:  return DBXS.UnitType.Millimeters;

        case Rhino.UnitSystem.Miles:        return DBXS.UnitType.Miles;
        case Rhino.UnitSystem.Yards:        return DBXS.UnitType.Yards;
        case Rhino.UnitSystem.Feet:         return DBXS.UnitType.Feet;
        case Rhino.UnitSystem.Inches:       return DBXS.UnitType.Inches;
      }

      Debug.Fail($"{value} conversion is not implemented");
      return DBXS.UnitType.Empty;
    }

    public static Rhino.UnitSystem ToUnitSystem(this DB.Units value, out int distanceDisplayPrecision)
    {
      var lengthFormatoptions = value.GetFormatOptions(DBXS.SpecType.Measurable.Length);
      distanceDisplayPrecision = Clamp((int) -Log10(lengthFormatoptions.Accuracy), 0, 7);
      return ToUnitSystem(lengthFormatoptions.GetUnitTypeId());
    }
  }
}

