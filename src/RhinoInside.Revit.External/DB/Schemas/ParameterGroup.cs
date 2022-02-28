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

    public string LocalizedLabel =>
#if REVIT_2022
      Autodesk.Revit.DB.LabelUtils.GetLabelForGroup(this);
#else
      Autodesk.Revit.DB.LabelUtils.GetLabelFor((Autodesk.Revit.DB.BuiltInParameterGroup) this);
#endif

    public ParameterGroup() { }
    public ParameterGroup(string id) : base(id)
    {
      if (!IsParameterGroup(id))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    public static bool IsParameterGroup(string id)
    {
      return id.StartsWith("autodesk.parameter.group") || id.StartsWith("autodesk.revit.group");
    }

#if REVIT_2021
    public static implicit operator ParameterGroup(Autodesk.Revit.DB.ForgeTypeId value) => value is null ? null : new ParameterGroup(value.TypeId);
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(ParameterGroup value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
#endif

#if !REVIT_2022
    public static implicit operator ParameterGroup(Autodesk.Revit.DB.BuiltInParameterGroup value) =>
      Extensions.ParameterGroupExtension.ToParameterGroup(value);

    public static implicit operator Autodesk.Revit.DB.BuiltInParameterGroup(ParameterGroup value) =>
      Extensions.ParameterGroupExtension.ToBuiltInParameterGroup(value);
#endif
  }
}

namespace RhinoInside.Revit.External.DB.Extensions
{
  static class ParameterGroupExtension
  {
    public static Schemas.ParameterGroup GetGroupType(this Autodesk.Revit.DB.Definition self)
    {
#if REVIT_2022
      // Revit 2022 has Definition.GetGroupTypeId defined,
      // but it throws an exception when ParameterGroup is BuiltInParameterGroup.INVALID
      // By now to improve speed we use our implementation even on Revit 2022
      return self.ParameterGroup == Autodesk.Revit.DB.BuiltInParameterGroup.INVALID ?
        default : (Schemas.ParameterGroup) self.GetGroupTypeId();
#else
      return self.ParameterGroup;
#endif
    }

    public static void SetGroupType(this Autodesk.Revit.DB.InternalDefinition self, Schemas.ParameterGroup group)
    {
#if REVIT_2022
      self.SetGroupTypeId(group);
#else
      self.set_ParameterGroup(group);
#endif
    }

    internal static Schemas.ParameterGroup ToParameterGroup(this Autodesk.Revit.DB.BuiltInParameterGroup value)
    {
      foreach (var item in Schemas.ParameterGroup.map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return default;
    }

    internal static Autodesk.Revit.DB.BuiltInParameterGroup ToBuiltInParameterGroup(this Schemas.ParameterGroup value)
    {
      if (value is object && Schemas.ParameterGroup.map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.BuiltInParameterGroup) ut;

      return Autodesk.Revit.DB.BuiltInParameterGroup.INVALID;
    }
  }
}
