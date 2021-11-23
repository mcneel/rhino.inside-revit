using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using EDBS = RhinoInside.Revit.External.DB.Schemas;

namespace RhinoInside.Revit.GH.Components.Filters
{
  public abstract class ElementFilterRule : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override bool IsPreviewCapable => false;

    protected ElementFilterRule(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "Parameter", "P", "Parameter to check", GH_ParamAccess.item);
      manager.AddGenericParameter("Value", "V", "Value to check with", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rule", "R", string.Empty, GH_ParamAccess.item);
    }

    static readonly Dictionary<ARDB.BuiltInParameter, EDBS.DataType> BuiltInParametersTypes = new Dictionary<ARDB.BuiltInParameter, EDBS.DataType>();

    internal static bool TryGetParameterDefinition(ARDB.Document doc, ARDB.ElementId id, out ARDB.StorageType storageType, out EDBS.DataType dataType)
    {
      if (id.TryGetBuiltInParameter(out var builtInParameter))
      {
        storageType = doc.get_TypeOfStorage(builtInParameter);

        if (storageType == ARDB.StorageType.ElementId)
        {
          dataType = EDBS.SpecType.Int.Integer;
          return true;
        }

        if (storageType == ARDB.StorageType.Double)
        {
          if (BuiltInParametersTypes.TryGetValue(builtInParameter, out dataType))
            return true;

          var categoriesWhereDefined = doc.GetBuiltInCategoriesWithParameters().
            Select(bic => new ARDB.ElementId(bic)).
            Where(cid => ARDB.TableView.GetAvailableParameters(doc, cid).Contains(id)).
            ToArray();

          using (var collector = new ARDB.FilteredElementCollector(doc))
          {
            using
            (
              var filteredCollector = categoriesWhereDefined.Length == 0 ?
              collector.WherePasses(new ARDB.ElementClassFilter(typeof(ARDB.ParameterElement), false)) :
              categoriesWhereDefined.Length > 1 ?
                collector.WherePasses(new ARDB.ElementMulticategoryFilter(categoriesWhereDefined)) :
                collector.WherePasses(new ARDB.ElementCategoryFilter(categoriesWhereDefined[0]))
            )
            {
              foreach (var element in filteredCollector)
              {
                var parameter = element.get_Parameter(builtInParameter);
                if (parameter is null)
                  continue;

                dataType = parameter.Definition.GetDataType();
                BuiltInParametersTypes.Add(builtInParameter, dataType);
                return true;
              }
            }
          }

          dataType = EDBS.DataType.Empty;
          return false;
        }

        dataType = EDBS.DataType.Empty;
        return true;
      }
      else
      {
        try
        {
          if (doc?.GetElement(id) is ARDB.ParameterElement parameter)
          {
            dataType = parameter.GetDefinition().GetDataType();
            storageType = dataType.ToStorageType();
            return true;
          }
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
      }

      storageType = ARDB.StorageType.None;
      dataType = EDBS.SpecType.Empty;
      return false;
    }

    protected enum ConditionType
    {
      NotEquals,
      Equals,
      Greater,
      GreaterOrEqual,
      Less,
      LessOrEqual
    }

    protected abstract ConditionType Condition { get; }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var parameterKey = default(Types.ParameterKey);
      if (!DA.GetData("Parameter", ref parameterKey))
        return;

      if (!parameterKey.IsReferencedData)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Conversion from '{parameterKey.Name}' to Parameter may be ambiguous. Please use 'BuiltInParameter Picker' or a 'Parameter' Param");

      if (!TryGetParameterDefinition(parameterKey.Document, parameterKey.Id, out var storageType, out var dataType))
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{ARDB.LabelUtils.GetLabelFor(builtInParameter)}' in Revit document.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{parameterKey.Name}' in Revit document.");

        return;
      }

      // Do a guess based on the Value content
      if (storageType == ARDB.StorageType.None)
      {
        var goo = default(IGH_Goo);
        if (!DA.GetData("Value", ref goo)) return;

        switch (goo)
        {
          case GH_Boolean _:          storageType = ARDB.StorageType.Integer; break;
          case GH_Integer _:          storageType = ARDB.StorageType.Integer; break;
          case GH_Number _:           storageType = ARDB.StorageType.Double; break;
          case GH_String _:           storageType = ARDB.StorageType.String; break;
          case Types.IGH_ElementId _: storageType = ARDB.StorageType.ElementId; break;
          default:
            switch (goo.ScriptVariable())
            {
              case bool _: storageType = ARDB.StorageType.Integer; break;
              case int _: storageType = ARDB.StorageType.Integer; break;
              case double _: storageType = ARDB.StorageType.Double; break;
              case string _: storageType = ARDB.StorageType.String; break;
              case ARDB.Element _: storageType = ARDB.StorageType.ElementId; break;
            }
            break;
        }
      }

      var provider = new ARDB.ParameterValueProvider(parameterKey.Id);

