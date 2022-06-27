using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH
{
  using Convert.Geometry;
  using External.DB.Extensions;

  static class ParameterUtils
  {
    internal static IGH_Goo AsGoo(this ARDB.Parameter parameter)
    {
      if (parameter is null) return default;

      // ALL_MODEL_FAMILY_NAME & ALL_MODEL_TYPE_NAME return null
      // but DB.ElementParameterFilter works on those parameters!?!?
      if (!parameter.HasValue)
      {
        switch ((ARDB.BuiltInParameter) parameter.Id.IntegerValue)
        {
          case ARDB.BuiltInParameter.ALL_MODEL_FAMILY_NAME:
            return ((parameter.Element.Document.GetElement(parameter.Element.GetTypeId()) as ARDB.ElementType)?.FamilyName)
              is string familyName ? new GH_String(familyName) : default;

          case ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME:
            return ((parameter.Element.Document.GetElement(parameter.Element.GetTypeId()) as ARDB.ElementType)?.Name)
              is string typeName ? new GH_String(typeName) : default;
        }

        return default;
      }

      switch (parameter.StorageType)
      {
        case ARDB.StorageType.Integer:
          var integer = parameter.AsInteger();

          if (parameter.Definition is ARDB.Definition definition)
          {
            var dataType = definition.GetDataType();

            if (dataType == ERDB.Schemas.SpecType.Boolean.YesNo)
              return new GH_Boolean(integer != 0);

            if (parameter.Id.TryGetBuiltInParameter(out var builtInInteger))
            {
              switch (builtInInteger)
              {
                case ARDB.BuiltInParameter.AUTO_JOIN_CONDITION: return new Types.CurtainGridJoinCondition((ERDB.CurtainGridJoinCondition) integer);
                case ARDB.BuiltInParameter.AUTO_JOIN_CONDITION_WALL: return new Types.CurtainGridJoinCondition((ERDB.CurtainGridJoinCondition) integer);
                case ARDB.BuiltInParameter.SPACING_LAYOUT_U: return new Types.CurtainGridLayout((ERDB.CurtainGridLayout) integer);
                case ARDB.BuiltInParameter.SPACING_LAYOUT_1: return new Types.CurtainGridLayout((ERDB.CurtainGridLayout) integer);
                case ARDB.BuiltInParameter.SPACING_LAYOUT_VERT: return new Types.CurtainGridLayout((ERDB.CurtainGridLayout) integer);
                case ARDB.BuiltInParameter.SPACING_LAYOUT_V: return new Types.CurtainGridLayout((ERDB.CurtainGridLayout) integer);
                case ARDB.BuiltInParameter.SPACING_LAYOUT_2: return new Types.CurtainGridLayout((ERDB.CurtainGridLayout) integer);
                case ARDB.BuiltInParameter.SPACING_LAYOUT_HORIZ: return new Types.CurtainGridLayout((ERDB.CurtainGridLayout) integer);
                case ARDB.BuiltInParameter.WRAPPING_AT_INSERTS_PARAM: return new Types.WallWrapping((ERDB.WallWrapping) integer);
                case ARDB.BuiltInParameter.WRAPPING_AT_ENDS_PARAM: return new Types.WallWrapping((ERDB.WallWrapping) integer);
                case ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM: return new Types.StructuralWallUsage((ARDB.Structure.StructuralWallUsage) integer);
                case ARDB.BuiltInParameter.WALL_KEY_REF_PARAM: return new Types.WallLocationLine((ARDB.WallLocationLine) integer);
                case ARDB.BuiltInParameter.FUNCTION_PARAM: return new Types.WallFunction((ARDB.WallFunction) integer);
                case ARDB.BuiltInParameter.VIEW_DETAIL_LEVEL: return new Types.ViewDetailLevel((ARDB.ViewDetailLevel) integer);
                case ARDB.BuiltInParameter.VIEW_DISCIPLINE: return new Types.ViewDiscipline((ARDB.ViewDiscipline) integer);
                case ARDB.BuiltInParameter.HOST_SSE_CURVED_EDGE_CONDITION_PARAM: return new Types.SlabShapeEditCurvedEdgeCondition((ERDB.SlabShapeEditCurvedEdgeCondition) integer);
              }

              var builtInIntegerName = builtInInteger.ToString();
              if (builtInIntegerName.Contains("COLOR_") || builtInIntegerName.Contains("_COLOR_") || builtInIntegerName.Contains("_COLOR"))
              {
                int r = integer % 256;
                integer /= 256;
                int g = integer % 256;
                integer /= 256;
                int b = integer % 256;

                return new GH_Colour(System.Drawing.Color.FromArgb(r, g, b));
              }
            }
          }

          return new GH_Integer(integer);

        case ARDB.StorageType.Double:
          var value = parameter.AsDouble();

          if (ERDB.Schemas.SpecType.IsMeasurableSpec(parameter.Definition.GetDataType(), out var spec))
            value = UnitConvertible.InRhinoUnits(value, spec);

          return new GH_Number(value);

        case ARDB.StorageType.String:
          return new GH_String(parameter.AsString());

        case ARDB.StorageType.ElementId:

          var elementId = parameter.AsElementId();
          if (parameter.Id.TryGetBuiltInParameter(out var builtInElementId))
          {
            if (builtInElementId == ARDB.BuiltInParameter.ID_PARAM || builtInElementId == ARDB.BuiltInParameter.SYMBOL_ID_PARAM)
              return new GH_Integer(elementId.IntegerValue);
          }

          return Types.Element.FromElementId(parameter.Element?.Document, parameter.AsElementId());

        default:
          throw new NotImplementedException();
      }
    }

    internal static bool Update(this ARDB.Parameter parameter, IGH_Goo value)
    {
      if (parameter is null)
        return default;

      switch (parameter.StorageType)
      {
        case ARDB.StorageType.Integer:

          if (parameter.Definition is ARDB.Definition definition)
          {
            if (definition.GetDataType() == ERDB.Schemas.SpecType.Boolean.YesNo)
            {
              if (!GH_Convert.ToBoolean(value, out var boolean, GH_Conversion.Both))
                throw new InvalidCastException();

              var _boolean = boolean ? 1 : 0;
              return parameter.Update(_boolean);
            }
            else if (parameter.Id.TryGetBuiltInParameter(out var builtInParameter))
            {
              var builtInParameterName = builtInParameter.ToString();
              if (builtInParameterName.Contains("COLOR_") || builtInParameterName.Contains("_COLOR_") || builtInParameterName.Contains("_COLOR"))
              {
                if (!GH_Convert.ToColor(value, out var color, GH_Conversion.Both))
                  throw new InvalidCastException();

                var _color = ((int) color.R) | ((int) color.G << 8) | ((int) color.B << 16);
                return parameter.Update(_color);
              }
            }
          }

          if (!GH_Convert.ToInt32(value, out var integer, GH_Conversion.Both))
            return false;

          return parameter.Update(integer);

        case ARDB.StorageType.Double:
          if (!GH_Convert.ToDouble(value, out var real, GH_Conversion.Both))
            return false;

          return parameter.Update
          (
            ERDB.Schemas.SpecType.IsMeasurableSpec(parameter.Definition.GetDataType(), out var spec) ?
            UnitConvertible.InHostUnits(real, spec) :
            real
          );

        case ARDB.StorageType.String:
          if (!GH_Convert.ToString(value, out var text, GH_Conversion.Both))
            throw new InvalidCastException();

          return parameter.Update(text);

        case ARDB.StorageType.ElementId:
          var element = new Types.Element();
          if (!element.CastFrom(value))
            throw new InvalidCastException();

          var elementId = element.Id;
          if (!elementId.IsBuiltInId() && !parameter.Element.Document.IsEquivalent(element.Document))
            throw new ArgumentException("Failed to assign an element from a diferent document.", parameter.Definition.Name);

          return parameter.Update(elementId);

        default:
          throw new NotImplementedException();
      }
    }

    internal static ARDB.Parameter GetParameter(IGH_ActiveObject obj, ARDB.Element element, IGH_Goo goo)
    {
      ARDB.Parameter parameter = null;
      object key = default;

      if (goo is Types.ParameterValue value)
        goo = new Types.ParameterKey(value.Document, value.Value.Id);

      if (goo is Types.ParameterId id) parameter = element.GetParameter(id.Value);
      else if (goo is Types.ParameterKey parameterKey)
      {
        if (parameterKey.Document.IsEquivalent(element.Document)) key = parameterKey.Id;
        else
        {
          parameter = parameterKey.GetParameter(element);

          if (parameter is null)
            obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterKey.DisplayName}' is not defined in 'Element'. {{{element.Id.IntegerValue}}}");

          return parameter;
        }
      }

      if (parameter is null)
      {
        switch (key ?? goo.ScriptVariable())
        {
          case string parameterName:
            {
              parameter = element.GetParameter(parameterName, ERDB.ParameterClass.Any);
              if (parameter is null) obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterName}' is not defined in 'Element'. {{{element.Id.IntegerValue}}}");
              break;
            }
          case int parameterId:
            {
              var elementId = new ARDB.ElementId(parameterId);
              if (elementId.TryGetBuiltInParameter(out var builtInParameter))
              {
                parameter = element.get_Parameter(builtInParameter);
                if (parameter is null) obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{ARDB.LabelUtils.GetLabelFor(builtInParameter)}' is not defined in 'Element' {{{element.Id.IntegerValue}}}");
              }
              else if (element.Document.GetElement(new ARDB.ElementId(parameterId)) is ARDB.ParameterElement parameterElement)
              {
                parameter = element.get_Parameter(parameterElement.GetDefinition());
                if (parameter is null) obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterElement.Name}' is not defined in 'Element'. {{{element.Id.IntegerValue}}}");
              }
              else obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Data conversion failed from {goo.TypeName} to Revit Parameter element");
              break;
            }
          case ARDB.Parameter param:
            {
              parameter = element.get_Parameter(param.Definition);
              if (parameter is null) obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{param.Definition.Name}' is not defined in 'Element'. {{{element.Id.IntegerValue}}}");
              break;
            }
          case ARDB.ElementId elementId:
            {
              if (elementId.TryGetBuiltInParameter(out var builtInParameter))
              {
                parameter = element.get_Parameter(builtInParameter);
                if (parameter is null) obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{ARDB.LabelUtils.GetLabelFor(builtInParameter)}' is not  defined in 'Element' {{{element.Id.IntegerValue}}}");
              }
              else if (element.Document.GetElement(elementId) is ARDB.ParameterElement parameterElement)
              {
                parameter = element.get_Parameter(parameterElement.GetDefinition());
                if (parameter is null) obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterElement.Name}' is not defined in 'Element'. {{{element.Id.IntegerValue}}}");
              }
              else obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Data conversion failed from {goo.TypeName} to Revit Parameter element.");
              break;
            }
          case Guid guid:
            {
              parameter = element.get_Parameter(guid);
              if (parameter is null) obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{guid}' is not defined in 'Element'. {{{element.Id.IntegerValue}}}");
              break;
            }
          default:
            {
              obj.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Data conversion failed from {goo.TypeName} to Revit Parameter element.");
              break;
            }
        }
      }

      return parameter;
    }

    public static IConvertible ToConvertible(this ARDB.Parameter parameter)
    {
      switch (parameter.StorageType)
      {
        case ARDB.StorageType.Integer:
          var integer = parameter.AsInteger();

          if (parameter.Definition is ARDB.Definition definition)
          {
            var dataType = definition.GetDataType();

            if (dataType == ERDB.Schemas.SpecType.Boolean.YesNo)
              return integer != 0;

            if (parameter.Id.TryGetBuiltInParameter(out var builtInInteger))
            {
              var builtInIntegerName = builtInInteger.ToString();
              if (builtInIntegerName.Contains("COLOR_") || builtInIntegerName.Contains("_COLOR_") || builtInIntegerName.Contains("_COLOR"))
              {
                int r = integer % 256;
                integer /= 256;
                int g = integer % 256;
                integer /= 256;
                int b = integer % 256;

                return System.Drawing.Color.FromArgb(r, g, b).ToArgb();
              }
            }
          }

          return integer;

        case ARDB.StorageType.Double:
          var value = parameter.AsDouble();
          return ERDB.Schemas.SpecType.IsMeasurableSpec(parameter.Definition.GetDataType(), out var spec) ?
            UnitConvertible.InRhinoUnits(value, spec) :
            value;

        case ARDB.StorageType.String:
          return parameter.AsString();

        case ARDB.StorageType.ElementId:

          var document = parameter.Element.Document;
          var documentGUID = document.GetFingerprintGUID();
          var elementId = parameter.AsElementId();

          return elementId.IsBuiltInId() ?
            ERDB.FullUniqueId.Format(documentGUID, ERDB.UniqueId.Format(ARDB.ExportUtils.GetGBXMLDocumentId(document), elementId.IntegerValue)) :
            document?.GetElement(elementId) is ARDB.Element element ?
            ERDB.FullUniqueId.Format(documentGUID, element.UniqueId) :
            ERDB.FullUniqueId.Format(Guid.Empty, ERDB.UniqueId.Format(Guid.Empty, ARDB.ElementId.InvalidElementId.IntegerValue));

        default:
          throw new NotImplementedException();
      }
    }
  }
}

