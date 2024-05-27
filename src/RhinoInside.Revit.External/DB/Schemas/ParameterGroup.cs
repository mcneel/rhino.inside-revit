using System;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.BuiltInParameterGroup
  /// </summary>
  public partial class ParameterGroup : DataType
  {
    public static new ParameterGroup Empty { get; } = new ParameterGroup();

    public override string LocalizedLabel => IsNullOrEmpty(this) ? "Other" :
#if REVIT_2022
      Autodesk.Revit.DB.LabelUtils.GetLabelForGroup(this);
#else
      Autodesk.Revit.DB.LabelUtils.GetLabelFor((Autodesk.Revit.DB.BuiltInParameterGroup) this);
#endif

    public ParameterGroup() { }
    public ParameterGroup(string id) : base(id)
    {
      if (!IsParameterGroup(id, empty: true))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    #region IParsable
    public static bool TryParse(string s, IFormatProvider provider, out ParameterGroup result)
    {
      if (IsParameterGroup(s, empty: true))
      {
        result = new ParameterGroup(s);
        return true;
      }

      result = default;
      return false;
    }

    public static ParameterGroup Parse(string s, IFormatProvider provider)
    {
      if (!TryParse(s, provider, out var result)) throw new FormatException($"{nameof(s)} is not in the correct format.");
      return result;
    }

    static bool IsParameterGroup(string id, bool empty)
    {
      return (empty && id == string.Empty) || // 'Other'
             id.StartsWith("autodesk.parameter.group") ||
             id.StartsWith("autodesk.revit.group");
    }
    #endregion

    public static bool IsParameterGroup(DataType value, out ParameterGroup parameterGroup)
    {
      switch (value)
      {
        case ParameterGroup pg: parameterGroup = pg; return true;
        default:

          var typeId = value.TypeId;
          if (IsParameterGroup(typeId, empty: false))
          {
            parameterGroup = new ParameterGroup(typeId);
            return true;
          }

          parameterGroup = default;
          return false;
      }
    }

#if REVIT_2021
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(ParameterGroup value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
    public static implicit operator ParameterGroup(Autodesk.Revit.DB.ForgeTypeId value)
    {
      if (value is null) return null;
      var typeId = value.TypeId;
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
      return IsParameterGroup(typeId, empty: true) ?
        new ParameterGroup(typeId) :
        throw new InvalidCastException($"'{typeId}' is not a valid {typeof(ParameterGroup)}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
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
#if REVIT_2024
      return self.GetGroupTypeId();
#elif REVIT_2022
      // Revit 2022 has Definition.GetGroupTypeId defined,
      // but it throws an exception when ParameterGroup is BuiltInParameterGroup.INVALID
      // By now to improve speed we use our implementation even on Revit 2022
      return self.ParameterGroup == Autodesk.Revit.DB.BuiltInParameterGroup.INVALID ?
        Schemas.ParameterGroup.Empty : (Schemas.ParameterGroup) self.GetGroupTypeId();
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

#if !REVIT_2024
    internal static Schemas.ParameterGroup ToParameterGroup(this Autodesk.Revit.DB.BuiltInParameterGroup value)
    {
      foreach (var item in Schemas.ParameterGroup.map)
      {
        if (item.Value == (int) value)
          return item.Key;
      }

      return Schemas.ParameterGroup.Empty;
    }

    internal static Autodesk.Revit.DB.BuiltInParameterGroup ToBuiltInParameterGroup(this Schemas.ParameterGroup value)
    {
      if (value is object && Schemas.ParameterGroup.map.TryGetValue(value, out var ut))
        return (Autodesk.Revit.DB.BuiltInParameterGroup) ut;

      return Autodesk.Revit.DB.BuiltInParameterGroup.INVALID;
    }
#endif
  }
}
