using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using External.DB.Schemas;

  public class GlobalParameter : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("32E77D86-0BF5-4766-B5E7-E181044C3820");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var doc = activeApp.ActiveUIDocument?.Document;
      if (doc is null) return;

#if REVIT_2022
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.GlobalParameters);
      Menu_AppendItem
      (
        menu, $"Open Global Parameters…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        !doc.IsFamilyDocument && activeApp.CanPostCommand(commandId), false
      );
#endif
    }
    #endregion

    public GlobalParameter() : base
  (
    name: "Global Parameter",
    nickname: "GlobalParam",
    description: "Get-Set access component to global parameter values",
    category: "Revit",
    subCategory: "Parameter"
  )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P",
          Description = "A global parameter"
        }
      ),
      new ParamDefinition
      (
        new Param_GenericObject()
        {
          Name = "Value",
          NickName = "V",
          Description = "Parameter value",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P",
          Description = "The global parameter"
        }
      ),
      new ParamDefinition
      (
        new Param_GenericObject()
        {
          Name = "Value",
          NickName = "V",
          Description = "Parameter value",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    static IGH_Goo GetGoo(ARDB.GlobalParameter parameter)
    {
      var parameterValue = parameter.GetValue();
      if (parameterValue is null)
        return default;

      switch (parameterValue)
      {
        case ARDB.IntegerParameterValue i:
        {
          var value = i.Value;

          if (parameter.GetDefinition() is ARDB.Definition definition)
          {
            var dataType = definition.GetDataType();

            if (dataType == SpecType.Boolean.YesNo)
              return new GH_Boolean(value != 0);
          }

          return new GH_Integer(value);
        }

        case ARDB.DoubleParameterValue d:
        {
          var value = SpecType.IsMeasurableSpec(parameter.GetDefinition().GetDataType(), out var spec) ?
            UnitConvertible.InRhinoUnits(d.Value, spec) :
            d.Value;

          return new GH_Number(value);
        }

        case ARDB.StringParameterValue s:
        {
          return new GH_String(s.Value);
        }

        case ARDB.ElementIdParameterValue id:
        {
          return Types.Element.FromElementId(parameter.Document, id.Value);
        }

        default:
          throw new NotImplementedException($"Unssuported Global parameter type {parameterValue.GetType()}");
      }
    }

    static bool SetGoo(ARDB.GlobalParameter parameter, IGH_Goo value)
    {
      if (parameter is null || value is null)
        return default;

      using (var parameterValue = parameter.GetValue())
        switch (parameterValue)
        {
          case ARDB.IntegerParameterValue i:

            if (parameter.GetDefinition() is ARDB.Definition definition)
            {
              if (definition.GetDataType() == SpecType.Boolean.YesNo)
              {
                if (!GH_Convert.ToBoolean(value, out var boolean, GH_Conversion.Both))
                  throw new InvalidCastException();

                i.Value = boolean ? 1 : 0;
                parameter.SetValue(i);
                return true;
              }
              else if (parameter.Id.TryGetBuiltInParameter(out var builtInParameter))
              {
                if (builtInParameter.IsColor())
                {
                  if (!GH_Convert.ToColor(value, out var color, GH_Conversion.Both))
                    throw new InvalidCastException();

                  i.Value = (color.B >> 16) | (color.G << 8) | (color.R << 0);
                  parameter.SetValue(i);
                  return true;
                }
              }
            }

            if (!GH_Convert.ToInt32(value, out var integer, GH_Conversion.Both))
              throw new InvalidCastException();

            i.Value = integer;
            parameter.SetValue(i);
            return true;

          case ARDB.DoubleParameterValue d:
            if (!GH_Convert.ToDouble(value, out var real, GH_Conversion.Both))
              throw new InvalidCastException();

            d.Value = SpecType.IsMeasurableSpec(parameter.GetDefinition().GetDataType(), out var spec) ?
              UnitConvertible.InHostUnits(real, spec) :
              real;

            parameter.SetValue(d);
            return true;

          case ARDB.StringParameterValue s:
            if (!GH_Convert.ToString(value, out var text, GH_Conversion.Both))
              throw new InvalidCastException();

            s.Value = text;
            parameter.SetValue(s);
            return true;

          case ARDB.ElementIdParameterValue id:
            var element = new Types.Element();
            if (!element.CastFrom(value))
              throw new InvalidCastException();

            id.Value = parameter.Document.LookupElement(element.Document, element.Id);
            parameter.SetValue(id);
            return true;

          default:
            throw new NotImplementedException();
        }
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.ParameterKey.GetDocumentParameter(this, DA, "Parameter", out var key)) return;
      if (!Params.TryGetData(DA, "Value", out IGH_Goo value)) return;

      if (key.Value is ARDB.GlobalParameter global)
      {
        if (value is object)
        {
          StartTransaction(global.Document);
          SetGoo(global, value);
        }

        DA.SetData("Parameter", key);
        Params.TrySetData(DA, "Value", () => GetGoo(global));
      }
      else throw new Exceptions.RuntimeArgumentException("Value", $"Parameter '{key.Nomen}' is not a valid reference to a global parameter");
    }
  }
}
