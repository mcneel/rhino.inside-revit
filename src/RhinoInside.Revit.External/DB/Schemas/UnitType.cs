using System;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.DisplayUnitType
  /// </summary>
  public partial class UnitType : DataType
  {
    static readonly UnitType empty = new UnitType();
    public static new UnitType Empty => empty;
    public static UnitType Custom => new UnitType("autodesk.unit.unit:custom-1.0.0");

    public UnitType() { }
    public UnitType(string id) : base(id)
    {
      if (!IsUnitType(id))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    public static bool IsUnitType(string id)
    {
      return id.StartsWith("autodesk.unit.unit");
    }

    public static bool IsUnitType(DataType value, out UnitType unitType)
    {
      var typeId = value.TypeId;
      if (IsUnitType(typeId))
      {
        unitType = new UnitType(typeId);
        return true;
      }

      unitType = default;
      return false;
    }

#if REVIT_2021
    public static implicit operator UnitType(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new UnitType(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(UnitType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

#if !REVIT_2022
#pragma warning disable CS0618 // Type or member is obsolete
    public static implicit operator UnitType(Autodesk.Revit.DB.DisplayUnitType value)
    {
      if (value == Autodesk.Revit.DB.DisplayUnitType.DUT_CUSTOM)
        return Custom;

      foreach (var item in map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.DisplayUnitType(UnitType value)
    {
      if (map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.DisplayUnitType) ut;

      return Autodesk.Revit.DB.DisplayUnitType.DUT_UNDEFINED;
    }
#pragma warning restore CS0618 // Type or member is obsolete
#endif
  }
}

#if !REVIT_2021
namespace RhinoInside.Revit.External.DB.Extensions
{
  static class UnitTypeExtension
  {
    internal static Schemas.UnitType GetUnitTypeId(this Autodesk.Revit.DB.Parameter value) => value.DisplayUnitType;
    internal static Schemas.UnitType GetUnitTypeId(this Autodesk.Revit.DB.FamilyParameter value) => value.DisplayUnitType;
    internal static Schemas.UnitType GetUnitTypeId(this Autodesk.Revit.DB.FormatOptions value) => value.DisplayUnits;
    internal static Schemas.UnitType GetUnitTypeId(this Autodesk.Revit.DB.FamilySizeTableColumn value) => value.DisplayUnitType;
#if REVIT_2018
    internal static Schemas.UnitType GetUnitTypeId(this Autodesk.Revit.DB.Visual.AssetPropertyDistance value) => value.DisplayUnitType;
#else
    internal static Schemas.UnitType GetUnitTypeId(this Autodesk.Revit.Utility.AssetPropertyDistance value) => value.DisplayUnitType;
#endif
  }
}
#endif
