using System;

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
