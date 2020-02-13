using System;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class ParameterKey : Element
  {
    public override string TypeName => "Revit ParameterKey";
    public override string TypeDescription => "Represents a Revit parameter definition";
    override public object ScriptVariable() => null;
    protected override Type ScriptVariableType => typeof(DB.ParameterElement);

    #region IGH_ElementId
    public override bool LoadElement()
    {
      if (Document is null)
      {
        Value = null;
        if (!Revit.ActiveUIApplication.TryGetDocument(DocumentGUID, out var doc))
        {
          Document = null;
          return false;
        }

        Document = doc;
      }
      else if (IsElementLoaded)
        return true;

      if (Document is object)
        return Document.TryGetParameterId(UniqueID, out m_value);

      return false;
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
      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      var parameterId = DB.ElementId.InvalidElementId;
      switch (source)
      {
        case DB.ParameterElement     parameterElement: SetValue(parameterElement.Document, parameterElement.Id); return true;
        case DB.Parameter            parameter:        SetValue(parameter.Element.Document, parameter.Id); return true;
        case DB.ElementId id:        parameterId = id; break;
        case int integer:            parameterId = new DB.ElementId(integer); break;
      }

      if (parameterId.IsParameterId(Revit.ActiveDBDocument))
      {
        SetValue(Revit.ActiveDBDocument, parameterId);
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Guid)))
      {
        target = (Q) (object) (Document.GetElement(Value) as DB.SharedParameterElement)?.GuidValue;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    new class Proxy : Element.Proxy
    {
      public Proxy(ParameterKey o) : base(o) { (this as IGH_GooProxy).UserString = FormatInstance(); }

      public override bool IsParsable() => true;
      public override string FormatInstance()
      {
        int value = owner.Value?.IntegerValue ?? -1;
        if (Enum.IsDefined(typeof(DB.BuiltInParameter), value))
          return ((DB.BuiltInParameter) value).ToString();

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

      DB.BuiltInParameter builtInParameter => owner.Id.TryGetBuiltInParameter(out var bip) ? bip : DB.BuiltInParameter.INVALID;
      DB.ParameterElement parameter => IsBuiltIn ? null : owner.Document?.GetElement(owner.Id) as DB.ParameterElement;

      [System.ComponentModel.Description("The Guid that identifies this parameter as a shared parameter.")]
      public Guid Guid => (parameter as DB.SharedParameterElement)?.GuidValue ?? Guid.Empty;
      [System.ComponentModel.Description("API Object Type.")]
      public override Type ObjectType => IsBuiltIn ? typeof(DB.BuiltInParameter) : parameter?.GetType();

      [System.ComponentModel.Category("Other"), System.ComponentModel.Description("Internal parameter data storage type.")]
      public DB.StorageType StorageType => builtInParameter != DB.BuiltInParameter.INVALID ? Revit.ActiveDBDocument.get_TypeOfStorage(builtInParameter) : parameter?.GetDefinition().ParameterType.ToStorageType() ?? DB.StorageType.None;
      [System.ComponentModel.Category("Other"), System.ComponentModel.Description("Visible in UI.")]
      public bool Visible => IsBuiltIn ? Valid : parameter?.GetDefinition().Visible ?? false;
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
  }

  public class ParameterValue : GH_Goo<DB.Parameter>
  {
    public override string TypeName => "Revit ParameterValue";
    public override string TypeDescription => "Represents a Revit parameter value on an element";
    protected Type ScriptVariableType => typeof(DB.Parameter);
    public override bool IsValid => Value is object;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    double ToRhino(double value, DB.ParameterType type)
    {
      switch (type)
      {
        case DB.ParameterType.Length: return value * Math.Pow(Revit.ModelUnits, 1.0);
        case DB.ParameterType.Area:   return value * Math.Pow(Revit.ModelUnits, 2.0);
        case DB.ParameterType.Volume: return value * Math.Pow(Revit.ModelUnits, 3.0);
      }

      return value;
    }

    public override bool CastFrom(object source)
    {
      if (source is DB.Parameter parameter)
      {
        Value = parameter;
        return true;
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsSubclassOf(ScriptVariableType))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(ScriptVariableType))
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
                     (Q) (object) new GH_Number(ToRhino(Value.AsDouble(), Value.Definition.ParameterType));
            return true;
          }
          else if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
          {
            if (Value.Element is object)
            {
              var value = Math.Round(ToRhino(Value.AsDouble(), Value.Definition.ParameterType));
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
          if (typeof(Q).IsSubclassOf(typeof(ElementId)))
          {
            target = Value.Element is null ? (Q) (object) null :
                     (Q) (object) ElementId.FromElementId(Value.Element.Document, Value.AsElementId());
            return true;
          }
          break;
      }

      return base.CastTo<Q>(ref target);
    }

    public override bool Equals(object obj)
    {
      if (obj is ParameterValue paramValue)
      {
        if
        (
          paramValue.Value.Id.IntegerValue == Value.Id.IntegerValue &&
          paramValue.Value.Element.Id.IntegerValue == Value.Element.Id.IntegerValue &&
          paramValue.Value.StorageType == Value.StorageType &&
          paramValue.Value.HasValue == Value.HasValue
        )
        {
          if (!Value.HasValue)
            return true;

          switch (Value.StorageType)
          {
            case DB.StorageType.None:    return true;
            case DB.StorageType.Integer: return paramValue.Value.AsInteger() == Value.AsInteger();
            case DB.StorageType.Double:  return paramValue.Value.AsDouble() == Value.AsDouble();
            case DB.StorageType.String:   return paramValue.Value.AsString() == Value.AsString();
            case DB.StorageType.ElementId: return paramValue.Value.AsElementId().IntegerValue == Value.AsElementId().IntegerValue;
          }
        }
      }

      return base.Equals(obj);
    }

    public override int GetHashCode() => Value.Id.IntegerValue;
    
    public override string ToString()
    {
      if (!IsValid)
        return null;

      string value = default;
      try
      {
        if (Value.HasValue)
        {
          switch (Value.StorageType)
          {
            case DB.StorageType.Integer:
              if (Value.Definition.ParameterType == DB.ParameterType.YesNo)
                value = Value.AsInteger() == 0 ? "False" : "True";
              else
                value = Value.AsInteger().ToString();
              break;
            case DB.StorageType.Double: value = ToRhino(Value.AsDouble(), Value.Definition.ParameterType).ToString(); break;
            case DB.StorageType.String: value = Value.AsString(); break;
            case DB.StorageType.ElementId:

              if (Value.Id.TryGetBuiltInParameter(out var builtInParameter))
              {
                if (builtInParameter == DB.BuiltInParameter.ID_PARAM || builtInParameter == DB.BuiltInParameter.SYMBOL_ID_PARAM)
                  return Value.AsElementId().IntegerValue.ToString();
              }

              if (ElementId.FromElementId(Value.Element.Document, Value.AsElementId()) is ElementId goo)
                return goo.ToString();

              value = string.Empty;
              break;
            default:
              throw new NotImplementedException();
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.InternalException) { }

      return value;
    }
  }
}
