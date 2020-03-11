using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents.Filters
{
  public abstract class ElementFilterRule : Component
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    public override bool IsPreviewCapable => false;

    protected ElementFilterRule(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Params.ParameterKey(), "ParameterKey", "K", "Parameter to check", GH_ParamAccess.item);
      manager.AddGenericParameter("Value", "V", "Value to check with", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Documents.Filters.FilterRule(), "Rule", "R", string.Empty, GH_ParamAccess.item);
    }

    static double ToHost(double value, DB.ParameterType parameterType)
    {
      switch (parameterType)
      {
        case DB.ParameterType.Length: return value / Math.Pow(Revit.ModelUnits, 1.0);
        case DB.ParameterType.Area: return value / Math.Pow(Revit.ModelUnits, 2.0);
        case DB.ParameterType.Volume: return value / Math.Pow(Revit.ModelUnits, 3.0);
      }

      return value;
    }

    static readonly Dictionary<DB.BuiltInParameter, DB.ParameterType> BuiltInParametersTypes = new Dictionary<DB.BuiltInParameter, DB.ParameterType>();

    static bool TryGetParameterDefinition(DB.Document doc, DB.ElementId id, out DB.StorageType storageType, out DB.ParameterType parameterType)
    {
      if (id.TryGetBuiltInParameter(out var builtInParameter))
      {
        storageType = doc.get_TypeOfStorage(builtInParameter);

        if (storageType == DB.StorageType.ElementId)
        {
          if (builtInParameter == DB.BuiltInParameter.ELEM_TYPE_PARAM)
          {
            parameterType = DB.ParameterType.FamilyType;
            return true;
          }

          if (builtInParameter == DB.BuiltInParameter.ELEM_CATEGORY_PARAM || builtInParameter == DB.BuiltInParameter.ELEM_CATEGORY_PARAM_MT)
          {
            parameterType = (DB.ParameterType) int.MaxValue;
            return true;
          }
        }

        if (storageType == DB.StorageType.Double)
        {
          if (BuiltInParametersTypes.TryGetValue(builtInParameter, out parameterType))
            return true;

          var categoriesWhereDefined = doc.GetBuiltInCategoriesWithParameters().
            Select(bic => new DB.ElementId(bic)).
            Where(cid => DB.TableView.GetAvailableParameters(doc, cid).Contains(id)).
            ToArray();

          using (var collector = new DB.FilteredElementCollector(doc))
          {
            using
            (
              var filteredCollector = categoriesWhereDefined.Length == 0 ?
              collector.WherePasses(new DB.ElementClassFilter(typeof(DB.ParameterElement), false)) :
              categoriesWhereDefined.Length > 1 ?
                collector.WherePasses(new DB.ElementMulticategoryFilter(categoriesWhereDefined)) :
                collector.WherePasses(new DB.ElementCategoryFilter(categoriesWhereDefined[0]))
            )
            {
              foreach (var element in filteredCollector)
              {
                var parameter = element.get_Parameter(builtInParameter);
                if (parameter is null)
                  continue;

                parameterType = parameter.Definition.ParameterType;
                BuiltInParametersTypes.Add(builtInParameter, parameterType);
                return true;
              }
            }
          }

          parameterType = DB.ParameterType.Invalid;
          return false;
        }

        parameterType = DB.ParameterType.Invalid;
        return true;
      }
      else
      {
        try
        {
          if (doc.GetElement(id) is DB.ParameterElement parameter)
          {
            storageType = parameter.GetDefinition().ParameterType.ToStorageType();
            parameterType = parameter.GetDefinition().ParameterType;
            return true;
          }
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
      }

      storageType = DB.StorageType.None;
      parameterType = DB.ParameterType.Invalid;
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
      var parameterKey = default(Types.Documents.Params.ParameterKey);
      if (!DA.GetData("ParameterKey", ref parameterKey))
        return;

      DA.DisableGapLogic();

      if (!TryGetParameterDefinition(parameterKey.Document, parameterKey.Id, out var storageType, out var parameterType))
      {
        if (parameterKey.Id.TryGetBuiltInParameter(out var builtInParameter))
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{DB.LabelUtils.GetLabelFor(builtInParameter)}' in Revit document.");
        else
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to found parameter '{parameterKey.Id.IntegerValue}' in Revit document.");

        return;
      }

      var provider = new DB.ParameterValueProvider(parameterKey);

      DB.FilterRule rule = null;
      if (storageType == DB.StorageType.String)
      {
        DB.FilterStringRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.NotEquals:
          case ConditionType.Equals: ruleEvaluator = new DB.FilterStringEquals(); break;
          case ConditionType.Greater: ruleEvaluator = new DB.FilterStringGreater(); break;
          case ConditionType.GreaterOrEqual: ruleEvaluator = new DB.FilterStringGreaterOrEqual(); break;
          case ConditionType.Less: ruleEvaluator = new DB.FilterStringLess(); break;
          case ConditionType.LessOrEqual: ruleEvaluator = new DB.FilterStringLessOrEqual(); break;
        }

        var goo = default(GH_String);
        if (DA.GetData("Value", ref goo))
          rule = new DB.FilterStringRule(provider, ruleEvaluator, goo.Value, true);
      }
      else
      {
        DB.FilterNumericRuleEvaluator ruleEvaluator = null;
        switch (Condition)
        {
          case ConditionType.NotEquals:
          case ConditionType.Equals: ruleEvaluator = new DB.FilterNumericEquals(); break;
          case ConditionType.Greater: ruleEvaluator = new DB.FilterNumericGreater(); break;
          case ConditionType.GreaterOrEqual: ruleEvaluator = new DB.FilterNumericGreaterOrEqual(); break;
          case ConditionType.Less: ruleEvaluator = new DB.FilterNumericLess(); break;
          case ConditionType.LessOrEqual: ruleEvaluator = new DB.FilterNumericLessOrEqual(); break;
        }

        switch (storageType)
        {
          case DB.StorageType.Integer:
            {
              var goo = default(GH_Integer);
              if (DA.GetData("Value", ref goo))
                rule = new DB.FilterIntegerRule(provider, ruleEvaluator, goo.Value);
            }
            break;
          case DB.StorageType.Double:
            {
              var goo = default(GH_Number);
              if (DA.GetData("Value", ref goo))
              {
                if (Condition == ConditionType.Equals || Condition == ConditionType.NotEquals)
                {
                  if (parameterType == DB.ParameterType.Length || parameterType == DB.ParameterType.Area || parameterType == DB.ParameterType.Volume)
                    rule = new DB.FilterDoubleRule(provider, ruleEvaluator, ToHost(goo.Value, parameterType), ToHost(Revit.VertexTolerance, parameterType));
                  else
                    rule = new DB.FilterDoubleRule(provider, ruleEvaluator, ToHost(goo.Value, parameterType), 1e-6);
                }
                else
                  rule = new DB.FilterDoubleRule(provider, ruleEvaluator, ToHost(goo.Value, parameterType), 0.0);
              }
            }
            break;
          case DB.StorageType.ElementId:
            {
              switch (parameterType)
              {
                case (DB.ParameterType) int.MaxValue: // Category
                  {
                    var value = default(Types.Documents.Categories.Category);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
                case DB.ParameterType.Material:
                  {
                    var value = default(Types.Elements.Material.Material);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
                case DB.ParameterType.FamilyType:
                  {
                    var value = default(Types.Documents.ElementTypes.ElementType);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
                default:
                  {
                    var value = default(Types.Element);
                    if (DA.GetData("Value", ref value))
                      rule = new DB.FilterElementIdRule(provider, ruleEvaluator, value);
                  }
                  break;
              }
            }
            break;
        }
      }

      if (rule is object)
      {
        if (Condition == ConditionType.NotEquals)
          DA.SetData("Rule", new DB.FilterInverseRule(rule));
        else
          DA.SetData("Rule", rule);
      }
    }
  }
}
