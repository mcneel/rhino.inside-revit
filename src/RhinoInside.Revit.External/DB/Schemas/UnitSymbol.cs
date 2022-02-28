using System;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.UnitSymbolType
  /// </summary>
  public partial class UnitSymbol : DataType
  {
    static readonly UnitSymbol empty = new UnitSymbol();
    public static new UnitSymbol Empty => empty;
    public static UnitSymbol Custom => new UnitSymbol("autodesk.unit.symbol:custom-1.0.0");

    public UnitSymbol() { }
    public UnitSymbol(string id) : base(id)
    {
      if (!IsUnitSymbol(id))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    public static bool IsUnitSymbol(string id)
    {
      return id.StartsWith("autodesk.unit.symbol");
    }

    public static bool IsUnitSymbol(DataType value, out UnitSymbol unitSymbol)
    {
      var typeId = value.TypeId;
      if (IsUnitSymbol(typeId))
      {
        unitSymbol = new UnitSymbol(typeId);
        return true;
      }

      unitSymbol = default;
      return false;
    }

#if REVIT_2021
    public static implicit operator UnitSymbol(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new UnitSymbol(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(UnitSymbol value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
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
