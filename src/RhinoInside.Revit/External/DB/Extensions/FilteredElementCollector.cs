using System;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class FilteredElementCollectorExtension
  {
    public static FilteredElementCollector WhereElementIsKindOf(this FilteredElementCollector collector, Type type)
    {
      if (type == typeof(Element))
        return collector.WhereElementIsNotElementType();
      
      return (typeof(ElementType).IsAssignableFrom(type) ? collector.WhereElementIsElementType() : collector).
        WherePasses(ElementExtension.CreateElementClassFilter(type));
    }

    public static FilteredElementCollector WhereCategoryIdEqualsTo(this FilteredElementCollector collector, ElementId value)
    {
      return value is object ? collector.WherePasses(new ElementCategoryFilter(value)) : collector;
    }

    public static FilteredElementCollector WhereCategoryIdEqualsTo(this FilteredElementCollector collector, BuiltInCategory? value)
    {
      return value is object ? collector.WherePasses(new ElementCategoryFilter(value.Value)) : collector;
    }

    public static FilteredElementCollector WhereTypeIdEqualsTo(this FilteredElementCollector collector, ElementId value)
    {
      if (value is null) return collector;

      using (var provider = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM)))
      using (var evaluator = new FilterNumericEquals())
      using (var rule = new FilterElementIdRule(provider, evaluator, value))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }

    public static FilteredElementCollector WhereParameterEqualsTo(this FilteredElementCollector collector, BuiltInParameter paramId, int value)
    {
      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterNumericEquals())
      using (var rule = new FilterIntegerRule(provider, evaluator, value))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }

    public static FilteredElementCollector WhereParameterEqualsTo(this FilteredElementCollector collector, BuiltInParameter paramId, string value, bool caseSensitive = true)
    {
      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterStringEquals())
      using (var rule = new FilterStringRule(provider, evaluator, value ?? "", caseSensitive))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }

    public static FilteredElementCollector WhereParameterEqualsTo(this FilteredElementCollector collector, BuiltInParameter paramId, ElementId value)
    {
      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterNumericEquals())
      using (var rule = new FilterElementIdRule(provider, evaluator, value))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }

    public static FilteredElementCollector WhereParameterBeginsWith(this FilteredElementCollector collector, BuiltInParameter paramId, string value, bool caseSensitive = true)
    {
      if (string.IsNullOrEmpty(value))
        return collector.WhereParameterEqualsTo(paramId, value, caseSensitive);

      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterStringBeginsWith())
      using (var rule = new FilterStringRule(provider, evaluator, value, caseSensitive))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }
  }
}
