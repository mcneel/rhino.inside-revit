using System;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.DisplayUnitType
  /// </summary>
  public partial class UnitType : DataType
  {
    public static new UnitType Empty { get; } = new UnitType();
    public static UnitType Custom { get; } = new UnitType("autodesk.unit.unit:custom-1.0.0");

    public override string LocalizedLabel => IsNullOrEmpty(this) ? string.Empty :
#if REVIT_2021
      Autodesk.Revit.DB.LabelUtils.GetLabelForUnit(this);
#else
      Autodesk.Revit.DB.LabelUtils.GetLabelFor((Autodesk.Revit.DB.DisplayUnitType) this);
#endif

    public UnitType() { }
    public UnitType(string id) : base(id)
    {
      if (!IsUnitType(id, empty: true))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    #region IParsable
    public static bool TryParse(string s, IFormatProvider provider, out UnitType result)
    {
      if (IsUnitType(s, empty: true))
      {
        result = new UnitType(s);
        return true;
      }

      result = default;
      return false;
    }

    public static UnitType Parse(string s, IFormatProvider provider)
    {
      if (!TryParse(s, provider, out var result)) throw new FormatException($"{nameof(s)} is not in the correct format.");
      return result;
    }

    static bool IsUnitType(string id, bool empty)
    {
      return (empty && id == string.Empty) || // 'Other'
             id.StartsWith("autodesk.unit.unit");
    }
    #endregion

    public static bool IsUnitType(DataType value, out UnitType unitType)
    {
      switch (value)
      {
        case UnitType ut: unitType = ut; return true;
        default:

          var typeId = value.TypeId;
          if (IsUnitType(typeId, empty: false))
          {
            unitType = new UnitType(typeId);
            return true;
          }

          unitType = default;
          return false;
      }
    }

#if REVIT_2021
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(UnitType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
    public static implicit operator UnitType(Autodesk.Revit.DB.ForgeTypeId value)
    {
      if (value is null) return null;
      var typeId = value.TypeId;
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
      return IsUnitType(typeId, empty: true) ?
        new UnitType(typeId) :
        throw new InvalidCastException($"'{typeId}' is not a valid {typeof(UnitType)}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
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