      ARDB.FilterRule rule = null;
      if (storageType == ARDB.StorageType.String)
      {
        ARDB.FilterStringRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.NotEquals:
          case ConditionType.Equals:          ruleEvaluator = new ARDB.FilterStringEquals();          break;
          case ConditionType.Greater:         ruleEvaluator = new ARDB.FilterStringGreater();         break;
          case ConditionType.GreaterOrEqual:  ruleEvaluator = new ARDB.FilterStringGreaterOrEqual();  break;
          case ConditionType.Less:            ruleEvaluator = new ARDB.FilterStringLess();            break;
          case ConditionType.LessOrEqual:     ruleEvaluator = new ARDB.FilterStringLessOrEqual();     break;
        }

        var goo = default(GH_String);
        if (DA.GetData("Value", ref goo) && goo.Value is string value)
          rule = new ARDB.FilterStringRule(provider, ruleEvaluator, value, true);
      }
      else
      {
        ARDB.FilterNumericRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.NotEquals:
          case ConditionType.Equals:          ruleEvaluator = new ARDB.FilterNumericEquals();         break;
          case ConditionType.Greater:         ruleEvaluator = new ARDB.FilterNumericGreater();        break;
          case ConditionType.GreaterOrEqual:  ruleEvaluator = new ARDB.FilterNumericGreaterOrEqual(); break;
          case ConditionType.Less:            ruleEvaluator = new ARDB.FilterNumericLess();           break;
          case ConditionType.LessOrEqual:     ruleEvaluator = new ARDB.FilterNumericLessOrEqual();    break;
        }

        switch (storageType)
        {
          case ARDB.StorageType.Integer:
          {
            var goo = default(GH_Integer);
            if (DA.GetData("Value", ref goo))
              rule = new ARDB.FilterIntegerRule(provider, ruleEvaluator, goo.Value);
          }
          break;

          case ARDB.StorageType.Double:
          {
            var goo = default(GH_Number);
            if (DA.GetData("Value", ref goo))
            {
              var value = goo.Value;
              var tol = 0.0;

              // If is a Measurable it may need to be scaled.
              if (EDBS.SpecType.IsMeasurableSpec(dataType, out var spec))
              {
                // Adjust value acording to data-type dimensionality
                if (spec.TryGetLengthDimensionality(out var dimensionality))
                  value = UnitConverter.Convert
                  (
                    value,
                    UnitConverter.ExternalUnitSystem,
                    UnitConverter.InternalUnitSystem,
                    dimensionality
                  );
                else
                  dimensionality = 0;

                // Adjust tolerance acording to data-type dimensionality
                if (Condition == ConditionType.Equals || Condition == ConditionType.NotEquals)
                {
                  tol = dimensionality == 0 ?
                    1e-6 :
                    UnitConverter.Convert
                    (
                      Revit.VertexTolerance,
                      UnitConverter.ExternalUnitSystem,
                      UnitConverter.InternalUnitSystem,
                      Math.Abs(dimensionality)
                    );
                }
              }

              rule = new ARDB.FilterDoubleRule(provider, ruleEvaluator, value, tol);
            }
          }
          break;

          case ARDB.StorageType.ElementId:
          {
            var value = default(ARDB.ElementId);
            if (DA.GetData("Value", ref value))
              rule = new ARDB.FilterElementIdRule(provider, ruleEvaluator, value);
          }
          break;
        }
      }

      if (rule is object)
      {
        if (Condition == ConditionType.NotEquals)
          DA.SetData("Rule", new ARDB.FilterInverseRule(rule));
        else
          DA.SetData("Rule", rule);
      }
    }
  }

  public class ElementFilterRuleNotEquals : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("6BBE9731-EF71-42E8-A880-1D2ADFEB9F79");
    protected override string IconTag => "≠";
    protected override ConditionType Condition => ConditionType.NotEquals;

    public ElementFilterRuleNotEquals()
    : base("Not Equals Rule", "NotEquals", "Filter used to match elements if value of a parameter are not equals to Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleEquals : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("0F9139AC-2A21-474C-9C5B-6864B2F2313C");
    protected override string IconTag => "=";
    protected override ConditionType Condition => ConditionType.Equals;

    public ElementFilterRuleEquals()
    : base("Equals Rule", "Equals", "Filter used to match elements if value of a parameter equals to Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleGreater : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("BB7D39DA-97AD-4277-82C7-010AF857FF03");
    protected override string IconTag => ">";
    protected override ConditionType Condition => ConditionType.Greater;

    public ElementFilterRuleGreater()
    : base("Greater Rule", "Greater", "Filter used to match elements if value of a parameter greater than Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleGreaterOrEqual : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("05BBAEDD-027B-40DA-8390-F826B63FD100");
    protected override string IconTag => "≥";
    protected override ConditionType Condition => ConditionType.GreaterOrEqual;

    public ElementFilterRuleGreaterOrEqual()
    : base("Greater Or Equal Rule", "GrtOrEqu", "Filter used to match elements if value of a parameter greater or equal than Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleLess : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("BE2C5AFE-7D56-4F63-9A23-20560E3675B9");
    protected override string IconTag => "<";
    protected override ConditionType Condition => ConditionType.Less;

    public ElementFilterRuleLess()
    : base("Less Rule", "Less", "Filter used to match elements if value of a parameter less than Value", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleLessOrEqual : ElementFilterRule
  {
    public override Guid ComponentGuid => new Guid("BB69852F-6A39-4ADC-B9B8-D16A8862B4C7");
    protected override string IconTag => "≤";
    protected override ConditionType Condition => ConditionType.LessOrEqual;

    public ElementFilterRuleLessOrEqual()
    : base("Less Or Equal Rule", "LessOrEqu", "Filter used to match elements if value of a parameter less or equal than Value", "Revit", "Filter")
    { }
  }

  public abstract class ElementFilterStringRule : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override bool IsPreviewCapable => false;

    protected enum ConditionType
    {
      Contains,
      BeginsWith,
      EndsWith,
    }

    protected abstract ConditionType Condition { get; }

    protected ElementFilterStringRule(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.ParameterKey(), "Parameter", "P", "Parameter to check", GH_ParamAccess.item);
      manager.AddTextParameter("Value", "V", "Value to check with", GH_ParamAccess.item);
      manager.AddBooleanParameter("Inverted", "I", "True if the results of the rule should be inverted", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rule", "R", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var parameterKey = default(Types.ParameterKey);
      if (!DA.GetData("Parameter", ref parameterKey))
        return;

      if (!parameterKey.IsReferencedData)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Conversion from '{parameterKey.Name}' to Parameter may be ambiguous. Please use 'BuiltInParameter Picker' or a 'Parameter' Param");

      var inverted = false;
      if (!DA.GetData("Inverted", ref inverted))
        return;

      if (!ElementFilterRule.TryGetParameterDefinition(parameterKey.Document, parameterKey.Id, out var storageType, out var dataType))
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{ARDB.LabelUtils.GetLabelFor(builtInParameter)}' in Revit document.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{parameterKey.Name}' in Revit document.");

        return;
      }

      if (storageType != ARDB.StorageType.String)
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{ARDB.LabelUtils.GetLabelFor(builtInParameter)}' is not a text parameter.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter '{parameterKey.Id.IntegerValue}' is not a text parameter.");

        return;
      }

      var provider = new ARDB.ParameterValueProvider(parameterKey.Id);

      ARDB.FilterRule rule = null;
      if (storageType == ARDB.StorageType.String)
      {
        ARDB.FilterStringRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.Contains: ruleEvaluator = new ARDB.FilterStringContains(); break;
          case ConditionType.BeginsWith: ruleEvaluator = new ARDB.FilterStringBeginsWith(); break;
          case ConditionType.EndsWith: ruleEvaluator = new ARDB.FilterStringEndsWith(); break;
        }

        var goo = default(GH_String);
        if (DA.GetData("Value", ref goo))
          rule = new ARDB.FilterStringRule(provider, ruleEvaluator, goo.Value, true);
      }

      if (rule is object)
      {
        if (inverted)
          DA.SetData("Rule", new ARDB.FilterInverseRule(rule));
        else
          DA.SetData("Rule", rule);
      }
    }
  }

  public class ElementFilterRuleContains : ElementFilterStringRule
  {
    public override Guid ComponentGuid => new Guid("B1265CF6-3031-4E05-B958-38D00C5A41EF");
    protected override string IconTag => "?";
    protected override ConditionType Condition => ConditionType.Contains;

    public ElementFilterRuleContains()
    : base("Text Contains Rule", "Contains", "Filter used to match elements if value of a parameter contains the specified text", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleBeginsWith : ElementFilterStringRule
  {
    public override Guid ComponentGuid => new Guid("7FA73840-6511-49BD-A4C9-85F0DFD907E5");
    protected override string IconTag => "<";
    protected override ConditionType Condition => ConditionType.BeginsWith;

    public ElementFilterRuleBeginsWith()
    : base("Text Begins Rule", "Begins", "Filter used to match elements if value of a parameter begins with the specified text", "Revit", "Filter")
    { }
  }

  public class ElementFilterRuleEndsWith : ElementFilterStringRule
  {
    public override Guid ComponentGuid => new Guid("84F29564-1ACD-4148-B00F-EA3FCFB6DF13");
    protected override string IconTag => ">";
    protected override ConditionType Condition => ConditionType.EndsWith;

    public ElementFilterRuleEndsWith()
    : base("Text Ends Rule", "Ends", "Filter used to match elements if value of a parameter ends with the specified text", "Revit", "Filter")
    { }
  }

  public class CategoryFilterRule : Component
  {
    public override Guid ComponentGuid => new Guid("0CE4F51D-49D0-4B0C-82C8-84CCCF0968F6");

    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;
    public override bool IsPreviewCapable => false;

    public CategoryFilterRule()
    : base("Category Rule", "Category", "Filter used to match elements on a category", "Revit", "Filter")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Categories", "C", "Categories to check", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.FilterRule(), "Rule", "R", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var categories = new List<Types.Category>();
      if (!DA.GetDataList("Categories", categories) || categories.Count == 0)
        return;

      var categoryIds = categories.Select(x => x.Id).ToList();
      var rule = new ARDB.FilterCategoryRule(categoryIds);

      DA.SetData("Rule", rule);
    }
  }
}
