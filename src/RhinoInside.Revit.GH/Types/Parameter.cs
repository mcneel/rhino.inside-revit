using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Kernel.Attributes;

  [Name("Parameter Key")]
  public class ParameterKey : Element
  {
    protected override Type ScriptVariableType => typeof(DB.ParameterElement);
    override public object ScriptVariable() => null;

    #region IGH_ElementId
    public override bool LoadElement()
    {
      if (IsReferencedElement && !IsElementLoaded)
      {
        Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc);
        doc.TryGetParameterId(UniqueID, out var id);

        SetValue(doc, id);
      }

      return IsElementLoaded;
    }
    #endregion

    public ParameterKey() { }
    public ParameterKey(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ParameterKey(DB.ParameterElement element) : base(element) { }

    new public static ParameterKey FromElementId(DB.Document doc, DB.ElementId id)
    {
      if (id.IsParameterId(doc))
        return new ParameterKey(doc, id);

      return null;
    }

    public override sealed bool CastFrom(object source)
    {
      if (base.CastFrom(source))
        return true;

      var document = Revit.ActiveDBDocument;
      var parameterId = DB.ElementId.InvalidElementId;

      if (source is IGH_Goo goo)
      {
        if (source is IGH_Element element)
          source = element.Document?.GetElement(element.Id);
        else
          source = goo.ScriptVariable();
      }

      switch (source)
      {
        case int integer:            parameterId = new DB.ElementId(integer); break;
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
      #endregion

      #region Definition
      const string Definition = "Definition";
      DB.ParameterElement parameter => owner.Value as DB.ParameterElement;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("The Guid that identifies this parameter as a shared parameter.")]
      public Guid? Guid => (parameter as DB.SharedParameterElement)?.GuidValue;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Internal parameter data storage type.")]
      public DB.StorageType? StorageType => BuiltInId.HasValue ? Revit.ActiveDBDocument?.get_TypeOfStorage(BuiltInId.Value) : parameter?.GetDefinition()?.ParameterType.ToStorageType();

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Visible in UI.")]
      public bool? Visible => parameter?.GetDefinition()?.Visible;

      [System.ComponentModel.Category(Definition), System.ComponentModel.Description("Whether or not the parameter values can vary across group members.")]
      public bool? VariesAcrossGroups => parameter?.GetDefinition()?.VariesAcrossGroups;

      [System.ComponentModel.Category(Definition)]
      public DB.ParameterType? Type => parameter?.GetDefinition()?.ParameterType;

      [System.ComponentModel.Category(Definition)]
      public DB.BuiltInParameterGroup? Group => parameter?.GetDefinition()?.ParameterGroup;
      #endregion
    }

    public override IGH_GooProxy EmitProxy() => new Proxy(this);

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
      set
      {
        if (value is object && value != Name)
        {
          if (Id.IsBuiltInId())
            throw new InvalidOperationException($"BuiltIn paramater '{Name}' does not support assignment of a user-specified name.");

          base.Name = value;
        }
      }
    }
    #endregion
  }

  [Name("Parameter Value")]
  public class ParameterValue : ReferenceObject, IEquatable<ParameterValue>
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
      if (!IsValid)
        return null;

      try
      {
        if (Value.HasValue)
        {
          switch (Value.StorageType)
          {
            case DB.StorageType.Integer:
              if (Value.Definition.ParameterType == DB.ParameterType.YesNo)
                return (Value.AsInteger() != 0).ToString();
              else
                return Value.AsInteger().ToString();

            case DB.StorageType.Double: return Value.AsDoubleInRhinoUnits().ToString();
            case DB.StorageType.String: return Value.AsString();
            case DB.StorageType.ElementId:

              var id = Value.AsElementId();
              if (Value.Id.TryGetBuiltInParameter(out var builtInParameter))
              {
                if (builtInParameter == DB.BuiltInParameter.ID_PARAM || builtInParameter == DB.BuiltInParameter.SYMBOL_ID_PARAM)
                  return id.IntegerValue.ToString();
              }

              if (Element.FromElementId(Value.Element.Document, id) is Element element)
                return element.ToString();

              if (id == DB.ElementId.InvalidElementId)
                return new Types.Element().ToString();

              return id.IntegerValue.ToString();

            default:
              throw new NotImplementedException();
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.InternalException) { }

      return default;
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

      switch (Value.StorageType)
      {
        case DB.StorageType.Integer:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Boolean)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Boolean(Value.AsInteger() != 0);
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Integer(Value.AsInteger());
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Number((double)Value.AsInteger());
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Colour)))
          {
            if (Value.Element is object)
            {
              int value = Value.AsInteger();
              int r = value % 256;
              value /= 256;
              int g = value % 256;
              value /= 256;
              int b = value % 256;

              target = (Q) (object) new GH_Colour(System.Drawing.Color.FromArgb(r, g, b));
            }
            else
              target = (Q) (object) null;
            return true;
          }
          break;
        case DB.StorageType.Double:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Number)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_Number(Value.AsDoubleInRhinoUnits());
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
          {
            if (Value.Element is object)
            {
              var value = Math.Round(Value.AsDoubleInRhinoUnits());
              if (int.MinValue <= value && value <= int.MaxValue)
              {
                target = (Q) (object) new GH_Integer((int) value);
                return true;
              }
            }
            else
            {
              target = (Q) (object) null;
              return true;
            }
          }
          break;
        case DB.StorageType.String:
          if (typeof(Q).IsAssignableFrom(typeof(GH_String)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) new GH_String(Value.AsString());
            return true;
          }
          break;
        case DB.StorageType.ElementId:
          if (typeof(Q).IsAssignableFrom(typeof(Element)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) Element.FromElementId(Value.Element.Document, Value.AsElementId());
            return true;
          }
          break;
      }

      return base.CastTo<Q>(out target);
    }
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
