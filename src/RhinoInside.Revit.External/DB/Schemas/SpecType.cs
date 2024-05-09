using System;
using System.Collections.Generic;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.UnitType
  /// </summary>
  public partial class SpecType : DataType
  {
    public static new SpecType Empty { get; } = new SpecType();
    public static SpecType Custom { get; } = new SpecType("autodesk.spec:custom-1.0.0");

    public override string LocalizedLabel => IsNullOrEmpty(this) ? string.Empty :
#if REVIT_2021
      Autodesk.Revit.DB.LabelUtils.GetLabelForSpec(this);
#else
      Autodesk.Revit.DB.LabelUtils.GetLabelFor((Autodesk.Revit.DB.UnitType) this);
#endif

    public SpecType() { }
    public SpecType(string id) : base(id)
    {
      if (!IsSpecType(id, empty: true))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    #region IParsable
    public static bool TryParse(string s, IFormatProvider provider, out SpecType result)
    {
      if (IsSpecType(s, empty: true))
      {
        result = new SpecType(s);
        return true;
      }

      result = default;
      return false;
    }

    public static SpecType Parse(string s, IFormatProvider provider)
    {
      if (!TryParse(s, provider, out var result)) throw new FormatException($"{nameof(s)} is not in the correct format.");
      return result;
    }

    static bool IsSpecType(string id, bool empty)
    {
      return (empty && id == string.Empty) || // 'Other'
             id.StartsWith("autodesk.spec");
    }
    #endregion

    public static bool IsSpecType(DataType value, out SpecType specType)
    {
      switch (value)
      {
        case SpecType st: specType = st;return true;
        default:

          var typeId = value.TypeId;
          if (IsSpecType(typeId, empty: false))
          {
            specType = new SpecType(typeId);
            return true;
          }

          specType = default;
          return false;
      }
    }

    public static bool IsMeasurableSpec(DataType value, out SpecType specType)
    {
      var typeId = value.TypeId;
#if REVIT_2022
      if (Autodesk.Revit.DB.UnitUtils.IsMeasurableSpec(value))
#else
      if (typeId.StartsWith("autodesk.spec.aec"))
#endif
      {
        specType = new SpecType(typeId);
        return true;
      }

      specType = default;
      return false;
    }

    /// <summary>
    /// Checks whether a unit type is valid for this spec.
    /// </summary>
    /// <param name="unitType"></param>
    /// <returns></returns>
    public bool IsValidUnitType(UnitType unitType)
    {
#if REVIT_2021
      return Autodesk.Revit.DB.UnitUtils.IsValidUnit(this, unitType);
#else
      return Autodesk.Revit.DB.UnitUtils.IsValidDisplayUnit(this, unitType);
#endif
    }

    public DisciplineType DisciplineType
    {
      get
      {
        if (this != Empty)
        {
#if REVIT_2022
          if (Autodesk.Revit.DB.UnitUtils.IsMeasurableSpec(this))
            return Autodesk.Revit.DB.UnitUtils.GetDiscipline(this);

#else
          if (Namespace == "autodesk.spec.aec.electrical") return DisciplineType.Electrical;
          if (Namespace == "autodesk.spec.aec.energy") return DisciplineType.Energy;
          if (Namespace == "autodesk.spec.aec.hvac") return DisciplineType.Hvac;
          if (Namespace == "autodesk.spec.aec.infrastructure") return DisciplineType.Infrastructure;
          if (Namespace == "autodesk.spec.aec.piping") return DisciplineType.Piping;
          if (Namespace == "autodesk.spec.aec.structural") return DisciplineType.Structural;
#endif
          return DisciplineType.Common;
        }

        return DisciplineType.Empty;
      }
    }

#region Dimensionality
    internal static readonly IReadOnlyDictionary<SpecType, int> LengthDimensionality = new Dictionary<SpecType, int>
    {
      #region Common
      { Measurable.Length, +1 },
      { Measurable.Area, +2 },
      { Measurable.Volume, +3 },
      { Measurable.Distance, +1 },
      { Measurable.Speed, +1 },
      { Measurable.MassDensity, -3 },
      { Measurable.CostPerArea, -2 },
      { Measurable.SheetLength, +1 },
      { Measurable.DecimalSheetLength, +1 },
      #endregion

      #region Structural
      { Measurable.Force, +1 },
      { Measurable.LinearForce, +1 -1 },
      { Measurable.AreaForce, +1 -2 },
      { Measurable.StructuralVelocity, +1 },
      { Measurable.Moment, +1 -1 },
      { Measurable.MomentOfInertia, +4 },
      { Measurable.LinearMoment, +1 +1 -1 },
      { Measurable.Stress, -1 },
      { Measurable.UnitWeight, +1 -3 },
      { Measurable.Weight, +1 },
      { Measurable.WeightPerUnitLength, +1 -1 },
      { Measurable.MassPerUnitLength, -1 },
      { Measurable.MassPerUnitArea, -2 },
      { Measurable.PointSpringCoefficient, +1 -1 },
      { Measurable.LineSpringCoefficient, +1 -2 },
      { Measurable.AreaSpringCoefficient, +1 -3 },
      { Measurable.RotationalPointSpringCoefficient, +1 +1 },
      { Measurable.RotationalLineSpringCoefficient, +1 +1 -1 },
      { Measurable.Displacement, +1 },
      { Measurable.Acceleration, +1 },
      { Measurable.Energy, +2 },
      { Measurable.ReinforcementLength, +1 },
      { Measurable.ReinforcementArea, +2 },
      { Measurable.ReinforcementVolume, +3 },
      { Measurable.ReinforcementAreaPerUnitLength, +2 -1 },
      { Measurable.ReinforcementSpacing, +1 },
      { Measurable.ReinforcementCover, +1 },
      { Measurable.BarDiameter, +1 },
      { Measurable.CrackWidth, +1 },
      { Measurable.SectionDimension, +1 },
      { Measurable.SectionProperty, +1 },
      { Measurable.SectionArea, +2 },
      { Measurable.SectionModulus, +3 },
      { Measurable.WarpingConstant, +6 },
      { Measurable.SurfaceAreaPerUnitLength, +2 -1 },
      #endregion

      #region HVAC
      { Measurable.HvacEnergy, +2 },
      { Measurable.HvacDensity, -3 },
      { Measurable.HvacFriction, -1 -1 },
      { Measurable.HvacPowerDensity, -2 },
      { Measurable.HvacPressure, -1 },
      { Measurable.HvacVelocity, +1 },
      { Measurable.AirFlow, +3 },
      { Measurable.DuctSize, +1 },
      { Measurable.CrossSection, +2 },
      { Measurable.HvacRoughness, +1 },
      { Measurable.HvacViscosity, -1 },
      { Measurable.AirFlowDensity, +3 -2 },
      { Measurable.CoolingLoadDividedByArea, -2 },
      { Measurable.HeatingLoadDividedByArea, -2 },
      { Measurable.CoolingLoadDividedByVolume, -3 },
      { Measurable.HeatingLoadDividedByVolume, -3 },
      { Measurable.AirFlowDividedByVolume, +3 -3 },
      { Measurable.AirFlowDividedByCoolingLoad, +3 },
      { Measurable.AreaDividedByCoolingLoad, +2 },
      { Measurable.AreaDividedByHeatingLoad, +2 },
      { Measurable.DuctInsulationThickness, +1 },
      { Measurable.DuctLiningThickness, +1 },
      #endregion

      #region Electrical
      { Measurable.Luminance, -2 },
      { Measurable.ElectricalPowerDensity, -2 },
      { Measurable.ElectricalResistivity, +1 },
      { Measurable.WireDiameter, +1 },
      { Measurable.CableTraySize, +1 },
      { Measurable.ConduitSize, +1 },
      #endregion

      #region Piping
      { Measurable.PipingDensity, -3 },
      { Measurable.Flow, +3 },
      { Measurable.PipingFriction, -1 +1 },
      { Measurable.PipingPressure, -1 },
      { Measurable.PipingVelocity, +1 },
      { Measurable.PipingViscosity, -1 },
      { Measurable.PipeSize, +1 },
      { Measurable.PipingRoughness, +1 },
      { Measurable.PipingVolume, +3 },
      { Measurable.PipeInsulationThickness, +1 },
      { Measurable.PipeDimension, +1 },
      { Measurable.PipingMass, 0 },
      { Measurable.PipeMassPerUnitLength, -1 },
      #endregion

      #region Energy
      { Measurable.HeatTransferCoefficient, -2 },
      { Measurable.ThermalResistance, +2 },
      { Measurable.ThermalMass, +2 },
      { Measurable.ThermalConductivity, -1 },
      { Measurable.SpecificHeat, +2 },
      { Measurable.SpecificHeatOfVaporization, +2 },
      { Measurable.Permeability, +1 -2 },
      #endregion
    };

    internal bool TryGetLengthDimensionality(out int dimensionality) =>
      LengthDimensionality.TryGetValue(this, out dimensionality);
    #endregion

#if REVIT_2021
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(SpecType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
    public static implicit operator SpecType(Autodesk.Revit.DB.ForgeTypeId value)
    {
      if (value is null) return null;
      var typeId = value.TypeId;
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
      return IsSpecType(typeId, empty: true) ?
        new SpecType(typeId) :
        throw new InvalidCastException($"'{typeId}' is not a valid {typeof(SpecType)}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
#endif

#if !REVIT_2021
#pragma warning disable CS0618 // Type or member is obsolete
    public static implicit operator SpecType(Autodesk.Revit.DB.UnitType value)
    {
      foreach (var item in map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.UnitType(SpecType value)
    {
      if (map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.UnitType) ut;

      return Autodesk.Revit.DB.UnitType.UT_Undefined;
    }
#pragma warning restore CS0618 // Type or member is obsolete
#endif
  }
}

#if !REVIT_2021
namespace RhinoInside.Revit.External.DB.Extensions
{
  static class SpecTypeExtension
  {
    internal static Schemas.SpecType GetSpecTypeId(this Autodesk.Revit.DB.FamilySizeTableColumn value) => value.UnitType;
  }
}
#endif
