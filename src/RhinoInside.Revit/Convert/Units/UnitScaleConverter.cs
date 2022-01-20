using System.Diagnostics;
using static System.Math;
using static Rhino.RhinoMath;
using ARDB = Autodesk.Revit.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.Convert.Units
{
  using External.DB.Extensions;

  static class UnitScaleConverter
  {
#if DEBUG
    static UnitScaleConverter()
    {
#if REVIT_2021
      var lengthUnits = ARDB.UnitUtils.GetValidUnits(DBXS.SpecType.Measurable.Length);
#else
      var lengthUnits = ARDB.UnitUtils.GetValidDisplayUnits(ARDB.UnitType.UT_Length);
#endif
      // Verify all conversions are implementd.
      foreach (var unit in lengthUnits)
        ToUnitScale(unit);
    }
#endif

    public static UnitScale ToUnitScale(this DBXS.UnitType value)
    {
      if (!DBXS.SpecType.Measurable.Length.IsValidUnitType(value))
        throw new ConversionException($"{value} is not a length unit");

      if (value == DBXS.UnitType.Meters) return UnitScale.Meters;
      if (value == DBXS.UnitType.MetersCentimeters) return UnitScale.Meters;
      if (value == DBXS.UnitType.Decimeters) return UnitScale.Decimeters;
      if (value == DBXS.UnitType.Centimeters) return UnitScale.Centimeters;
      if (value == DBXS.UnitType.Millimeters) return UnitScale.Millimeters;

      if (value == DBXS.UnitType.Inches) return UnitScale.Inches;
      if (value == DBXS.UnitType.FractionalInches) return UnitScale.Inches;
      if (value == DBXS.UnitType.Feet) return UnitScale.Feet;
      if (value == DBXS.UnitType.FeetFractionalInches) return UnitScale.Feet;
      if (value == DBXS.UnitType.UsSurveyFeet) return UnitScale.UsSurveyFeet;

      Debug.Fail($"{value} conversion is not implemented");
      return UnitScale.Unset;
    }

    public static UnitScale ToUnitScale(this ARDB.Units value, out int distanceDisplayPrecision)
    {
      var lengthFormatoptions = value.GetFormatOptions(DBXS.SpecType.Measurable.Length);
      distanceDisplayPrecision = Clamp((int) -Log10(lengthFormatoptions.Accuracy), 0, 7);
      return ToUnitScale(lengthFormatoptions.GetUnitTypeId());
    }
  }
}

