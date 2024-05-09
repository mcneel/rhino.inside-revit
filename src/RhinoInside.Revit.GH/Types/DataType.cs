using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Special;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;
  using Kernel.Attributes;

  public abstract class DataType<T> : IGH_Goo,
    IEquatable<DataType<T>>,
    IComparable<DataType<T>>,
    IComparable,
    IGH_QuickCast,
    IGH_ItemDescription
    where T : EDBS.DataType
  {
    protected DataType() { }
    protected DataType(T value) => Value = value;

    public virtual string Text
    {
      get
      {
        if (!IsValid) return "<invalid>";
        if (IsEmpty) return "<empty>";
#if REVIT_2022
        return Value.LocalizedLabel;
#else
        return Value.Label;
#endif
      }
    }

    public T Value { get; set; }

    public bool IsEmpty => Value == EDBS.DataType.Empty;

    #region IGH_Goo
    public virtual bool IsValid => Value != default;
    public virtual string IsValidWhyNot => IsValid ? default : "Not Valid";

    public virtual string TypeName
    {
      get
      {
        var name = GetType().GetTypeInfo().GetCustomAttribute<NameAttribute>();
        return $"Revit {name?.Name ?? typeof(T).Name}";
      }
    }

    public virtual string TypeDescription => TypeName;

    public IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    string IGH_Goo.ToString() => Text;

    public virtual IGH_GooProxy EmitProxy() => default;

    public virtual object ScriptVariable() => Value.FullName;

    public virtual bool CastFrom(object source) => false;
    public virtual bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_String)))
      {
        target = (Q) (object) new GH_String(Value.TypeId);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(T)))
      {
        target = (Q) (object) Value;
        return true;
      }

      target = default;
      return false;
    }

    public virtual bool Write(GH_IWriter writer)
    {
      if(Value?.TypeId is string typeId)
        writer.SetString("string", typeId);

      return true;
    }

    public virtual bool Read(GH_IReader reader)
    {
      string typeId = default;
      if (reader.TryGetString("string", ref typeId))
        Value = (T) Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new object[] { typeId }, null, null);
      else
        Value = default;

      return true;
    }
    #endregion

    #region IGH_ItemDescription
    Bitmap IGH_ItemDescription.GetTypeIcon(Size size)
    {
      if (typeof(EDBS.DataType).IsAssignableFrom(typeof(T)))
        return Properties.Resources.BuiltInParameterTypes;

      if (typeof(EDBS.ParameterGroup).IsAssignableFrom(typeof(T)))
        return Properties.Resources.BuiltInParameterGroups;

      if (typeof(EDBS.ParameterId).IsAssignableFrom(typeof(T)))
        return Properties.Resources.BuiltInParameters;

      if (typeof(EDBS.CategoryId).IsAssignableFrom(typeof(T)))
        return Properties.Resources.BuiltInCategories;

      return Properties.Resources.UnknownIcon;
    }

    internal virtual string Label => Value.Label;
    internal virtual string LocalizedLabel => Value.LocalizedLabel;

#if REVIT_2022
    string IGH_ItemDescription.Name => LocalizedLabel;
#else
    string IGH_ItemDescription.Name => Label;
