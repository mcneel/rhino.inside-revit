using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using static System.Math;
using static Rhino.RhinoMath;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  internal static class ElementParameterUtils
  {
    public static DB.Parameter GetParameter(IGH_ActiveObject obj, DB.Element element, IGH_Goo key)
    {
      DB.Parameter parameter = null;
      switch (key as Types.Documents.Params.ParameterKey ?? key.ScriptVariable())
      {
        case Types.Documents.Params.ParameterKey parameterKey:
          if (parameterKey.Document.Equals(element.Document))
          {
            if (Enum.IsDefined(typeof(DB.BuiltInParameter), parameterKey.Id.IntegerValue))
            {
              parameter = element.get_Parameter((DB.BuiltInParameter) parameterKey.Id.IntegerValue);
              if (parameter is null)
                obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{DB.LabelUtils.GetLabelFor((DB.BuiltInParameter) parameterKey.Id.IntegerValue)}' not defined in 'Element'");
            }
            else if (element.Document.GetElement(parameterKey.Id) is DB.ParameterElement parameterElement)
            {
              parameter = element.get_Parameter(parameterElement.GetDefinition());
              if (parameter is null)
                obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterElement.Name}' not defined in 'Element'");
            }
            else
              obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Data conversion failed from {key.TypeName} to Revit Parameter element");
          }
          else
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'ParameterKey' doesn't belong same document as 'Element'");

          break;

        case DB.Parameter param:
          if (param.Element.Document.Equals(element.Document) && param.Element.Id == element.Id)
            parameter = param;
          else
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Parameter '{param.Definition.Name}' doesn't belong to 'Element'");

          break;

        case string parameterName:
          parameter = element.GetParameter(parameterName, RevitAPI.ParameterSet.Any);
          if (parameter is null)
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterName}' not defined in 'Element'");
          break;

        case int parameterId:
          if (Enum.IsDefined(typeof(DB.BuiltInParameter), parameterId))
          {
            parameter = element.get_Parameter((DB.BuiltInParameter) parameterId);
            if (parameter is null)
              obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{DB.LabelUtils.GetLabelFor((DB.BuiltInParameter) parameterId)}' not defined in 'Element'");
          }
          else if (element.Document.GetElement(new DB.ElementId(parameterId)) is DB.ParameterElement parameterElement)
          {
            parameter = element.get_Parameter(parameterElement.GetDefinition());
            if (parameter is null)
              obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterElement.Name}' not defined in 'Element'");
          }
          else
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Data conversion failed from {key.TypeName} to Revit Parameter element");
          break;

        case DB.ElementId parameterElementId:
          if (Enum.IsDefined(typeof(DB.BuiltInParameter), parameterElementId.IntegerValue))
          {
            parameter = element.get_Parameter((DB.BuiltInParameter) parameterElementId.IntegerValue);
            if (parameter is null)
              obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{DB.LabelUtils.GetLabelFor((DB.BuiltInParameter) parameterElementId.IntegerValue)}' not defined in 'Element'");
          }
          else if (element.Document.GetElement(parameterElementId) is DB.ParameterElement parameterElement)
          {
            parameter = element.get_Parameter(parameterElement.GetDefinition());
            if (parameter is null)
              obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterElement.Name}' not defined in 'Element'");
          }
          else
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Data conversion failed from {key.TypeName} to Revit Parameter element");
          break;

        case Guid guid:
          parameter = element.get_Parameter(guid);
          if (parameter is null)
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{guid}' not defined in 'Element'");
          break;

        default:
          obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Data conversion failed from {key.TypeName} to Revit Parameter element");
          break;
      }

      return parameter;
    }

    public static bool SetParameter(IGH_ActiveObject obj, DB.Parameter parameter, IGH_Goo goo)
    {
      if (goo is null)
        return false;

      try
      {
        var element = parameter.Element;
        var value = goo.ScriptVariable();
        var document = default(DB.Document);
        if (value is DB.Parameter paramValue)
        {
          switch (paramValue.StorageType)
          {
            case DB.StorageType.Integer: value = paramValue.AsInteger(); break;
            case DB.StorageType.Double: value = paramValue.AsDouble(); break;
            case DB.StorageType.String: value = paramValue.AsString(); break;
            case DB.StorageType.ElementId: value = paramValue.AsElementId(); document = paramValue.Element.Document; break;
          }
        }

        switch (parameter.StorageType)
        {
          case DB.StorageType.Integer:
            {
              switch (value)
              {
                case bool boolean: parameter.Set(boolean ? 1 : 0); break;
                case int integer: parameter.Set(integer); break;
                case double real: parameter.Set((int) Clamp(Round(ToHost(real, parameter.Definition.ParameterType)), int.MinValue, int.MaxValue)); break;
                case System.Drawing.Color color: parameter.Set(((int) color.R) | ((int) color.G << 8) | ((int) color.B << 16)); break;
                default: element = null; break;
              }
              break;
            }
          case DB.StorageType.Double:
            {
              switch (value)
              {
                case int integer: parameter.Set((double) integer); break;
                case double real: parameter.Set(ToHost(real, parameter.Definition.ParameterType)); break;
                default: element = null; break;
              }
              break;
            }
          case DB.StorageType.String:
            {
              switch (value)
              {
                case string str: parameter.Set(str); break;
                default: element = null; break;
              }
              break;
            }
          case DB.StorageType.ElementId:
            {
              switch (value)
              {
                case DB.Element ele:
                  if (element.Document.Equals(ele.Document)) parameter.Set(ele.Id);
                  else obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Referencing elements from other documents is not valid");
                  break;
                case DB.Category cat:
                  if (element.Document.Equals(cat.Document())) parameter.Set(cat.Id);
                  else obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Referencing categories from other documents is not valid");
                  break;
                case DB.ElementId id:
                  if (document is object)
                  {
                    if (element.Document.Equals(document)) parameter.Set(id);
                    else obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Referencing elements from other documents is not valid");
                  }
                  else element = null;
                  break;
                default: element = null; break;
              }
              break;
            }
          default:
            {
              element = null;
              break;
            }
        }

        if (element is null && parameter is object)
        {
          obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to cast from {value.GetType().Name} to {parameter.StorageType.ToString()}.");
          return false;
        }
      }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException e)
      {
        obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to set '{parameter.Definition.Name}' value : {e.Message}");
        return false;
      }

      return true;
    }

    static double ToHost(double value, DB.ParameterType type)
    {
      switch (type)
      {
        case DB.ParameterType.Length: return value / Pow(Revit.ModelUnits, 1.0);
        case DB.ParameterType.Area: return value / Pow(Revit.ModelUnits, 2.0);
        case DB.ParameterType.Volume: return value / Pow(Revit.ModelUnits, 3.0);
      }

      return value;
    }
  }
}
