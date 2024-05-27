using System;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.UnitSymbolType
  /// </summary>
  public partial class UnitSymbol : DataType
  {
    public static new UnitSymbol Empty { get; } = new UnitSymbol();
    public static UnitSymbol Custom { get; } = new UnitSymbol("autodesk.unit.symbol:custom-1.0.0");

    public override string LocalizedLabel => IsNullOrEmpty(this) ? string.Empty :
#if REVIT_2021
      Autodesk.Revit.DB.LabelUtils.GetLabelForSymbol(this);
#else
      Autodesk.Revit.DB.LabelUtils.GetLabelFor((Autodesk.Revit.DB.UnitSymbolType) this);
#endif

    public UnitSymbol() { }
    public UnitSymbol(string id) : base(id)
    {
      if (!IsUnitSymbol(id, empty: true))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    #region IParsable
    public static bool TryParse(string s, IFormatProvider provider, out UnitSymbol result)
    {
      if (IsUnitSymbol(s, empty: true))
      {
        result = new UnitSymbol(s);
        return true;
      }

      result = default;
      return false;
    }

    public static UnitSymbol Parse(string s, IFormatProvider provider)
    {
      if (!TryParse(s, provider, out var result)) throw new FormatException($"{nameof(s)} is not in the correct format.");
      return result;
    }

    static bool IsUnitSymbol(string id, bool empty)
    {
      return (empty && id == string.Empty) || // 'Other'
              id.StartsWith("autodesk.unit.symbol");
    }
    #endregion

    public static bool IsUnitSymbol(DataType value, out UnitSymbol unitSymbol)
    {
      switch (value)
      {
        case UnitSymbol us: unitSymbol = us; return true;
        default:

          var typeId = value.TypeId;
          if (IsUnitSymbol(typeId, empty: false))
          {
            unitSymbol = new UnitSymbol(typeId);
            return true;
          }

          unitSymbol = default;
          return false;
      }
    }

#if REVIT_2021
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(UnitSymbol value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
    public static implicit operator UnitSymbol(Autodesk.Revit.DB.ForgeTypeId value)
    {
      if (value is null) return null;
      var typeId = value.TypeId;
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
      return IsUnitSymbol(typeId, empty: true) ?
        new UnitSymbol(typeId) :
        throw new InvalidCastException($"'{typeId}' is not a valid {typeof(UnitSymbol)}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
#endif

#if !REVIT_2022
#pragma warning disable CS0618 // Type or member is obsolete
    public static implicit operator UnitSymbol(Autodesk.Revit.DB.UnitSymbolType value)
    {
      if ((int) value == -1 /*Autodesk.Revit.DB.UnitSymbolType.UST_CUSTOM*/)
        return Custom;

      foreach (var item in map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.UnitSymbolType(UnitSymbol value)
    {
      if (map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.UnitSymbolType) ut;

      return Autodesk.Revit.DB.UnitSymbolType.UST_NONE;
    }

#pragma warning restore CS0618 // Type or member is obsolete
#endif
  }
}

#if !REVIT_2021
namespace RhinoInside.Revit.External.DB.Extensions
{
  static class UnitSymbolExtension
  {
    internal static Schemas.UnitSymbol GetSymbolTypeId(this Autodesk.Revit.DB.FormatOptions value) => value.UnitSymbol;
  }
}
#endif
