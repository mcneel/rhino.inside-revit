using System;
using System.Collections.Generic;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.UnitType
  /// </summary>
  public partial class SpecType : DataType
  {
    static readonly SpecType empty = new SpecType();
    public static new SpecType Empty => empty;
    public static SpecType Custom => new SpecType("autodesk.spec:custom-1.0.0");

    public SpecType() { }
    public SpecType(string id) : base(id)
    {
      if (!id.StartsWith("autodesk.spec"))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    public static bool IsSpecType(DataType value, out SpecType specType)
    {
      var typeId = value.TypeId;
      if (typeId.StartsWith("autodesk.spec"))
      {
        specType = new SpecType(typeId);
        return true;
      }

      specType = default;
      return false;
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

    #region Dimensionality
    internal static readonly IReadOnlyDictionary<SpecType, int> LengthDimensionality = new Dictionary<SpecType, int>
    {
      #region Length
      { Measurable.Length, +1 },
      { Measurable.RotationalLineSpringCoefficient, +1 },
      { Measurable.ReinforcementLength, +1 },

      { Measurable.PointSpringCoefficient, -1 },
      { Measurable.MassPerUnitLength, -1 },
      { Measurable.WeightPerUnitLength, -1 },
      { Measurable.PipeMassPerUnitLength, -1 },
      #endregion

      #region Area
      { Measurable.Area, +2 },
      { Measurable.AreaForce, +2 },
      { Measurable.AreaDividedByCoolingLoad, +2 },
      { Measurable.AreaDividedByHeatingLoad, +2 },
      { Measurable.SurfaceAreaPerUnitLength, +2 -1 },
      { Measurable.ReinforcementAreaPerUnitLength, +2 -1 },
      { Measurable.ReinforcementArea, +2 },
      { Measurable.SectionArea, +2 },
      { Measurable.RotationalPointSpringCoefficient, +2 },

      { Measurable.LineSpringCoefficient, -2 },
      { Measurable.CoolingLoadDividedByArea, -2 },
      { Measurable.HeatingLoadDividedByArea, -2 },
      { Measurable.MassPerUnitArea, -2 },
      #endregion

      #region Volume
      { Measurable.Volume, +3 },
      { Measurable.PipingVolume, +3 },
      { Measurable.ReinforcementVolume, +3 },

      { Measurable.AreaSpringCoefficient, -3 },
      { Measurable.CoolingLoadDividedByVolume, -3 },
      { Measurable.HeatingLoadDividedByVolume, -3 },
      { Measurable.AirFlowDividedByVolume, -3 },
      #endregion
    };

    internal bool TryGetLengthDimensionality(out int dimensionality) =>
      LengthDimensionality.TryGetValue(this, out dimensionality);
    #endregion

#if REVIT_2021
    public static implicit operator SpecType(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new SpecType(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(SpecType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
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
