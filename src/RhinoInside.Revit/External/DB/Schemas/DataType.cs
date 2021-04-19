using System;
using System.Collections.Generic;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.ForgeTypeId
  /// </summary>
  public partial class DataType : IEquatable<DataType>, IComparable<DataType>, IComparable
  {
    readonly string id;

    public string TypeId => id;

    /// <summary>
    /// Gets the fully qualified name of the type, including its namespace.
    /// </summary>
    public string FullName
    {
      get
      {
        if (id is null) return string.Empty;

        var start = id.IndexOf(':') + 1;
        if (start == 0) return string.Empty;

        var end = id.IndexOf('-', start);
        if (end < 0) end = id.Length;

        return id.Substring(0, end);
      }
    }

    /// <summary>
    /// Gets the namespace of the DataType
    /// </summary>
    public string Namespace
    {
      get
      {
        if (id is null) return string.Empty;

        var index = id.IndexOf(':');
        return index < 0 ? id : id.Substring(0, index);
      }
    }

    public string Name
    {
      get
      {
        if (id is null) return string.Empty;

        var start = id.IndexOf(':') + 1;
        if (start == 0) return string.Empty;

        var end = id.IndexOf('-', start);
        if (end < 0) end = id.Length;

        return id.Substring(start, end - start);
      }
    }

    public string Version
    {
      get
      {
        if (id is null) return string.Empty;

        var start = id.IndexOf(':') + 1;
        if (start == 0) return string.Empty;

        var index = id.IndexOf('-', start) + 1;
        return index == 0 ? string.Empty : id.Substring(index, id.Length - index);
      }
    }

    static readonly DataType empty = new DataType();
    public static DataType Empty => empty;

    public DataType() => id = string.Empty;
    public DataType(string typeId) => id = typeId;

    #region System.Object
    public override string ToString() => id;
    public override int GetHashCode() => FullName.GetHashCode();
    #endregion

    #region IComparable
    int IComparable.CompareTo(object obj) => ((IComparable<DataType>) this).CompareTo(obj as DataType);

    int IComparable<DataType>.CompareTo(DataType other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      return string.CompareOrdinal(FullName, other.FullName);
    }
    #endregion

    #region IEquatable
    public override bool Equals(object other) => other is DataType schemaTypeId && Equals(schemaTypeId);
    public bool Equals(DataType other) => FullName == other?.FullName;
    #endregion

    public static bool operator == (DataType lhs, DataType rhs) =>  (ReferenceEquals(lhs, rhs) || lhs.Equals(rhs));
    public static bool operator != (DataType lhs, DataType rhs) => !(ReferenceEquals(lhs, rhs) || lhs.Equals(rhs));

#if REVIT_2021
    public static implicit operator DataType(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new DataType(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(DataType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

#if !REVIT_2022
#pragma warning disable CS0618 // Type or member is obsolete
    public static implicit operator DataType(Autodesk.Revit.DB.ParameterType value)
    {
      if (value is Autodesk.Revit.DB.ParameterType.FamilyType)
        return new CategoryId("autodesk.revit.category.family");

      foreach (var item in parameterTypeMap)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.ParameterType(DataType value)
    {
      if (CategoryId.IsCategoryId(value, out var _))
        return Autodesk.Revit.DB.ParameterType.FamilyType;

      if (parameterTypeMap.TryGetValue(value, out var pt))
        return (Autodesk.Revit.DB.ParameterType) pt;

      return Autodesk.Revit.DB.ParameterType.Invalid;
    }

    static readonly Dictionary<DataType, int> parameterTypeMap = new Dictionary<DataType, int>()
    {
      { SpecType.String.Text, 1 }, // ParameterType.Text
      { SpecType.Int.Integer, 2 }, // ParameterType.Integer
      { SpecType.Measurable.Number, 3 }, // ParameterType.Number
      { SpecType.Measurable.Length, 4 }, // ParameterType.Length
      { SpecType.Measurable.Area, 5 }, // ParameterType.Area
      { SpecType.Measurable.Volume, 6 }, // ParameterType.Volme
      { SpecType.Measurable.Angle, 7 }, // ParameterType.Angle
      { SpecType.String.Url, 8}, // ParameterType.URL
      { SpecType.Reference.Material, 9}, // ParameterType.Material
      { SpecType.Boolean.YesNo, 10}, // ParameterType.YesNo
      { SpecType.Measurable.Force, 11}, // ParameterType.Force
      { SpecType.Measurable.LinearForce, 12}, // ParameterType.LinearForce
      { SpecType.Measurable.AreaForce, 13}, // ParameterType.AreaForce
      { SpecType.Measurable.Moment, 14}, // ParameterType.Moment
      { SpecType.Int.NumberOfPoles, 15}, // ParameterType.NumberOfPoles
      //{ SpecType.Measurable.FixtureUnit, 16}, // ParameterType.FixtureUnit
      //{ Reference.FamilyType, 17}, // ParameterType.FamilyType
      { SpecType.Reference.LoadClassification, 18}, // ParameterType.LoadClassification
      { SpecType.Reference.Image, 19}, // ParameterType.Image
      { SpecType.String.MultilineText, 20}, // ParameterType.MultilineText
      { SpecType.Custom, 99}, // ParameterType.Custom
      { SpecType.Measurable.HvacDensity, 107}, // ParameterType.HVACDensity
      { SpecType.Measurable.HvacEnergy, 108}, // ParameterType.HVACEnergy
      { SpecType.Measurable.HvacFriction, 109}, // ParameterType.HVACFriction
      { SpecType.Measurable.HvacPower, 110}, // ParameterType.HVACPower
      { SpecType.Measurable.HvacPowerDensity, 111}, // ParameterType.HVACPowerDensity
      { SpecType.Measurable.HvacPressure, 112}, // ParameterType.HVACPressure
      { SpecType.Measurable.HvacTemperature, 113}, // ParameterType.HVACTemperature
      { SpecType.Measurable.HvacVelocity, 114}, // ParameterType.HVACVelocity
      { SpecType.Measurable.AirFlow, 115}, // ParameterType.HVACAirflow
      { SpecType.Measurable.DuctSize, 116}, // ParameterType.HVACDuctSize
      { SpecType.Measurable.CrossSection, 117}, // ParameterType.HVACCrossSection
      { SpecType.Measurable.HeatGain, 118}, // ParameterType.HVACHeatGain
      { SpecType.Measurable.Current, 119}, // ParameterType.ElectricalCurrent
      { SpecType.Measurable.ElectricalPotential, 120}, // ParameterType.ElectricalPotential
      { SpecType.Measurable.ElectricalFrequency, 121}, // ParameterType.ElectricalFrequency
      { SpecType.Measurable.Illuminance, 122}, // ParameterType.ElectricalFrequency
      { SpecType.Measurable.LuminousFlux, 123}, // ParameterType.ElectricalLuminousFlux
      { SpecType.Measurable.ElectricalPower, 124}, // ParameterType.ElectricalPower
      { SpecType.Measurable.HvacRoughness, 125}, // ParameterType.HVACRoughness
      { SpecType.Measurable.ApparentPower, 134}, // ParameterType.ElectricalApparentPower
      { SpecType.Measurable.ElectricalPowerDensity, 135}, // ParameterType.ElectricalPowerDensity
      { SpecType.Measurable.PipingDensity, 136}, // ParameterType.PipingDensity
      { SpecType.Measurable.Flow, 137}, // ParameterType.PipingFlow
      { SpecType.Measurable.PipingFriction, 138}, // ParameterType.PipingFriction
      { SpecType.Measurable.PipingPressure, 139}, // ParameterType.PipingPressure
      { SpecType.Measurable.PipingTemperature, 140}, // ParameterType.PipingTemperature
      { SpecType.Measurable.PipingVelocity, 141}, // ParameterType.PipingVelocity
      { SpecType.Measurable.PipingViscosity, 142}, // ParameterType.PipingViscosity
      { SpecType.Measurable.PipeSize, 143}, // ParameterType.PipeSize
      { SpecType.Measurable.PipingRoughness, 144}, // ParameterType.PipingRoughness
      { SpecType.Measurable.Stress, 145}, // ParameterType.Stress
      { SpecType.Measurable.UnitWeight, 146}, // ParameterType.UnitWeight
      { SpecType.Measurable.ThermalExpansionCoefficient, 147}, // ParameterType.ThermalExpansion
      { SpecType.Measurable.LinearMoment, 148}, // ParameterType.LinearMoment
      //{ SpecType.Measurable.ForcePerLength, 150}, // ParameterType.ForcePerLength
      //{ SpecType.Measurable.ForceLengthPerAngle, 151}, // ParameterType.ForceLengthPerAngle
      //{ SpecType.Measurable.LinearForcePerLength, 152}, // ParameterType.LinearForcePerLength
      //{ SpecType.Measurable.LinearForceLengthPerAngle, 153}, // ParameterType.LinearForceLengthPerAngle
      //{ SpecType.Measurable.AreaForcePerLength, 154}, // ParameterType.AreaForcePerLength
      { SpecType.Measurable.PipingVolume, 155}, // ParameterType.PipingVolume
      { SpecType.Measurable.HvacViscosity, 156}, // ParameterType.HVACViscosity
      { SpecType.Measurable.HeatTransferCoefficient, 157}, // ParameterType.HVACCoefficientOfHeatTransfer
      { SpecType.Measurable.AirFlowDensity, 158}, // ParameterType.HVACAirflowDensity
      { SpecType.Measurable.Slope, 159}, // ParameterType.Slope
      { SpecType.Measurable.CoolingLoad, 160}, // ParameterType.HVACCoolingLoad
      { SpecType.Measurable.CoolingLoadDividedByArea, 161}, // ParameterType.HVACCoolingLoadDividedByArea
      { SpecType.Measurable.CoolingLoadDividedByVolume, 162}, // ParameterType.HVACCoolingLoadDividedByVolume
      { SpecType.Measurable.HeatingLoad, 163}, // ParameterType.HVACHeatingLoad
      { SpecType.Measurable.HeatingLoadDividedByArea, 164}, // ParameterType.HVACHeatingLoadDividedByArea
      { SpecType.Measurable.HeatingLoadDividedByVolume, 165}, // ParameterType.HVACHeatingLoadDividedByVolume
      { SpecType.Measurable.AirFlowDividedByVolume, 166}, // ParameterType.HVACAirflowDividedByVolume
      { SpecType.Measurable.AirFlowDividedByCoolingLoad, 167}, // ParameterType.HVACAirflowDividedByCoolingLoad
      { SpecType.Measurable.AreaDividedByCoolingLoad, 168}, // ParameterType.HVACAreaDividedByCoolingLoad
      { SpecType.Measurable.WireDiameter, 169}, // ParameterType.WireSize
      { SpecType.Measurable.HvacSlope, 170}, // ParameterType.HVACSlope
      { SpecType.Measurable.PipingSlope, 171}, // ParameterType.PipingSlope
      { SpecType.Measurable.Currency, 172}, // ParameterType.Currency
      { SpecType.Measurable.Efficacy, 173}, // ParameterType.ElectricalEfficacy
      { SpecType.Measurable.Wattage, 174}, // ParameterType.ElectricalWattage
      { SpecType.Measurable.ColorTemperature, 175}, // ParameterType.ColorTemperature
      { SpecType.Measurable.LuminousIntensity, 177}, // ParameterType.ElectricalLuminousIntensity
      { SpecType.Measurable.Luminance, 178}, // ParameterType.ElectricalLuminance
      { SpecType.Measurable.AreaDividedByHeatingLoad, 179}, // ParameterType.HVACAreaDividedByHeatingLoad
      { SpecType.Measurable.Factor, 180}, // ParameterType.HVACFactor
      { SpecType.Measurable.ElectricalTemperature, 181}, // ParameterType.ElectricalTemperature
      { SpecType.Measurable.CableTraySize, 182}, // ParameterType.ElectricalCableTraySize
      { SpecType.Measurable.ConduitSize, 183}, // ParameterType.ElectricalConduitSize
      { SpecType.Measurable.ReinforcementVolume, 184}, // ParameterType.ReinforcementVolume
      { SpecType.Measurable.ReinforcementLength, 185}, // ParameterType.ReinforcementLength
      { SpecType.Measurable.DemandFactor, 186}, // ParameterType.ElectricalDemandFactor
      { SpecType.Measurable.DuctInsulationThickness, 187}, // ParameterType.HVACDuctInsulationThickness
      { SpecType.Measurable.DuctLiningThickness, 188}, // ParameterType.HVACDuctLiningThickness
      { SpecType.Measurable.PipeInsulationThickness, 189}, // ParameterType.PipeInsulationThickness
      { SpecType.Measurable.ThermalResistance, 190}, // ParameterType.HVACThermalResistance
      { SpecType.Measurable.ThermalMass, 191}, // ParameterType.HVACThermalMass
      { SpecType.Measurable.Acceleration, 192}, // ParameterType.Acceleration
      { SpecType.Measurable.BarDiameter, 193}, // ParameterType.BarDiameter
      { SpecType.Measurable.CrackWidth, 194}, // ParameterType.CrackWidth
      { SpecType.Measurable.Displacement, 195}, // ParameterType.DisplacementDeflection
      { SpecType.Measurable.Energy, 196}, // ParameterType.Energy
      { SpecType.Measurable.StructuralFrequency, 197}, // ParameterType.StructuralFrequency
      { SpecType.Measurable.Mass, 198}, // ParameterType.Mass
      { SpecType.Measurable.MassPerUnitLength, 199}, // ParameterType.MassPerUnitLength
      { SpecType.Measurable.MomentOfInertia, 200}, // ParameterType.MomentOfInertia
      { SpecType.Measurable.SurfaceAreaPerUnitLength, 201}, // ParameterType.SurfaceArea
      { SpecType.Measurable.Period, 202}, // ParameterType.Period
      { SpecType.Measurable.Pulsation, 203}, // ParameterType.Pulsation
      { SpecType.Measurable.ReinforcementArea, 204}, // ParameterType.ReinforcementArea
      { SpecType.Measurable.ReinforcementAreaPerUnitLength, 205}, // ParameterType.ReinforcementAreaPerUnitLength
      { SpecType.Measurable.ReinforcementCover, 206}, // ParameterType.ReinforcementCover
      { SpecType.Measurable.ReinforcementSpacing, 207}, // ParameterType.ReinforcementSpacing
      { SpecType.Measurable.Rotation, 208}, // ParameterType.Rotation
      { SpecType.Measurable.SectionArea, 209}, // ParameterType.SectionArea
      { SpecType.Measurable.SectionDimension, 210}, // ParameterType.SectionDimension
      { SpecType.Measurable.SectionModulus, 211}, // ParameterType.SectionModulus
      { SpecType.Measurable.SectionProperty, 212}, // ParameterType.SectionProperty
      { SpecType.Measurable.StructuralVelocity, 213}, // ParameterType.StructuralVelocity
      { SpecType.Measurable.WarpingConstant, 214}, // ParameterType.WarpingConstant
      { SpecType.Measurable.Weight, 215}, // ParameterType.Weight
      { SpecType.Measurable.WeightPerUnitLength, 216}, // ParameterType.WeightPerUnitLength
      { SpecType.Measurable.ThermalConductivity, 217}, // ParameterType.HVACThermalConductivity
      { SpecType.Measurable.SpecificHeat, 218}, // ParameterType.HVACSpecificHeat
      { SpecType.Measurable.SpecificHeatOfVaporization, 219}, // ParameterType.HVACSpecificHeatOfVaporization
      { SpecType.Measurable.Permeability, 220}, // ParameterType.HVACPermeability
      { SpecType.Measurable.ElectricalResistivity, 221}, // ParameterType.ElectricalResistivity
      { SpecType.Measurable.MassDensity, 222}, // ParameterType.MassDensity
      { SpecType.Measurable.MassPerUnitArea, 223}, // ParameterType.MassPerUnitArea
      { SpecType.Measurable.PipeDimension, 224}, // ParameterType.PipeDimension
      { SpecType.Measurable.PipingMass, 225}, // ParameterType.PipeMass
      { SpecType.Measurable.PipeMassPerUnitLength, 226}, // ParameterType.PipeMassPerUnitLength
      { SpecType.Measurable.HvacTemperatureDifference, 227}, // ParameterType.HVACTemperatureDifference
      { SpecType.Measurable.PipingTemperatureDifference, 228}, // ParameterType.PipingTemperatureDifference
      { SpecType.Measurable.ElectricalTemperatureDifference, 229}, // ParameterType.ElectricalTemperatureDifference
      { SpecType.Measurable.Time, 230}, // ParameterType.TimeInterval
      { SpecType.Measurable.Speed, 231}, // ParameterType.Speed
      { SpecType.Measurable.Stationing, 232}, // ParameterType.Stationing
    };
#pragma warning restore CS0618 // Type or member is obsolete
#endif
  }
}

namespace RhinoInside.Revit.External.DB.Extensions
{
  static class DataTypeExtension
  {
#if !REVIT_2022
    public static Schemas.DataType GetDataType(this Autodesk.Revit.DB.Definition value)
    {
      if (value.ParameterType == Autodesk.Revit.DB.ParameterType.Invalid)
        return Schemas.DataType.Empty;

      Schemas.DataType dataType = value.ParameterType;

      return dataType != Schemas.DataType.Empty ? dataType :
#if REVIT_2021
      (Schemas.DataType) value.GetSpecTypeId();
#else
      (Schemas.SpecType) value.UnitType;
#endif
    }
#endif

#if !REVIT_2021
    internal static Schemas.DataType GetDataType(this Autodesk.Revit.DB.ExternalDefinitionCreationOptions value) => value.Type;
    internal static void SetDataType(this Autodesk.Revit.DB.ExternalDefinitionCreationOptions value, Schemas.DataType dataType) => value.Type = dataType;
#endif
  }
}
