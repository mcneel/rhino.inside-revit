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
    public static bool IsNullOrEmpty(DataType value) => string.IsNullOrEmpty(value?.id);

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

    public static bool operator == (DataType lhs, DataType rhs) =>  (ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true);
    public static bool operator != (DataType lhs, DataType rhs) => !(ReferenceEquals(lhs, rhs) || lhs?.Equals(rhs) == true);

#if REVIT_2021
    public static implicit operator DataType(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new DataType(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(DataType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

#if !REVIT_2022
#pragma warning disable CS0618 // Type or member is obsolete
    public static implicit operator DataType(Autodesk.Revit.DB.ParameterType value) =>
      Extensions.DataTypeExtension.ToDataType(value);

    public static implicit operator Autodesk.Revit.DB.ParameterType(DataType value) =>
      Extensions.DataTypeExtension.ToParameterType(value);
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
  
    public static Schemas.DataType GetDataType(this Autodesk.Revit.DB.ScheduleField field)
    {
#if REVIT_2021
      return (Schemas.DataType) field.GetSpecTypeId();
#else
      return (Schemas.SpecType) field.UnitType;
#endif
    }  

#if !REVIT_2022
    internal static Schemas.DataType GetDataType(this Autodesk.Revit.DB.ExternalDefinitionCreationOptions value) => value.Type;
    internal static void SetDataType(this Autodesk.Revit.DB.ExternalDefinitionCreationOptions value, Schemas.DataType dataType) => value.Type = dataType;
#endif

    #region ParameterType
#if !REVIT_2023
#pragma warning disable CS0618 // Type or member is obsolete
    static readonly Dictionary<Schemas.DataType, int> parameterTypeMap = new Dictionary<Schemas.DataType, int>()
    {
      { Schemas.SpecType.String.Text, 1 }, // ParameterType.Text
      { Schemas.SpecType.Int.Integer, 2 }, // ParameterType.Integer
      { Schemas.SpecType.Measurable.Number, 3 }, // ParameterType.Number
      { Schemas.SpecType.Measurable.Length, 4 }, // ParameterType.Length
      { Schemas.SpecType.Measurable.Area, 5 }, // ParameterType.Area
      { Schemas.SpecType.Measurable.Volume, 6 }, // ParameterType.Volme
      { Schemas.SpecType.Measurable.Angle, 7 }, // ParameterType.Angle
      { Schemas.SpecType.String.Url, 8}, // ParameterType.URL
      { Schemas.SpecType.Reference.Material, 9}, // ParameterType.Material
      { Schemas.SpecType.Boolean.YesNo, 10}, // ParameterType.YesNo
      { Schemas.SpecType.Measurable.Force, 11}, // ParameterType.Force
      { Schemas.SpecType.Measurable.LinearForce, 12}, // ParameterType.LinearForce
      { Schemas.SpecType.Measurable.AreaForce, 13}, // ParameterType.AreaForce
      { Schemas.SpecType.Measurable.Moment, 14}, // ParameterType.Moment
      { Schemas.SpecType.Int.NumberOfPoles, 15}, // ParameterType.NumberOfPoles
      //{ Schemas.SpecType.Measurable.FixtureUnit, 16}, // ParameterType.FixtureUnit
      //{ Reference.FamilyType, 17}, // ParameterType.FamilyType
      { Schemas.SpecType.Reference.LoadClassification, 18}, // ParameterType.LoadClassification
      { Schemas.SpecType.Reference.Image, 19}, // ParameterType.Image
      { Schemas.SpecType.String.MultilineText, 20}, // ParameterType.MultilineText
      { Schemas.SpecType.Custom, 99}, // ParameterType.Custom
      { Schemas.SpecType.Measurable.HvacDensity, 107}, // ParameterType.HVACDensity
      { Schemas.SpecType.Measurable.HvacEnergy, 108}, // ParameterType.HVACEnergy
      { Schemas.SpecType.Measurable.HvacFriction, 109}, // ParameterType.HVACFriction
      { Schemas.SpecType.Measurable.HvacPower, 110}, // ParameterType.HVACPower
      { Schemas.SpecType.Measurable.HvacPowerDensity, 111}, // ParameterType.HVACPowerDensity
      { Schemas.SpecType.Measurable.HvacPressure, 112}, // ParameterType.HVACPressure
      { Schemas.SpecType.Measurable.HvacTemperature, 113}, // ParameterType.HVACTemperature
      { Schemas.SpecType.Measurable.HvacVelocity, 114}, // ParameterType.HVACVelocity
      { Schemas.SpecType.Measurable.AirFlow, 115}, // ParameterType.HVACAirflow
      { Schemas.SpecType.Measurable.DuctSize, 116}, // ParameterType.HVACDuctSize
      { Schemas.SpecType.Measurable.CrossSection, 117}, // ParameterType.HVACCrossSection
      { Schemas.SpecType.Measurable.HeatGain, 118}, // ParameterType.HVACHeatGain
      { Schemas.SpecType.Measurable.Current, 119}, // ParameterType.ElectricalCurrent
      { Schemas.SpecType.Measurable.ElectricalPotential, 120}, // ParameterType.ElectricalPotential
      { Schemas.SpecType.Measurable.ElectricalFrequency, 121}, // ParameterType.ElectricalFrequency
      { Schemas.SpecType.Measurable.Illuminance, 122}, // ParameterType.ElectricalFrequency
      { Schemas.SpecType.Measurable.LuminousFlux, 123}, // ParameterType.ElectricalLuminousFlux
      { Schemas.SpecType.Measurable.ElectricalPower, 124}, // ParameterType.ElectricalPower
      { Schemas.SpecType.Measurable.HvacRoughness, 125}, // ParameterType.HVACRoughness
      { Schemas.SpecType.Measurable.ApparentPower, 134}, // ParameterType.ElectricalApparentPower
      { Schemas.SpecType.Measurable.ElectricalPowerDensity, 135}, // ParameterType.ElectricalPowerDensity
      { Schemas.SpecType.Measurable.PipingDensity, 136}, // ParameterType.PipingDensity
      { Schemas.SpecType.Measurable.Flow, 137}, // ParameterType.PipingFlow
      { Schemas.SpecType.Measurable.PipingFriction, 138}, // ParameterType.PipingFriction
      { Schemas.SpecType.Measurable.PipingPressure, 139}, // ParameterType.PipingPressure
      { Schemas.SpecType.Measurable.PipingTemperature, 140}, // ParameterType.PipingTemperature
      { Schemas.SpecType.Measurable.PipingVelocity, 141}, // ParameterType.PipingVelocity
      { Schemas.SpecType.Measurable.PipingViscosity, 142}, // ParameterType.PipingViscosity
      { Schemas.SpecType.Measurable.PipeSize, 143}, // ParameterType.PipeSize
      { Schemas.SpecType.Measurable.PipingRoughness, 144}, // ParameterType.PipingRoughness
      { Schemas.SpecType.Measurable.Stress, 145}, // ParameterType.Stress
      { Schemas.SpecType.Measurable.UnitWeight, 146}, // ParameterType.UnitWeight
      { Schemas.SpecType.Measurable.ThermalExpansionCoefficient, 147}, // ParameterType.ThermalExpansion
      { Schemas.SpecType.Measurable.LinearMoment, 148}, // ParameterType.LinearMoment
      { Schemas.SpecType.Measurable.PointSpringCoefficient, 150}, // ParameterType.ForcePerLength
      { Schemas.SpecType.Measurable.RotationalPointSpringCoefficient, 151}, // ParameterType.ForceLengthPerAngle
      { Schemas.SpecType.Measurable.LineSpringCoefficient, 152}, // ParameterType.LinearForcePerLength
      { Schemas.SpecType.Measurable.RotationalLineSpringCoefficient, 153}, // ParameterType.LinearForceLengthPerAngle
      { Schemas.SpecType.Measurable.AreaSpringCoefficient, 154}, // ParameterType.AreaForcePerLength
      { Schemas.SpecType.Measurable.PipingVolume, 155}, // ParameterType.PipingVolume
      { Schemas.SpecType.Measurable.HvacViscosity, 156}, // ParameterType.HVACViscosity
      { Schemas.SpecType.Measurable.HeatTransferCoefficient, 157}, // ParameterType.HVACCoefficientOfHeatTransfer
      { Schemas.SpecType.Measurable.AirFlowDensity, 158}, // ParameterType.HVACAirflowDensity
      { Schemas.SpecType.Measurable.Slope, 159}, // ParameterType.Slope
      { Schemas.SpecType.Measurable.CoolingLoad, 160}, // ParameterType.HVACCoolingLoad
      { Schemas.SpecType.Measurable.CoolingLoadDividedByArea, 161}, // ParameterType.HVACCoolingLoadDividedByArea
      { Schemas.SpecType.Measurable.CoolingLoadDividedByVolume, 162}, // ParameterType.HVACCoolingLoadDividedByVolume
      { Schemas.SpecType.Measurable.HeatingLoad, 163}, // ParameterType.HVACHeatingLoad
      { Schemas.SpecType.Measurable.HeatingLoadDividedByArea, 164}, // ParameterType.HVACHeatingLoadDividedByArea
      { Schemas.SpecType.Measurable.HeatingLoadDividedByVolume, 165}, // ParameterType.HVACHeatingLoadDividedByVolume
      { Schemas.SpecType.Measurable.AirFlowDividedByVolume, 166}, // ParameterType.HVACAirflowDividedByVolume
      { Schemas.SpecType.Measurable.AirFlowDividedByCoolingLoad, 167}, // ParameterType.HVACAirflowDividedByCoolingLoad
      { Schemas.SpecType.Measurable.AreaDividedByCoolingLoad, 168}, // ParameterType.HVACAreaDividedByCoolingLoad
      { Schemas.SpecType.Measurable.WireDiameter, 169}, // ParameterType.WireSize
      { Schemas.SpecType.Measurable.HvacSlope, 170}, // ParameterType.HVACSlope
      { Schemas.SpecType.Measurable.PipingSlope, 171}, // ParameterType.PipingSlope
      { Schemas.SpecType.Measurable.Currency, 172}, // ParameterType.Currency
      { Schemas.SpecType.Measurable.Efficacy, 173}, // ParameterType.ElectricalEfficacy
      { Schemas.SpecType.Measurable.Wattage, 174}, // ParameterType.ElectricalWattage
      { Schemas.SpecType.Measurable.ColorTemperature, 175}, // ParameterType.ColorTemperature
      { Schemas.SpecType.Measurable.LuminousIntensity, 177}, // ParameterType.ElectricalLuminousIntensity
      { Schemas.SpecType.Measurable.Luminance, 178}, // ParameterType.ElectricalLuminance
      { Schemas.SpecType.Measurable.AreaDividedByHeatingLoad, 179}, // ParameterType.HVACAreaDividedByHeatingLoad
      { Schemas.SpecType.Measurable.Factor, 180}, // ParameterType.HVACFactor
      { Schemas.SpecType.Measurable.ElectricalTemperature, 181}, // ParameterType.ElectricalTemperature
      { Schemas.SpecType.Measurable.CableTraySize, 182}, // ParameterType.ElectricalCableTraySize
      { Schemas.SpecType.Measurable.ConduitSize, 183}, // ParameterType.ElectricalConduitSize
      { Schemas.SpecType.Measurable.ReinforcementVolume, 184}, // ParameterType.ReinforcementVolume
      { Schemas.SpecType.Measurable.ReinforcementLength, 185}, // ParameterType.ReinforcementLength
      { Schemas.SpecType.Measurable.DemandFactor, 186}, // ParameterType.ElectricalDemandFactor
      { Schemas.SpecType.Measurable.DuctInsulationThickness, 187}, // ParameterType.HVACDuctInsulationThickness
      { Schemas.SpecType.Measurable.DuctLiningThickness, 188}, // ParameterType.HVACDuctLiningThickness
      { Schemas.SpecType.Measurable.PipeInsulationThickness, 189}, // ParameterType.PipeInsulationThickness
      { Schemas.SpecType.Measurable.ThermalResistance, 190}, // ParameterType.HVACThermalResistance
      { Schemas.SpecType.Measurable.ThermalMass, 191}, // ParameterType.HVACThermalMass
      { Schemas.SpecType.Measurable.Acceleration, 192}, // ParameterType.Acceleration
      { Schemas.SpecType.Measurable.BarDiameter, 193}, // ParameterType.BarDiameter
      { Schemas.SpecType.Measurable.CrackWidth, 194}, // ParameterType.CrackWidth
      { Schemas.SpecType.Measurable.Displacement, 195}, // ParameterType.DisplacementDeflection
      { Schemas.SpecType.Measurable.Energy, 196}, // ParameterType.Energy
      { Schemas.SpecType.Measurable.StructuralFrequency, 197}, // ParameterType.StructuralFrequency
      { Schemas.SpecType.Measurable.Mass, 198}, // ParameterType.Mass
      { Schemas.SpecType.Measurable.MassPerUnitLength, 199}, // ParameterType.MassPerUnitLength
      { Schemas.SpecType.Measurable.MomentOfInertia, 200}, // ParameterType.MomentOfInertia
      { Schemas.SpecType.Measurable.SurfaceAreaPerUnitLength, 201}, // ParameterType.SurfaceArea
      { Schemas.SpecType.Measurable.Period, 202}, // ParameterType.Period
      { Schemas.SpecType.Measurable.Pulsation, 203}, // ParameterType.Pulsation
      { Schemas.SpecType.Measurable.ReinforcementArea, 204}, // ParameterType.ReinforcementArea
      { Schemas.SpecType.Measurable.ReinforcementAreaPerUnitLength, 205}, // ParameterType.ReinforcementAreaPerUnitLength
      { Schemas.SpecType.Measurable.ReinforcementCover, 206}, // ParameterType.ReinforcementCover
      { Schemas.SpecType.Measurable.ReinforcementSpacing, 207}, // ParameterType.ReinforcementSpacing
      { Schemas.SpecType.Measurable.Rotation, 208}, // ParameterType.Rotation
      { Schemas.SpecType.Measurable.SectionArea, 209}, // ParameterType.SectionArea
      { Schemas.SpecType.Measurable.SectionDimension, 210}, // ParameterType.SectionDimension
      { Schemas.SpecType.Measurable.SectionModulus, 211}, // ParameterType.SectionModulus
      { Schemas.SpecType.Measurable.SectionProperty, 212}, // ParameterType.SectionProperty
      { Schemas.SpecType.Measurable.StructuralVelocity, 213}, // ParameterType.StructuralVelocity
      { Schemas.SpecType.Measurable.WarpingConstant, 214}, // ParameterType.WarpingConstant
      { Schemas.SpecType.Measurable.Weight, 215}, // ParameterType.Weight
      { Schemas.SpecType.Measurable.WeightPerUnitLength, 216}, // ParameterType.WeightPerUnitLength
      { Schemas.SpecType.Measurable.ThermalConductivity, 217}, // ParameterType.HVACThermalConductivity
      { Schemas.SpecType.Measurable.SpecificHeat, 218}, // ParameterType.HVACSpecificHeat
      { Schemas.SpecType.Measurable.SpecificHeatOfVaporization, 219}, // ParameterType.HVACSpecificHeatOfVaporization
      { Schemas.SpecType.Measurable.Permeability, 220}, // ParameterType.HVACPermeability
      { Schemas.SpecType.Measurable.ElectricalResistivity, 221}, // ParameterType.ElectricalResistivity
      { Schemas.SpecType.Measurable.MassDensity, 222}, // ParameterType.MassDensity
      { Schemas.SpecType.Measurable.MassPerUnitArea, 223}, // ParameterType.MassPerUnitArea
      { Schemas.SpecType.Measurable.PipeDimension, 224}, // ParameterType.PipeDimension
      { Schemas.SpecType.Measurable.PipingMass, 225}, // ParameterType.PipeMass
      { Schemas.SpecType.Measurable.PipeMassPerUnitLength, 226}, // ParameterType.PipeMassPerUnitLength
      { Schemas.SpecType.Measurable.HvacTemperatureDifference, 227}, // ParameterType.HVACTemperatureDifference
      { Schemas.SpecType.Measurable.PipingTemperatureDifference, 228}, // ParameterType.PipingTemperatureDifference
      { Schemas.SpecType.Measurable.ElectricalTemperatureDifference, 229}, // ParameterType.ElectricalTemperatureDifference
      { Schemas.SpecType.Measurable.Time, 230}, // ParameterType.TimeInterval
      { Schemas.SpecType.Measurable.Speed, 231}, // ParameterType.Speed
      { Schemas.SpecType.Measurable.Stationing, 232}, // ParameterType.Stationing
    };

    internal static Schemas.DataType ToDataType(this Autodesk.Revit.DB.ParameterType value)
    {
      if (value is Autodesk.Revit.DB.ParameterType.FamilyType)
        return new Schemas.CategoryId("autodesk.revit.category.family");

      foreach (var item in parameterTypeMap)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return default;
    }

    internal static Autodesk.Revit.DB.ParameterType ToParameterType(this Schemas.DataType value)
    {
      if (value is object)
      {
        if (Schemas.CategoryId.IsCategoryId(value, out var _))
          return Autodesk.Revit.DB.ParameterType.FamilyType;

        if (parameterTypeMap.TryGetValue(value, out var pt))
          return (Autodesk.Revit.DB.ParameterType) pt;
      }

      return Autodesk.Revit.DB.ParameterType.Invalid;
    }
#pragma warning restore CS0618 // Type or member is obsolete
#endif
    #endregion

    #region StorageType
    static readonly IReadOnlyDictionary<Schemas.DataType, Autodesk.Revit.DB.StorageType> SpecToStorageType = new Dictionary<Schemas.DataType, Autodesk.Revit.DB.StorageType>
    {
      { Schemas.SpecType.Boolean.YesNo, Autodesk.Revit.DB.StorageType.Integer },
      { Schemas.SpecType.Int.Integer, Autodesk.Revit.DB.StorageType.Integer },

      { Schemas.SpecType.String.Text, Autodesk.Revit.DB.StorageType.String },
      { Schemas.SpecType.String.MultilineText, Autodesk.Revit.DB.StorageType.String },
      { Schemas.SpecType.String.Url, Autodesk.Revit.DB.StorageType.String },

      { Schemas.SpecType.Reference.Material, Autodesk.Revit.DB.StorageType.ElementId },
      { Schemas.SpecType.Reference.Image, Autodesk.Revit.DB.StorageType.ElementId },
      { Schemas.SpecType.Reference.LoadClassification, Autodesk.Revit.DB.StorageType.ElementId },
    };

    internal static Autodesk.Revit.DB.StorageType ToStorageType(this Schemas.DataType dataType)
    {
      if (dataType is object)
      {
        if (SpecToStorageType.TryGetValue(dataType, out var storage))
          return storage;

        if (Schemas.CategoryId.IsCategoryId(dataType, out var _))
          return Autodesk.Revit.DB.StorageType.ElementId;

        if (Schemas.SpecType.IsMeasurableSpec(dataType, out var _))
          return Autodesk.Revit.DB.StorageType.Double;
      }

      return Autodesk.Revit.DB.StorageType.None;
    }

#if REVIT_2021
    internal static Autodesk.Revit.DB.StorageType ToStorageType(this Autodesk.Revit.DB.ForgeTypeId dataType) =>
      ToStorageType((Schemas.DataType) dataType);
#endif
    #endregion
  }
}
