using System.Diagnostics;
using Rhino;
using static System.Math;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Units
{
  static class DisplayUnitTypeConverter
  {
    static class ToUnitSystemStatic
    {
      static ToUnitSystemStatic()
      {
#if REVIT_2022
        foreach (var unit in DB.UnitUtils.GetValidUnits(DB.SpecTypeId.Length))
        {
          var revit = DB.UnitUtils.Convert(1.0, DB.UnitTypeId.Meters, unit);
          var rhino = RhinoMath.UnitScale(UnitSystem.Meters, unit.ToUnitSystem());
          //Debug.Assert(Rhino.RhinoMath.EpsilonEquals(revit, rhino, Rhino.RhinoMath.ZeroTolerance), $"ToRhinoLengthUnits({unit}) fails!!");
        }
#else
        foreach (var unit in DB.UnitUtils.GetValidDisplayUnits(DB.UnitType.UT_Length))
        {
          var revit = DB.UnitUtils.Convert(1.0, DB.DisplayUnitType.DUT_METERS, unit);
          var rhino = RhinoMath.UnitScale(UnitSystem.Meters, unit.ToUnitSystem());
          //Debug.Assert(Rhino.RhinoMath.EpsilonEquals(revit, rhino, Rhino.RhinoMath.ZeroTolerance), $"ToRhinoLengthUnits({unit}) fails!!");
        }
#endif
      }

      [Conditional("DEBUG")]
      internal static void Assert() { }
    }

#if REVIT_2022
    public static UnitSystem ToUnitSystem(this DB.ForgeTypeId value)
    {
      ToUnitSystemStatic.Assert();

      if (!DB.UnitUtils.IsValidUnit(DB.SpecTypeId.Length, value))
        throw new ConversionException($"{value} is not a length unit");

      if (value.Equals(DB.UnitTypeId.Meters)
          || value.Equals(DB.UnitTypeId.MetersCentimeters))
        return Rhino.UnitSystem.Meters;
      else if (value.Equals(DB.UnitTypeId.Decimeters))
        return Rhino.UnitSystem.Decimeters;
      else if (value.Equals(DB.UnitTypeId.Centimeters))
        return Rhino.UnitSystem.Centimeters;
      else if (value.Equals(DB.UnitTypeId.Millimeters))
        return Rhino.UnitSystem.Millimeters;

      else if (value.Equals(DB.UnitTypeId.Inches)
                || value.Equals(DB.UnitTypeId.FractionalInches))
        return Rhino.UnitSystem.Inches;
      else if (value.Equals(DB.UnitTypeId.Feet)
                || value.Equals(DB.UnitTypeId.FeetFractionalInches)
                || value.Equals(DB.UnitTypeId.UsSurveyFeet))
        return Rhino.UnitSystem.Feet;

      else if (value.Equals(DB.UnitTypeId.Millimeters))
        return Rhino.UnitSystem.Millimeters;

      Debug.Fail($"{value} conversion is not implemented");
      return Rhino.UnitSystem.Unset;
    }
#else
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
#endif

    public static UnitSystem ToUnitSystem(this DB.Units value)
    {
#if REVIT_2022
      var lengthFormatoptions = value.GetFormatOptions(DB.SpecTypeId.Length);
      return lengthFormatoptions.GetUnitTypeId().ToUnitSystem();
#else
        var lengthFormatoptions = value.GetFormatOptions(DB.UnitType.UT_Length);
        return lengthFormatoptions.DisplayUnits.ToUnitSystem();
#endif
    }

#if REVIT_2022
    public static DB.ForgeTypeId ToDisplayUnitType(this Rhino.UnitSystem value)
    {
      switch (value)
      {
        case Rhino.UnitSystem.Meters: return DB.UnitTypeId.Meters;
        case Rhino.UnitSystem.Decimeters: return DB.UnitTypeId.Decimeters;
        case Rhino.UnitSystem.Centimeters: return DB.UnitTypeId.Centimeters;
        case Rhino.UnitSystem.Millimeters: return DB.UnitTypeId.Millimeters;

        case Rhino.UnitSystem.Inches: return DB.UnitTypeId.Inches;
        case Rhino.UnitSystem.Feet: return DB.UnitTypeId.Feet;
      }

      Debug.Fail($"{value} conversion is not implemented");
      return null;
    }
#else
    public static DB.DisplayUnitType ToDisplayUnitType(this Rhino.UnitSystem value)
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
#endif

    public static double Convert(this DB.Visual.AssetPropertyDistance value, Rhino.UnitSystem unitSystem)
    {
#if REVIT_2022
      return DB.UnitUtils.Convert(value.Value, value.GetUnitTypeId(), unitSystem.ToDisplayUnitType());
#else
      return DB.UnitUtils.Convert(value.Value, value.DisplayUnitType, unitSystem.ToDisplayUnitType());
#endif
    }

    public static double ScaleToRhino(this DB.Visual.AssetPropertyDistance value, Rhino.UnitSystem unitSystem)
    {
#if REVIT_2022
      return value.Value * Rhino.RhinoMath.UnitScale(value.GetUnitTypeId().ToUnitSystem(), unitSystem);
#else
      return value.Value * Rhino.RhinoMath.UnitScale(value.DisplayUnitType.ToUnitSystem(), unitSystem);
#endif
    }

    public static double ScaleToRevit(this DB.Visual.AssetPropertyDistance value, Rhino.UnitSystem unitSystem)
    {
#if REVIT_2022
      return value.Value * Rhino.RhinoMath.UnitScale(unitSystem, value.GetUnitTypeId().ToUnitSystem());
#else
      return value.Value * Rhino.RhinoMath.UnitScale(unitSystem, value.DisplayUnitType.ToUnitSystem());
#endif
    }

    public static double ConvertAsLength(this double value, DB.Document doc, Rhino.UnitSystem unitSystem)
    {
#if REVIT_2022
      return DB.UnitUtils.Convert(value, unitSystem.ToDisplayUnitType(), doc.GetUnits().GetFormatOptions(DB.SpecTypeId.Length).GetUnitTypeId());
#else
      return DB.UnitUtils.Convert(value, unitSystem.ToDisplayUnitType(), doc.GetUnits().GetFormatOptions(DB.UnitType.UT_Length).DisplayUnits);
#endif
    }

    public static int CalculateModelDistanceDisplayPrecision(this DB.Units value)
    {
#if REVIT_2022
      var lengthFormatoptions = value.GetFormatOptions(DB.SpecTypeId.Length);
#else
      var lengthFormatoptions = value.GetFormatOptions(DB.UnitType.UT_Length);
#endif
      return (int) -Log10(lengthFormatoptions.Accuracy);
    }

#if REVIT_2022
    public static DB.ForgeTypeId GetUnitType(this DB.Definition value) => value.GetDataType();
#else
    public static DB.UnitType GetUnitType(this DB.Definition value) => value.UnitType;
#endif

    public static bool IsNumberParameter(this DB.Definition value)
    {
#if REVIT_2022
      return value.GetDataType().Equals(DB.SpecTypeId.Number);
#else
      return value.UnitType == DB.UnitType.UT_Number;
#endif
    }

    public static string GetDataTypeLabel(this DB.Definition value)
    {
#if REVIT_2022
      return DB.LabelUtils.GetLabelForSpec(value.GetDataType());
#else
      return DB.LabelUtils.GetLabelFor(value.UnitType);
#endif
    }
  }
}

