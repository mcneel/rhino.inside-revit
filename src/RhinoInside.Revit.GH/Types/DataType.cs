using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Types
{
  public abstract class DataType<T> : IGH_Goo,
    IEquatable<DataType<T>>,
    IComparable<DataType<T>>,
    IComparable,
    IGH_QuickCast
    where T : EDBS.DataType, new()
  {
    protected DataType() { }
    protected DataType(T value) => Value = value;

    public virtual string Text
    {
      get
      {
        if (!IsValid) return "<invalid>";
        if (IsEmpty) return "<empty>";
        return Value.Label;
      }
    }

    public T Value { get; set; }

    public bool IsEmpty => Value == EDBS.DataType.Empty;

    #region IGH_Goo
    public virtual bool IsValid => Value != default;
    public virtual string IsValidWhyNot => IsValid ? default : "Not Valid";

    string IGH_Goo.ToString() => Value.Label;

    public virtual string TypeName
    {
      get
      {
        var name = GetType().GetTypeInfo().GetCustomAttribute<NameAttribute>();
        return name?.Name ?? typeof(T).Name;
      }
    }

    public virtual string TypeDescription
    {
      get
      {
        var name = GetType().GetTypeInfo().GetCustomAttribute<DescriptionAttribute>();
        return name?.Description ?? typeof(T).Name;
      }
    }

    public IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

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
        Value = (T) Activator.CreateInstance(typeof(T), typeId);
      else
        Value = default;

      return true;
    }
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

    static UnitType[] enumValues;
    public static IReadOnlyCollection<UnitType> EnumValues
    {
      get
      {
        if (enumValues is null)
        {
          enumValues = typeof(EDBS.UnitType).
            GetProperties(BindingFlags.Public | BindingFlags.Static).
            Where(x => x.PropertyType == typeof(EDBS.UnitType)).
            Select(x => new UnitType((EDBS.UnitType) x.GetValue(null))).
            OrderBy(x => x.Value.Label).
            ToArray();
        }

        return enumValues;
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
          case DB.ForgeTypeId f: Value = f; break;
#else
          case int i: Value = (DB.DisplayUnitType) i; break;
          case DB.DisplayUnitType u: Value = u; break;
#endif
          case EDBS.DataType s: Value = new EDBS.UnitType(s.TypeId); break;
          case string t: Value = new EDBS.UnitType(t); break;
          default: return base.CastFrom(source);
        }

        return true;
      }
      catch (ArgumentException) { return false; }
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
          return "Family Type";

        return Value.Label;
      }
    }


    static ParameterType[] enumValues;
    public static IReadOnlyCollection<ParameterType> EnumValues
    {
      get
      {
        if (enumValues is null)
        {
          enumValues = typeof(EDBS.SpecType.Measurable).
            GetProperties(BindingFlags.Public | BindingFlags.Static).
            Where(x => x.PropertyType == typeof(EDBS.SpecType.Measurable)).
            Select(x => new ParameterType((EDBS.UnitType) x.GetValue(null))).
            OrderBy(x => x.Value.FullName).
            ToArray();
        }

        return enumValues;
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
          case DB.ForgeTypeId f: Value = f; break;
#else
          case int i: Value = (EDBS.SpecType) (DB.ParameterType) i; break;
          case DB.ParameterType u: Value = (EDBS.SpecType) u; break;
          case DB.UnitType t: Value = (EDBS.SpecType) t; break;
#endif
          case EDBS.DataType s: Value = new EDBS.DataType(s.TypeId); break;
          case string t: Value = new EDBS.SpecType(t); break;
          default: return base.CastFrom(source);
        }

        return true;
      }
      catch (ArgumentException) { return false; }
    }
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

    static ParameterGroup[] enumValues;
    public static IReadOnlyCollection<ParameterGroup> EnumValues
    {
      get
      {
        if (enumValues is null)
        {
          enumValues = typeof(EDBS.ParameterGroup).
            GetProperties(BindingFlags.Public | BindingFlags.Static).
            Where(x => x.PropertyType == typeof(EDBS.ParameterGroup)).
            Select(x => new ParameterGroup((EDBS.ParameterGroup) x.GetValue(null))).
            OrderBy(x => x.Value.Label).
            ToArray();
        }

        return enumValues;
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
          case DB.ForgeTypeId f: Value = f; break;
#endif
#if !REVIT_2022
          case int i: Value = (DB.BuiltInParameterGroup) i; break;
          case DB.BuiltInParameterGroup u: Value = u; break;
#endif
          case EDBS.ParameterGroup v: Value = v; break;
          case EDBS.DataType s: Value = new EDBS.ParameterGroup(s.TypeId); break;
          case string t: Value = new EDBS.ParameterGroup(t); break;
          default: return base.CastFrom(source);
        }

        return true;
      }
      catch (ArgumentException) { return false; }
    }
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

    static ParameterId[] enumValues;
    public static IReadOnlyCollection<ParameterId> EnumValues
    {
      get
      {
        if (enumValues is null)
        {
          enumValues = typeof(EDBS.ParameterId).
            GetProperties(BindingFlags.Public | BindingFlags.Static).
            Where(x => x.PropertyType == typeof(EDBS.ParameterId)).
            Select(x => new ParameterId((EDBS.ParameterId) x.GetValue(null))).
            OrderBy(x => x.Value.Label).
            ToArray();
        }

        return enumValues;
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
          case DB.ForgeTypeId f: Value = f; break;
#endif
#if !REVIT_2022
          case int i: Value = (DB.BuiltInParameter) i; break;
          case DB.BuiltInParameter u: Value = u; break;
#endif
          case EDBS.ParameterId v: Value = v; break;
          case EDBS.DataType s: Value = new EDBS.ParameterId(s.TypeId); break;
          case string t: Value = new EDBS.ParameterId(t); break;
          default: return base.CastFrom(source);
        }

        return true;
      }
      catch (ArgumentException) { return false; }
    }
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

    static CategoryId[] enumValues;
    public static IReadOnlyCollection<CategoryId> EnumValues
    {
      get
      {
        if (enumValues is null)
        {
          enumValues = typeof(EDBS.CategoryId).
            GetProperties(BindingFlags.Public | BindingFlags.Static).
            Where(x => x.PropertyType == typeof(EDBS.CategoryId)).
            Select(x => new CategoryId((EDBS.CategoryId) x.GetValue(null))).
            OrderBy(x => x.Value.Label).
            ToArray();
        }

        return enumValues;
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
          case DB.ForgeTypeId f: Value = f; break;
#endif
#if !REVIT_2022
          case int i: Value = (DB.BuiltInCategory) i; break;
          case DB.BuiltInCategory u: Value = u; break;
#endif
          case EDBS.CategoryId v: Value = v; break;
          case EDBS.DataType s: Value = new EDBS.CategoryId(s.TypeId); break;
          case string t: Value = new EDBS.CategoryId(t); break;
          default: return base.CastFrom(source);
        }

        return true;
      }
      catch (ArgumentException) { return false; }
    }
  }
}