#endif
    string IGH_ItemDescription.Identity => Value?.Name;
    string IGH_ItemDescription.Description => Value?.Namespace;
    #endregion

    #region System.Object
    public sealed override string ToString() => Value.FullName;
    public override int GetHashCode() => Value.GetHashCode();
    #endregion

    #region IComparable
    int IComparable.CompareTo(object obj) => ((IComparable<DataType<T>>) this).CompareTo(obj as DataType<T>);

    int IComparable<DataType<T>>.CompareTo(DataType<T> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      return string.CompareOrdinal(Value.TypeId, other.Value.TypeId);
    }
    #endregion

    #region IEquatable
    public override bool Equals(object obj) => obj is DataType<T> other && Equals(other);
    public bool Equals(DataType<T> other) => other.GetType() == GetType() && Value.Equals(other.Value);
    #endregion

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type => GH_QuickCastType.text;

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other)
    {
      switch (other.QC_Type)
      {
        case GH_QuickCastType.text:
          var dist0 = GH_StringMatcher.LevenshteinDistance(Value.FullName, other.QC_Text());
          var dist1 = GH_StringMatcher.LevenshteinDistance(Value.FullName.ToUpperInvariant(), other.QC_Text().ToUpperInvariant());
          return 0.5 * (dist0 + dist1);
        default:
          return (this as IGH_QuickCast).QC_Distance(new GH_String(other.QC_Text()));
      }
    }

    int IGH_QuickCast.QC_Hash() => Value.GetHashCode();
    bool IGH_QuickCast.QC_Bool() => IsValid && !IsEmpty;
    int IGH_QuickCast.QC_Int() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Int32)}");
    double IGH_QuickCast.QC_Num() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Double)}");
    string IGH_QuickCast.QC_Text() => Value.FullName;
    Color IGH_QuickCast.QC_Col() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Color)}");
    Point3d IGH_QuickCast.QC_Pt() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Point3d)}");
    Vector3d IGH_QuickCast.QC_Vec() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Vector3d)}");
    Complex IGH_QuickCast.QC_Complex() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Complex)}");
    Matrix IGH_QuickCast.QC_Matrix() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Matrix)}");
    Interval IGH_QuickCast.QC_Interval() => throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Interval)}");
    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      if ((this as IGH_QuickCast).QC_Type != other.QC_Type) (this as IGH_QuickCast).QC_Type.CompareTo(other.QC_Type);
      return Value.FullName.CompareTo(other.QC_Text());
    }
    #endregion
  }

  [
    ComponentGuid("38E9E729-9D9F-461F-A1D7-798CDFA2CD4C"),
    Name("Unit Type"),
    Description("Contains a collection of Revit unit type values"),
  ]
  public class UnitType : DataType<EDBS.UnitType>, IGH_Enumerate
  {
    public UnitType() { }
    public UnitType(EDBS.UnitType value) : base(value) { }

    public static IEnumerable<UnitType> EnumValues => typeof(EDBS.UnitType).
      GetProperties(BindingFlags.Public | BindingFlags.Static).
      Where(x => x.PropertyType == typeof(EDBS.UnitType)).
      Select(x => new UnitType((EDBS.UnitType) x.GetValue(null))).
      OrderBy(x => x.Text);

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      try
      {
        switch (source)
        {
#if REVIT_2021
          case ARDB.ForgeTypeId f:
            if (!EDBS.UnitType.IsUnitType(f, out var fid)) return false;
            Value = fid; return true;
#else
          case int i: Value = (ARDB.DisplayUnitType) i; return true;
          case ARDB.DisplayUnitType u: Value = u; return true;
#endif
          case EDBS.DataType s:
            if (!EDBS.UnitType.IsUnitType(s, out var id)) return false;
            Value = id; return true;

          case string t:
            if (!EDBS.UnitType.TryParse(t, null, out var tid)) return false;
            Value = tid; return true;
        }
      }
      catch (ArgumentException) { return false; }

      return base.CastFrom(source);
    }
  }

  [
    ComponentGuid("A5EA05A9-C17E-48F4-AC4C-34F169AE4F9A"),
    Name("Parameter Type"),
    Description("Contains a collection of Revit parameter type values"),
  ]
  public class ParameterType : DataType<EDBS.DataType>, IGH_Enumerate
  {
    public ParameterType() { }
    public ParameterType(EDBS.DataType value) : base(value) { }

    public override string Text
    {
      get
      {
        if (!IsValid) return "<invalid>";
        if (IsEmpty) return "<empty>";

        if (EDBS.CategoryId.IsCategoryId(Value, out var _))
          return $"Family Type : {Value.Label}";

        return Value.Label;
      }
    }


    public static IEnumerable<ParameterType> EnumValues
    {
      get
      {
        var types = new Type[]
        {
          typeof(EDBS.SpecType.Boolean),
          typeof(EDBS.SpecType.Int),
          typeof(EDBS.SpecType.String),
          typeof(EDBS.SpecType.Measurable),
          typeof(EDBS.SpecType.Reference)
        };

        return types.SelectMany
        (
          type => type.
          GetProperties(BindingFlags.Public | BindingFlags.Static).
          Where(x => x.PropertyType == typeof(EDBS.SpecType)).
          Select(x => new ParameterType((EDBS.DataType) x.GetValue(null))).
          OrderBy(x => x.Value.FullName)
        );
      }
    }

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      try
      {
        switch (source)
        {
#if REVIT_2021
          case ARDB.ForgeTypeId f:    Value = f; return true;
#else
          case ARDB.ParameterType u:  Value = (EDBS.SpecType) u; return true;
          case ARDB.UnitType t:       Value = (EDBS.SpecType) t; return true;
#endif
          case EDBS.DataType s:
            if (EDBS.DataType.IsNullOrEmpty(s))               { Value = EDBS.DataType.Empty; return true; }
            if (EDBS.SpecType.IsSpecType(s, out var sid))     { Value = sid; return true; }
            if (EDBS.CategoryId.IsCategoryId(s, out var cid)) { Value = cid; return true; }
            return false;

          case string t:
            if (string.IsNullOrEmpty(t))                      { Value = EDBS.DataType.Empty; return true; }
            if (EDBS.SpecType.TryParse(t, null, out var vs))  { Value = vs; return true; }
            if (EDBS.CategoryId.TryParse(t, null, out var vc)){ Value = vc; return true; }
            return false;
        }
      }
      catch { return false; }

      return base.CastFrom(source);
    }

    public static Type PickerObjectType => typeof(Parameters.Input.BuiltInParameterTypes);
  }

  [
    ComponentGuid("3D9979B4-65C8-447F-BCEA-3705249DF3B6"),
    Name("Built-In Parameter Group"),
    Description("Contains a collection of Revit built-in parameter group values"),
  ]
  public class ParameterGroup : DataType<EDBS.ParameterGroup>, IGH_Enumerate
  {
    public ParameterGroup() { }
    public ParameterGroup(EDBS.ParameterGroup value) : base(value) { }

    public static IEnumerable<ParameterGroup> EnumValues => typeof(EDBS.ParameterGroup).
      GetProperties(BindingFlags.Public | BindingFlags.Static).
      Where(x => x.PropertyType == typeof(EDBS.ParameterGroup)).
      Select(x => new ParameterGroup((EDBS.ParameterGroup) x.GetValue(null))).
      OrderBy(x => x.Value.Label);

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      try
      {
        switch (source)
        {
#if REVIT_2021
          case ARDB.ForgeTypeId f:
            if (!EDBS.ParameterGroup.IsParameterGroup(f, out var fpg)) return false;
            Value = fpg; return true;
#else
          case int i: Value = (ARDB.BuiltInParameterGroup) i; return true;
          case ARDB.BuiltInParameterGroup u: Value = u; return true;
#endif
          case EDBS.DataType s:
            if (!EDBS.ParameterGroup.IsParameterGroup(s, out var pg)) return false;
            Value = pg; return true;

          case string t:
            if (!EDBS.ParameterGroup.TryParse(t, null, out var tpg)) return false;
            Value = tpg; return true;
        }
      }
      catch (ArgumentException) { return false; }

      return base.CastFrom(source);
    }

    public static Type PickerObjectType => typeof(Parameters.Input.BuiltInParameterGroups);
  }

  [
    ComponentGuid("BCD9B7A7-1B9F-4563-8FF4-2C7726F2DCC0"),
    Name("Built-In Parameter"),
    Description("Contains a collection of Revit built-in parameter values"),
  ]
  public class ParameterId : DataType<EDBS.ParameterId>, IGH_Enumerate
  {
    public ParameterId() { }
    public ParameterId(EDBS.ParameterId value) : base(value) { }

    public static IEnumerable<ParameterId> EnumValues => typeof(EDBS.ParameterId).
      GetProperties(BindingFlags.Public | BindingFlags.Static).
      Where(x => x.PropertyType == typeof(EDBS.ParameterId)).
      Select(x => new ParameterId((EDBS.ParameterId) x.GetValue(null))).
      OrderBy(x => x.Text);

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      try
      {
        switch (source)
        {
#if REVIT_2021
          case ARDB.ForgeTypeId f:
            if (!EDBS.ParameterId.IsParameterId(f, out var fid)) return false;
            Value = fid; return true;
#else
          case int i: Value = (ARDB.BuiltInParameter) i; return true;
          case ARDB.BuiltInParameter u: Value = u; return true;
#endif
          case EDBS.DataType s:
            if (!EDBS.ParameterId.IsParameterId(s, out var id)) return false;
            Value = id; return true;

          case string t:
            if (!EDBS.ParameterId.TryParse(t, null, out var tid)) return false;
            Value = tid; return true;
        }
      }
      catch (ArgumentException) { return false; }

      return base.CastFrom(source);
    }

    public static Type PickerObjectType => typeof(Parameters.Input.BuiltInParameters);
  }

  [
    ComponentGuid("6FBA8F56-FDFA-4EA4-90C1-52CCB29DF32D"),
    Name("Built-In Category"),
    Description("Contains a collection of Revit built-in category values"),
  ]
  public class CategoryId : DataType<EDBS.CategoryId>, IGH_Enumerate
  {
    public CategoryId() { }
    public CategoryId(EDBS.CategoryId value) : base(value) { }

    public static IEnumerable<CategoryId> EnumValues => EDBS.CategoryId.BuiltIn.Values.Select(x => new CategoryId(x));

    public override bool CastFrom(object source)
    {
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      try
      {
        switch (source)
        {
#if REVIT_2021
          case ARDB.ForgeTypeId f:
            if (!EDBS.CategoryId.IsCategoryId(f, out var fc)) return false;
            Value = fc; return true;
#else
          case int i: Value = (ARDB.BuiltInCategory) i; return true;
          case ARDB.BuiltInCategory u: Value = u; return true;
#endif
          case EDBS.DataType s:
            if (!EDBS.CategoryId.IsCategoryId(s, out var c)) return false;
            return true;

          case string t:
            if (!EDBS.CategoryId.TryParse(t, null, out var tc)) return false;
            Value = tc; return true;
        }
      }
      catch { return false; }

      return base.CastFrom(source);
    }

    public static Type PickerObjectType => typeof(Parameters.Input.BuiltInCategories);

    #region Properties
    private ARDB.BuiltInCategory BuiltInCategory => (ARDB.BuiltInCategory) Value;

    public ARDB.ElementId Id => new ARDB.ElementId(BuiltInCategory);

    internal override string Label => BuiltInCategory.FullName(localized: false);
    internal override string LocalizedLabel => BuiltInCategory.FullName(localized: true);

    public ARDB.CategoryType CategoryType => BuiltInCategory.CategoryType();
    public bool IsTagCategory => BuiltInCategory.IsTagCategory();
    public bool IsSubCategory => BuiltInCategory.Parent() != ARDB.BuiltInCategory.INVALID;
    public bool IsVisibleInUI => BuiltInCategory.IsVisibleInUI();
    public ERDB.CategoryDiscipline CategoryDiscipline => BuiltInCategory.CategoryDiscipline();
    #endregion
  }
}
