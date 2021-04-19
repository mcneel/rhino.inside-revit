using System;
using System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.BuiltInParameter
  /// </summary>
  public partial class ParameterId : DataType
  {
    static readonly ParameterId empty = new ParameterId();
    public static new ParameterId Empty => empty;

    public ParameterId() { }
    public ParameterId(string id) : base(id)
    {
      if (!id.StartsWith("autodesk.revit.parameter"))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

#if REVIT_2021
    public static implicit operator ParameterId(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new ParameterId(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(ParameterId value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

    public static implicit operator ParameterId(Autodesk.Revit.DB.BuiltInParameter value)
    {
      foreach (var item in map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.BuiltInParameter(ParameterId value)
    {
      if (map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.BuiltInParameter) ut;

      return Autodesk.Revit.DB.BuiltInParameter.INVALID;
    }

    public static implicit operator ParameterId(Autodesk.Revit.DB.ElementId value)
    {
      if (value is null) return default;
      if (value == Autodesk.Revit.DB.ElementId.InvalidElementId) return Empty;
      if (value.TryGetBuiltInParameter(out var builtInParameter)) return builtInParameter;

      throw new InvalidCastException();
    }

    public static implicit operator Autodesk.Revit.DB.ElementId(ParameterId value)
    {
      if (value is null) return default;
      if (value == Empty) return Autodesk.Revit.DB.ElementId.InvalidElementId;
      return value;
    }
  }
}
