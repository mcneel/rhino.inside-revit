using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class FilteredElementCollectorExtension
  {
    public static FilteredElementCollector WhereElementIsKindOf(this FilteredElementCollector collector, Type type)
    {
      if (type == typeof(Element))
        return collector;

      return collector.WherePasses(ElementExtension.CreateElementClassFilter(type));
    }

    public static FilteredElementCollector WhereCategoryIdEqualsTo(this FilteredElementCollector collector, ElementId value)
    {
      return collector.WherePasses(new ElementCategoryFilter(value));
    }

    public static FilteredElementCollector WhereTypeIdEqualsTo(this FilteredElementCollector collector, ElementId value)
    {
      using (var provider = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM)))
      using (var evaluator = new FilterNumericEquals())
      using (var rule = new FilterElementIdRule(provider, evaluator, value))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }

    public static FilteredElementCollector WhereParameterEqualsTo(this FilteredElementCollector collector, BuiltInParameter paramId, string value, bool caseSensitive = true)
    {
      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterStringEquals())
      using (var rule = new FilterStringRule(provider, evaluator, value, caseSensitive))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }
  }
}
