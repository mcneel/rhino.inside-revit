using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.GH.Types.Elements;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types.Documents.Params
{
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
          if (typeof(Q).IsSubclassOf(typeof(DB.ElementId)))
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