namespace RhinoInside.Revit.GH.Components.ElementParameters
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public class ElementParameter : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("F568D3E7-BE3F-455B-A8D4-EBFA573D55C2");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "P";

    static readonly string[] keywords = new string[] { "Get", "Set" };
    public override IEnumerable<string> Keywords => base.Keywords is null ? keywords : Enumerable.Concat(base.Keywords, keywords);

    public ElementParameter() : base
    (
      name: "Element Parameter",
      nickname: "Param",
      description: "Get-Set accessor to element parameter values",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "An Element to update",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P",
          Description = "Element parameter to modify",
        }
      ),
      new ParamDefinition
      (
        new Param_GenericObject()
        {
          Name = "Value",
          NickName = "V",
          Description = "Element parameter value",
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
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "The Element",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P",
          Description = "Element parameter",
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_GenericObject()
        {
          Name = "Value",
          NickName = "V",
          Description = "Element parameter value",
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      if (!Params.TryGetData(DA, "Parameter", out Types.ParameterKey key, x => x.IsValid)) return;

      var parameter = key.GetParameter(element.Value);
      if (parameter is null)
      {
        var message = $"Parameter '{key.DisplayName}' is not defined on 'Element'. {{{element.Id.IntegerValue}}}";
        throw new Exceptions.RuntimeArgumentException("Parameter", message);
      }
      else Params.TrySetData(DA, "Parameter", () => new Types.ParameterKey(element.Document, parameter.Definition as ARDB.InternalDefinition));

      if (Params.GetData(DA, "Value", out IGH_Goo value, x => x.IsValid))
      {
        StartTransaction(element.Document);

        if (parameter.IsReadOnly || !parameter.Update(value))
        {
          var message = parameter.IsReadOnly ?
            $"Can't set parameter. '{parameter.Definition.Name}' is read-only.":
            $"Invalid value. Failed to set value '{value}' to parameter '{parameter.Definition.Name}'.";

          var dataTypeId = parameter.Definition?.GetDataType();
          if
          (
            !parameter.IsReadOnly &&
            ERDB.Schemas.SpecType.IsMeasurableSpec(dataTypeId, out var specTypeId) &&
            GH_Convert.ToDouble(value, out var number, GH_Conversion.Both)
          )
          {
            var unit_symbol = string.Empty;
            if (specTypeId == ERDB.Schemas.SpecType.Measurable.Angle) unit_symbol = " ㎭";
            else if (specTypeId == ERDB.Schemas.SpecType.Measurable.Length) unit_symbol = $" {GH_Format.RhinoUnitSymbol()}";
            else if (specTypeId == ERDB.Schemas.SpecType.Measurable.Area) unit_symbol = $" {GH_Format.RhinoUnitSymbol()}²";
            else if (specTypeId == ERDB.Schemas.SpecType.Measurable.Volume) unit_symbol = $" {GH_Format.RhinoUnitSymbol()}³";

            using (var formatOptions = new ARDB.FormatValueOptions() { AppendUnitSymbol = true })
            {
              var host = UnitConvertible.InHostUnits(number, specTypeId);
#if REVIT_2021
              var formated = ARDB.UnitFormatUtils.Format(element.Document.GetUnits(), specTypeId, host, forEditing: false, formatOptions);
#else
              var formated = ARDB.UnitFormatUtils.Format(element.Document.GetUnits(), specTypeId, host, maxAccuracy: false, forEditing: false, formatOptions);
#endif
              message = $"Cannot to set value '{value}{unit_symbol}' to parameter '{parameter.Definition.Name}'. This value would be {formated} in Revit.";
            }
          }

          message += $" {{{element.Id.IntegerValue}}}";

          if (FailureProcessingMode >= ARDB.FailureProcessingResult.ProceedWithRollBack)
          {
            using (var failure = new ARDB.FailureMessage(parameter.IsReadOnly ? ARDB.BuiltInFailures.GeneralFailures.CannotSetParameter : ARDB.BuiltInFailures.GeneralFailures.InvalidValue))
              element.Document.PostFailure(failure.SetFailingElement(element.Id));
          }

          throw new Exceptions.RuntimeArgumentException("Parameter", message);
        }

        element.InvalidateGraphics();
      }

      Params.TrySetData(DA, "Value", () => parameter.AsGoo());
    }
  }

  public class ElementParameterReset : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("2C374E6D-A547-45AC-B77D-04DD61317622");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    public ElementParameterReset() : base
    (
      name: "Reset Element Parameter",
      nickname: "Reset",
      description: "Resets the parameter value of a specified Revit Element",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to update",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P",
          Description = "Element parameter to reset",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Updated Element",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      if (!Params.GetData(DA, "Parameter", out Types.ParameterKey key)) return;

      var parameter = key.GetParameter(element.Value);
      if (parameter is null)
      {
        var message = $"Parameter '{key.DisplayName}' is not defined in 'Element'. {{{element.Id.IntegerValue}}}";
        if (FailureProcessingMode == ARDB.FailureProcessingResult.Continue)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, message);
        }
        else if (FailureProcessingMode == ARDB.FailureProcessingResult.ProceedWithCommit)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
          return;
        }
        else throw new Exceptions.RuntimeArgumentException("Parameter", message);
      }
      else
      {
        StartTransaction(element.Document);

        if (!parameter.ResetValue())
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Unable to reset parameter '{parameter.Definition.Name}'.");
      }

      DA.SetData("Element", element);
    }
  }

  public class QueryElementParameters : Component
  {
    public override Guid ComponentGuid => new Guid("44515A6B-84EE-4DBD-8241-17EDBE07C5B6");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public QueryElementParameters()
    : base("Query Element Parameters", "Parameters", "Get the parameters of the specified Element", "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query", GH_ParamAccess.item);
      manager[manager.AddTextParameter("Name", "N", "Filter params by Name", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.ParameterGroup>(), "Group", "G", "Filter params by the group they belong", GH_ParamAccess.item)].Optional = true;
      manager[manager.AddBooleanParameter("ReadOnly", "R", "Filter params by its ReadOnly property", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "Parameters", "P", "Element parameters", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;

      var filterName = Params.GetData(DA, "Name", out string parameterName);
      var filterGroup = Params.GetData(DA, "Group", out ERDB.Schemas.ParameterGroup parameterGroup);
      var filterReadOnly = Params.GetData(DA, "ReadOnly", out bool? readOnly);

      var parameters = new List<ARDB.Parameter>(element.Value.Parameters.Size);
      foreach (var group in element.Value.GetParameters(ERDB.ParameterClass.Any).GroupBy(x => x.Definition?.GetGroupType() ?? ERDB.Schemas.ParameterGroup.Empty).OrderBy(x => x.Key))
      {
        foreach (var param in group.OrderBy(x => x.Id.IntegerValue))
        {
          if (string.IsNullOrEmpty(param.Definition.Name))
            continue;

          if (filterName && !param.Definition.Name.IsSymbolNameLike(parameterName))
            continue;

          if (filterGroup && parameterGroup != (param.Definition?.GetGroupType() ?? ERDB.Schemas.ParameterGroup.Empty))
            continue;

          if (filterReadOnly && readOnly != param.IsReadOnly)
            continue;

          parameters.Add(param);
        }
      }

      DA.SetDataList("Parameters", parameters);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.ElementParameters.Obsolete
{
  using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
  using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

  [EditorBrowsable(EditorBrowsableState.Never)]
  public class ElementParameterGetUpgrader : ComponentUpgrader
  {
    public ElementParameterGetUpgrader() { }
    public override DateTime Version => new DateTime(2021, 07, 30);
    public override Guid UpgradeFrom => new Guid("D86050F2-C774-49B1-9973-FB3AB188DC94");
    public override Guid UpgradeTo => new Guid("F568D3E7-BE3F-455B-A8D4-EBFA573D55C2");

    public override IReadOnlyDictionary<string, string> GetInputAliases(IGH_Component _) =>
      new Dictionary<string, string>()
      {
        {"ParameterKey", "Parameter"}
      };

    public override IReadOnlyDictionary<string, string> GetOutputAliases(IGH_Component _) =>
      new Dictionary<string, string>()
      {
        {"ParameterValue", "Value"}
      };
  }

  [Obsolete("Obsolete since 2021-07-30")]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public class ElementParameterGet : Component
  {
    public override Guid ComponentGuid => new Guid("D86050F2-C774-49B1-9973-FB3AB188DC94");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.hidden;

    public ElementParameterGet() : base
    (
      name: "Get Element Parameter",
      nickname: "Get",
      description: "Gets the parameter value of a specified Revit Element",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query", GH_ParamAccess.item);
      manager.AddGenericParameter("ParameterKey", "K", "Element parameter to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterValue(), "ParameterValue", "V", "Element parameter value", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      if (!Params.GetData(DA, "ParameterKey", out IGH_Goo key)) return;

      if (element.Value is object)
      {
        var parameter = ParameterUtils.GetParameter(this, element.Value, key);
        DA.SetData("ParameterValue", parameter);
      }
    }
  }

  [EditorBrowsable(EditorBrowsableState.Never)]
  public class ElementParameterSetUpgrader : ComponentUpgrader
  {
    public ElementParameterSetUpgrader() { }
    public override DateTime Version => new DateTime(2021, 07, 30);
    public override Guid UpgradeFrom => new Guid("8F1EE110-7FDA-49E0-BED4-E8E0227BC021");
    public override Guid UpgradeTo => new Guid("F568D3E7-BE3F-455B-A8D4-EBFA573D55C2");

    public override IReadOnlyDictionary<string, string> GetInputAliases(IGH_Component _) =>
      new Dictionary<string, string>()
      {
        {"ParameterKey", "Parameter"},
        {"ParameterValue", "Value"}
      };
  }

  [Obsolete("Obsolete since 2021-07-30")]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public class ElementParameterSet : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("8F1EE110-7FDA-49E0-BED4-E8E0227BC021");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.hidden;

    public ElementParameterSet() : base
    (
      name: "Set Element Parameter",
      nickname: "Set",
      description: "Sets the parameter value of a specified Revit Element",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
    new ParamDefinition
    (
      new Parameters.Element()
      {
        Name = "Element",
        NickName = "E",
        Description = "Element to update",
      }
    ),
    new ParamDefinition
    (
      new Param_GenericObject()
      {
        Name = "ParameterKey",
        NickName = "K",
        Description = "Element parameter to modify",
      }
    ),
    new ParamDefinition
    (
      new Param_GenericObject()
      {
        Name = "ParameterValue",
        NickName = "V",
        Description = "Element parameter value",
      }
    ),
  };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
    new ParamDefinition
    (
      new Parameters.Element()
      {
        Name = "Element",
        NickName = "E",
        Description = "Updated Element",
      }
    ),
  };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      if (!Params.GetData(DA, "ParameterKey", out IGH_Goo key)) return;
      if (!Params.GetData(DA, "ParameterValue", out IGH_Goo value)) return;

      var parameter = ParameterUtils.GetParameter(this, element.Value, key);
      if (parameter is null)
        return;

      StartTransaction(element.Document);

      if (parameter.Update(value))
        DA.SetData("Element", element);
      else
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unable to set parameter '{parameter.Definition.Name}' : '{value}' is not a valid value.");
    }
  }
}
