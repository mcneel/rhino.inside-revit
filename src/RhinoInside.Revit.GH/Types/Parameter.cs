using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;
using DBXS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Parameter Key")]
  public class ParameterKey : Element
  {
    #region IGH_Goo
    public override bool IsValid => (Id?.TryGetBuiltInParameter(out var _) == true) || base.IsValid;

    protected override Type ScriptVariableType => typeof(DB.ParameterElement);
    public override object ScriptVariable() => null;

    public sealed override bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var parameterId = DB.ElementId.InvalidElementId;

      if (source is ValueTuple<DB.Document, DB.ElementId> tuple)
      {
        (document, parameterId) = tuple;
      }
      else if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
        {
          document = element.Document;
          parameterId = element.Id;
        }
        else if (source is ParameterId id)
        {
          source = (DB.BuiltInParameter) id.Value;
        }
        else source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int integer:            parameterId = new DB.ElementId(integer); break;
        case DB.BuiltInParameter bip:parameterId = new DB.ElementId(bip); break;
        case DB.ElementId id:        parameterId = id; break;
        case DB.Parameter parameter: SetValue(parameter.Element.Document, parameter.Id); return true;
      }

      if (parameterId.TryGetBuiltInParameter(out var _))
      {
        SetValue(document, parameterId);
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Guid)))
      {
        target = (Q) (object) (Document.GetElement(Id) as DB.SharedParameterElement)?.GuidValue;
        return true;
      }

      return base.CastTo<Q>(out target);
    }

    new class Proxy : Element.Proxy
    {
      protected new ParameterKey owner => base.owner as ParameterKey;

      public Proxy(ParameterKey o) : base(o) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => true;
      public override string FormatInstance()
      {
        int value = owner.Id?.IntegerValue ?? -1;
        if (Enum.IsDefined(typeof(DB.BuiltInParameter), value))
          return ((DB.BuiltInParameter) value).ToStringGeneric();

        return value.ToString();
      }
      public override bool FromString(string str)
      {
        if (Enum.TryParse(str, out DB.BuiltInParameter builtInParameter))
        {
          owner.SetValue(owner.Document ?? Revit.ActiveUIDocument.Document, new DB.ElementId(builtInParameter));
          return true;
        }

        return false;
      }

      #region Misc
      protected override bool IsValidId(DB.Document doc, DB.ElementId id) => id.IsParameterId(doc);
      public override Type ObjectType => IsBuiltIn ? typeof(DB.BuiltInParameter) : base.ObjectType;

      [System.ComponentModel.Description("BuiltIn parameter Id.")]
      public DB.BuiltInParameter? BuiltInId => owner.Id.TryGetBuiltInParameter(out var bip) ? bip : default;

      [System.ComponentModel.Description("Forge Id.")]
      public DBXS.ParameterId SchemaId => owner.Id.TryGetBuiltInParameter(out var bip) ? (DBXS.ParameterId) bip : default;
      #endregion

      #region Definition
      const string Definition = "Definition";
      DB.ParameterElement parameter => owner.Value as DB.ParameterElement;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("The Guid that identifies this parameter as a shared parameter.")]
      public Guid? Guid => (parameter as DB.SharedParameterElement)?.GuidValue;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Internal parameter data storage type.")]
      public DB.StorageType? StorageType => BuiltInId.HasValue ? Revit.ActiveDBDocument?.get_TypeOfStorage(BuiltInId.Value) : parameter?.GetDefinition()?.GetDataType().ToStorageType();

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Visible in UI.")]
      public bool? Visible => parameter?.GetDefinition()?.Visible;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Whether or not the parameter values can vary across group members.")]
      public bool? VariesAcrossGroups => parameter?.GetDefinition()?.VariesAcrossGroups;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Parameter data Type")]
      public string Type => ((DBXS.DataType) parameter?.GetDefinition()?.GetDataType())?.Label;

      [System.ComponentModel.Category(Definition)]
      public string Group => parameter?.GetDefinition()?.GetGroupType()?.Label;
      #endregion
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);
    #endregion

    public new DB.ParameterElement Value => base.Value as DB.ParameterElement;

    public ParameterKey() { }
    public ParameterKey(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ParameterKey(DB.ParameterElement element) : base(element) { }

    public static new ParameterKey FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsParameterId(doc))
        return new ParameterKey(doc, id);

      return null;
    }

    public override string DisplayName
    {
      get
      {
        try
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
            return DB.LabelUtils.GetLabelFor(builtInParameter) ?? base.DisplayName;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.DisplayName;
      }
    }

    #region Properties
    public override string Name
    {
      get
      {
        try
        {
          if (Id is object && Id.TryGetBuiltInParameter(out var builtInParameter))
            return DB.LabelUtils.GetLabelFor(builtInParameter) ?? base.Name;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        return base.Name;
      }
    }

    public DBX.ParameterClass Class
    {
      get
      {
        if (Id is object && Id.TryGetBuiltInParameter(out var _))
          return DBX.ParameterClass.BuiltIn;

        switch (Value)
        {
          case DB.GlobalParameter _: return DBX.ParameterClass.Global;
          case DB.SharedParameterElement _: return DBX.ParameterClass.Shared;
          case DB.ParameterElement project:
            switch (project.get_Parameter(DB.BuiltInParameter.ELEM_DELETABLE_IN_FAMILY).AsInteger())
            {
              case 0: return DBX.ParameterClass.Family;
              case 1: return DBX.ParameterClass.Project;
            }
            break;
        }

        return default;
      }
    }
    public DB.Definition Definition
    {
      get
      {
        if (Value is DB.ParameterElement element)
          return element.GetDefinition();

        return default;
      }
    }

    public Guid? GuidValue => Value is DB.SharedParameterElement shared ? shared.GuidValue : default;
    #endregion
  }

  [Kernel.Attributes.Name("Parameter Value")]
  public class ParameterValue : ReferenceObject, IEquatable<ParameterValue>, IGH_Goo, IGH_QuickCast, IConvertible
  {
    #region System.Object
    public bool Equals(ParameterValue other)
    {
      if (other is null) return false;
      if (Value is DB.Parameter A && other.Value is DB.Parameter B)
      {
        if
        (
          A.Id.IntegerValue == B.Id.IntegerValue &&
          A.StorageType == B.StorageType &&
          A.HasValue == B.HasValue
        )
        {
          if (!Value.HasValue)
            return true;

          switch (Value.StorageType)
          {
            case DB.StorageType.None: return true;
            case DB.StorageType.Integer: return A.AsInteger() == B.AsInteger();
            case DB.StorageType.Double: return A.AsDouble() == B.AsDouble();
            case DB.StorageType.String: return A.AsString() == B.AsString();
            case DB.StorageType.ElementId: return A.AsElementId() == B.AsElementId();
          }
        }
      }

      return false;
    }
    public override bool Equals(object obj) => (obj is ElementId id) ? Equals(id) : base.Equals(obj);
    public override int GetHashCode()
    {
      int hashCode = 0;
      if (Value is DB.Parameter value)
      {
        hashCode ^= value.Id.GetHashCode();
        hashCode ^= value.StorageType.GetHashCode();

        if (value.HasValue)
        {
          switch (value.StorageType)
          {
            case DB.StorageType.Integer: hashCode ^= value.AsInteger().GetHashCode(); break;
            case DB.StorageType.Double: hashCode ^= value.AsDouble().GetHashCode(); break;
            case DB.StorageType.String: hashCode ^= value.AsString().GetHashCode(); break;
            case DB.StorageType.ElementId: hashCode ^= value.AsElementId().GetHashCode(); break;
          }
        }
      }

      return hashCode;
    }
    public override string ToString()
    {
      return Value.AsGoo()?.ToString();
    }
    #endregion

    #region IGH_Goo
    public override bool IsValid => base.IsValid && Value is object;
    public override bool CastFrom(object source)
    {
      if (source is DB.Parameter parameter)
      {
        base.Value = parameter;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.Parameter)))
      {
        target = (Q) (object) Value;
        return true;
      }

      var goo = Value.AsGoo();
      if (goo is null)
      {
        target = default;
        return true;
      }

      if (goo is Q q)
      {
        target = q;
        return true;
      }

      return goo.CastTo(out target);
    }

    object IGH_Goo.ScriptVariable() => Value.AsGoo() is IGH_Goo goo ? goo.ScriptVariable() : default;
    #endregion

    #region IGH_QuickCast
    GH_QuickCastType IGH_QuickCast.QC_Type
    {
      get
      {
        //return GH_QuickCastType.text;
        switch (Value.ToConvertible())
        {
          default:
          case bool     _: return GH_QuickCastType.@bool;
          case char     _: return GH_QuickCastType.@int;
          case sbyte    _: return GH_QuickCastType.@int;
          case byte     _: return GH_QuickCastType.@int;
          case short    _: return GH_QuickCastType.@int;
          case ushort   _: return GH_QuickCastType.@int;
          case int      _: return GH_QuickCastType.@int;
          case uint     _: return GH_QuickCastType.@int;
          case long     _: return GH_QuickCastType.@int;
          case ulong    _: return GH_QuickCastType.@int;
          case float    _: return GH_QuickCastType.num;
          case double   _: return GH_QuickCastType.num;
          case decimal  _: return GH_QuickCastType.num;
          case DateTime _: return GH_QuickCastType.num;
          case string   _: return GH_QuickCastType.text;
        }
      }
    }

    double IGH_QuickCast.QC_Distance(IGH_QuickCast other)
    {
      var quickCast = Value.AsGoo() as IGH_QuickCast;
      var quickType = quickCast?.QC_Type ?? (GH_QuickCastType) (-1);

      switch (quickType)
      {
        case GH_QuickCastType.@bool:    return Math.Abs((quickCast.QC_Bool() ? 0.0 : 1.0) - (other.QC_Bool() ? 0.0 : 1.0));
        case GH_QuickCastType.@int:     return Math.Abs(((double) quickCast.QC_Int()) - ((double) other.QC_Int()));
        case GH_QuickCastType.num:      return Math.Abs(((double) quickCast.QC_Num()) - ((double) other.QC_Num()));
        case GH_QuickCastType.text:
          var thisText = quickCast.QC_Text(); var otherText = other.QC_Text();
          var dist0 = Grasshopper.Kernel.GH_StringMatcher.LevenshteinDistance(thisText, otherText);
          var dist1 = Grasshopper.Kernel.GH_StringMatcher.LevenshteinDistance(thisText.ToUpperInvariant(), otherText.ToUpperInvariant());
          return 0.5 * (dist0 + dist1);
        case GH_QuickCastType.col:
          var thisCol = quickCast.QC_Col(); var otherCol = other.QC_Col();
          var colorRGBA = new Rhino.Geometry.Point4d(thisCol.R - otherCol.R, thisCol.G - otherCol.G, thisCol.B - otherCol.B, thisCol.A - otherCol.A);
          colorRGBA *= 1.0 / 255.0;
          return Math.Sqrt(colorRGBA.X * colorRGBA.X + colorRGBA.Y * colorRGBA.Y + colorRGBA.Z * colorRGBA.Z + colorRGBA.W * colorRGBA.W);
        case GH_QuickCastType.pt:       return quickCast.QC_Pt().DistanceTo(other.QC_Pt());
        case GH_QuickCastType.vec:      return ((Rhino.Geometry.Point3d) quickCast.QC_Vec()).DistanceTo((Rhino.Geometry.Point3d) other.QC_Vec());
        case GH_QuickCastType.complex:  throw new InvalidOperationException();
        case GH_QuickCastType.interval: throw new InvalidOperationException();
        case GH_QuickCastType.matrix:   throw new InvalidOperationException();
        default:                        throw new InvalidOperationException();
      }
    }

    int IGH_QuickCast.QC_Hash() => Value.ToConvertible()?.GetHashCode() ?? 0;
    bool IGH_QuickCast.QC_Bool() => System.Convert.ToBoolean(Value.ToConvertible());
    int IGH_QuickCast.QC_Int() => System.Convert.ToInt32(Value.ToConvertible());
    double IGH_QuickCast.QC_Num() => System.Convert.ToDouble(Value.ToConvertible());
    string IGH_QuickCast.QC_Text() => System.Convert.ToString(Value.ToConvertible());
    System.Drawing.Color IGH_QuickCast.QC_Col() => System.Drawing.Color.FromArgb(System.Convert.ToInt32(Value.ToConvertible()));
    Rhino.Geometry.Point3d IGH_QuickCast.QC_Pt() => (Value.AsGoo() as IGH_QuickCast)?.QC_Pt() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Point3d)}");
    Rhino.Geometry.Vector3d IGH_QuickCast.QC_Vec() => (Value.AsGoo() as IGH_QuickCast)?.QC_Vec() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Vector3d)}");
    Complex IGH_QuickCast.QC_Complex() => (Value.AsGoo() as IGH_QuickCast)?.QC_Complex() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Complex)}");
    Rhino.Geometry.Matrix IGH_QuickCast.QC_Matrix() => (Value.AsGoo() as IGH_QuickCast)?.QC_Matrix() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Matrix)}");
    Rhino.Geometry.Interval IGH_QuickCast.QC_Interval() => (Value.AsGoo() as IGH_QuickCast)?.QC_Interval() ??
      throw new InvalidCastException($"{GetType().Name} cannot be cast to {nameof(Rhino.Geometry.Interval)}");
    int IGH_QuickCast.QC_CompareTo(IGH_QuickCast other)
    {
      var quickCast = Value.AsGoo() as IGH_QuickCast;
      var quickType = quickCast?.QC_Type ?? (GH_QuickCastType) (-1);

      if (quickType != other.QC_Type) quickType.CompareTo(other.QC_Type);
      return quickCast.QC_CompareTo(other);
    }
    #endregion

    #region IConvertible
    TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

    object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Value.ToConvertible().ToType(conversionType, provider);
    bool IConvertible.ToBoolean(IFormatProvider provider) => Value.ToConvertible().ToBoolean(provider);
    sbyte IConvertible.ToSByte(IFormatProvider provider) => Value.ToConvertible().ToSByte(provider);
    byte IConvertible.ToByte(IFormatProvider provider) => Value.ToConvertible().ToByte(provider);
    char IConvertible.ToChar(IFormatProvider provider) => Value.ToConvertible().ToChar(provider);
    short IConvertible.ToInt16(IFormatProvider provider) => Value.ToConvertible().ToInt16(provider);
    ushort IConvertible.ToUInt16(IFormatProvider provider) => Value.ToConvertible().ToUInt16(provider);
    uint IConvertible.ToUInt32(IFormatProvider provider) => Value.ToConvertible().ToUInt32(provider);
    int IConvertible.ToInt32(IFormatProvider provider) => Value.ToConvertible().ToInt32(provider);
    long IConvertible.ToInt64(IFormatProvider provider) => Value.ToConvertible().ToInt64(provider);
    ulong IConvertible.ToUInt64(IFormatProvider provider) => Value.ToConvertible().ToUInt64(provider);
    float IConvertible.ToSingle(IFormatProvider provider) => Value.ToConvertible().ToSingle(provider);
    double IConvertible.ToDouble(IFormatProvider provider) => Value.ToConvertible().ToDouble(provider);
    decimal IConvertible.ToDecimal(IFormatProvider provider) => Value.ToConvertible().ToDecimal(provider);
    DateTime IConvertible.ToDateTime(IFormatProvider provider) => Value.ToConvertible().ToDateTime(provider);
    string IConvertible.ToString(IFormatProvider provider) => Value.ToConvertible().ToString(provider);
    #endregion

    #region DocumentObject
    public new DB.Parameter Value => base.Value as DB.Parameter;

    public override string DisplayName
    {
      get
      {
        if (Value is DB.Parameter param)
          return param.Definition?.Name;

        return default;
      }
    }
    #endregion

    #region ReferenceObject
    public override DB.ElementId Id => Value?.Element.Id;
    #endregion

    public ParameterValue() { }
    public ParameterValue(DB.Parameter value) : base(value.Element.Document, value) { }
  }
}
