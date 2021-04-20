using System;
using System.Collections.Generic;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.BuiltInParameterGroup
  /// </summary>
  public partial class ParameterGroup : DataType
  {
    static readonly ParameterGroup empty = new ParameterGroup();
    public static new ParameterGroup Empty => empty;

    public ParameterGroup() { }
    public ParameterGroup(string id) : base(id)
    {
      if (!id.StartsWith("autodesk.parameter.group") && !id.StartsWith("autodesk.revit.group"))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

#if REVIT_2021
    public static implicit operator ParameterGroup(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new ParameterGroup(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(ParameterGroup value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

#if !REVIT_2022
    public static implicit operator ParameterGroup(Autodesk.Revit.DB.BuiltInParameterGroup value)
    {
      foreach (var item in map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Empty;
    }

    public static implicit operator Autodesk.Revit.DB.BuiltInParameterGroup(ParameterGroup value)
    {
      if (map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.BuiltInParameterGroup) ut;

      return Autodesk.Revit.DB.BuiltInParameterGroup.INVALID;
    }
#endif
  }
}

namespace RhinoInside.Revit.External.DB.Extensions
{
  static class ParameterGroupExtension
  {
    public static Schemas.ParameterGroup GetGroupType(this Autodesk.Revit.DB.Definition value)
    {
#if REVIT_2022
      // Revit 2022 has Definition.GetGroupTypeId defined,
      // but it throws an exception when ParameterGroup is BuiltInParameterGroup.INVALID
      // By now to improve speed we use our implementation even on Revit 2022
      return value.ParameterGroup == Autodesk.Revit.DB.BuiltInParameterGroup.INVALID ?
        Schemas.ParameterGroup.Empty :
        (Schemas.ParameterGroup) value.GetGroupTypeId();
#else
      return value.ParameterGroup;
#endif
    }
  }
}
