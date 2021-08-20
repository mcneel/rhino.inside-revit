using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.DB.Schemas;
using DB = Autodesk.Revit.DB;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.ParameterElement
{
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
    description: "Get-Set accessor to global parameter values",
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

    static IGH_Goo GetGoo(DB.GlobalParameter parameter)
    {
      var parameterValue = parameter.GetValue();
      if (parameterValue is null)
        return default;

      switch (parameterValue)
      {
        case DB.IntegerParameterValue i:
        {
          var value = i.Value;

          if (parameter.GetDefinition() is DB.Definition definition)
          {
            var dataType = definition.GetDataType();

            if (dataType == SpecType.Boolean.YesNo)
              return new GH_Boolean(value != 0);

            if (parameter.Id.TryGetBuiltInParameter(out var builtInInteger))
            {
              switch (builtInInteger)
              {
                case DB.BuiltInParameter.AUTO_JOIN_CONDITION: return new Types.CurtainGridJoinCondition((DBX.CurtainGridJoinCondition) value);
                case DB.BuiltInParameter.AUTO_JOIN_CONDITION_WALL: return new Types.CurtainGridJoinCondition((DBX.CurtainGridJoinCondition) value);
                case DB.BuiltInParameter.SPACING_LAYOUT_U: return new Types.CurtainGridLayout((DBX.CurtainGridLayout) value);
                case DB.BuiltInParameter.SPACING_LAYOUT_1: return new Types.CurtainGridLayout((DBX.CurtainGridLayout) value);
                case DB.BuiltInParameter.SPACING_LAYOUT_VERT: return new Types.CurtainGridLayout((DBX.CurtainGridLayout) value);
                case DB.BuiltInParameter.SPACING_LAYOUT_V: return new Types.CurtainGridLayout((DBX.CurtainGridLayout) value);
                case DB.BuiltInParameter.SPACING_LAYOUT_2: return new Types.CurtainGridLayout((DBX.CurtainGridLayout) value);
                case DB.BuiltInParameter.SPACING_LAYOUT_HORIZ: return new Types.CurtainGridLayout((DBX.CurtainGridLayout) value);
                case DB.BuiltInParameter.WRAPPING_AT_INSERTS_PARAM: return new Types.WallWrapping((DBX.WallWrapping) value);
                case DB.BuiltInParameter.WRAPPING_AT_ENDS_PARAM: return new Types.WallWrapping((DBX.WallWrapping) value);
                case DB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM: return new Types.StructuralWallUsage((DB.Structure.StructuralWallUsage) value);
                case DB.BuiltInParameter.WALL_KEY_REF_PARAM: return new Types.WallLocationLine((DB.WallLocationLine) value);
                case DB.BuiltInParameter.FUNCTION_PARAM: return new Types.WallFunction((DB.WallFunction) value);
              }

              var builtInIntegerName = builtInInteger.ToString();
              if (builtInIntegerName.Contains("COLOR_") || builtInIntegerName.Contains("_COLOR_") || builtInIntegerName.Contains("_COLOR"))
              {
                int r = value % 256;
                value /= 256;
                int g = value % 256;
                value /= 256;
                int b = value % 256;

                return new GH_Colour(System.Drawing.Color.FromArgb(r, g, b));
              }
            }
          }

          return new GH_Integer(value);
        }
        case DB.DoubleParameterValue d:
        {
          var value = SpecType.IsMeasurableSpec(parameter.GetDefinition().GetDataType(), out var spec) ?
            UnitConverter.InRhinoUnits(d.Value, spec) :
            d.Value;

          return new GH_Number(value);
        }

        case DB.StringParameterValue s:
        {
          return new GH_String(s.Value);
        }

        case DB.ElementIdParameterValue id:
        {
          var value = id.Value;
          if (parameter.Id.TryGetBuiltInParameter(out var builtInElementId))
          {
            if (builtInElementId == DB.BuiltInParameter.ID_PARAM || builtInElementId == DB.BuiltInParameter.SYMBOL_ID_PARAM)
              return new GH_Integer(value.IntegerValue);
          }

          return Types.Element.FromElementId(parameter.Document, value);
        }

        default:
          throw new NotImplementedException();
      }
    }

    static bool SetGoo(DB.GlobalParameter parameter, IGH_Goo value)
    {
      if (parameter is null || value is null)
        return default;

      using (var parameterValue = parameter.GetValue())
        switch (parameterValue)
        {
          case DB.IntegerParameterValue i:

            if (parameter.GetDefinition() is DB.Definition definition)
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
                var builtInParameterName = builtInParameter.ToString();
                if (builtInParameterName.Contains("COLOR_") || builtInParameterName.Contains("_COLOR_") || builtInParameterName.Contains("_COLOR"))
                {
                  if (!GH_Convert.ToColor(value, out var color, GH_Conversion.Both))
                    throw new InvalidCastException();

                  i.Value = ((int) color.R) | ((int) color.G << 8) | ((int) color.B << 16);
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

          case DB.DoubleParameterValue d:
            if (!GH_Convert.ToDouble(value, out var real, GH_Conversion.Both))
              throw new InvalidCastException();

            d.Value = SpecType.IsMeasurableSpec(parameter.GetDefinition().GetDataType(), out var spec) ?
              UnitConverter.InHostUnits(real, spec) :
              real;

            parameter.SetValue(d);
            return true;

          case DB.StringParameterValue s:
            if (!GH_Convert.ToString(value, out var text, GH_Conversion.Both))
              throw new InvalidCastException();

            s.Value = text;
            parameter.SetValue(s);
            return true;

          case DB.ElementIdParameterValue id:
            var element = new Types.Element();
            if (!element.CastFrom(value))
              throw new InvalidCastException();

            if (!parameter.Document.IsEquivalent(element.Document))
              throw new ArgumentException("Failed to assign an element from a diferent document.", parameter.Name);

            id.Value = element.Id;
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

      if (key.Value is DB.GlobalParameter global)
      {
        if (value is object)
        {
          StartTransaction(global.Document);
          SetGoo(global, value);
        }

        DA.SetData("Parameter", key);
        Params.TrySetData(DA, "Value", () => GetGoo(global));
      }
      else throw new Exceptions.RuntimeWarningException($"Parameter '{key.Name}' is not a valid reference to a global parameter");
    }
  }
}
