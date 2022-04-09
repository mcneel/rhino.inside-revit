using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal struct ReadOnlySortedElementIdCollection : ICollection<ElementId>
  {
    readonly ICollection<ElementId> collection;
    public ReadOnlySortedElementIdCollection(ICollection<ElementId> source) => collection = source;

    public int Count => collection.Count;
    public bool IsReadOnly => true;

    public bool Contains(ElementId item)
    {
      if (collection is List<ElementId> list)
        return list.BinarySearch(item, ElementIdComparer.NoNullsAscending) >= 0;
      else
        return collection.Contains(item);
    }

    public void CopyTo(ElementId[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);

    public void Add(ElementId item) => throw new InvalidOperationException("Collection is read-only");
    public bool Remove(ElementId item) => throw new InvalidOperationException("Collection is read-only");
    public void Clear() => throw new InvalidOperationException("Collection is read-only");

    public IEnumerator<ElementId> GetEnumerator() => collection.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => collection.GetEnumerator();
  }

  public static class FilteredElementCollectorExtension
  {
    public static ICollection<ElementId> ToReadOnlyElementIdCollection(this FilteredElementCollector collector)
    {
      return new ReadOnlySortedElementIdCollection(collector.ToElementIds());
    }

    public static FilteredElementCollector WhereElementIsKindOf(this FilteredElementCollector collector, Type type)
    {
      return type == typeof(Element) ? collector : collector.WherePasses(CompoundElementFilter.ElementClassFilter(type));
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

    public static FilteredElementCollector WhereParameterEqualsTo(this FilteredElementCollector collector, BuiltInParameter paramId, string value)
    {
      if (value is null) return collector;

      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterStringEquals())
      using (var rule = CompoundElementFilter.FilterStringRule(provider, evaluator, value))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }

    public static FilteredElementCollector WhereParameterEqualsTo(this FilteredElementCollector collector, BuiltInParameter paramId, ElementId value)
    {
      if (value is null) return collector;

      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterNumericEquals())
      using (var rule = new FilterElementIdRule(provider, evaluator, value))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }

    public static FilteredElementCollector WhereParameterBeginsWith(this FilteredElementCollector collector, BuiltInParameter paramId, string value)
    {
      if (string.IsNullOrEmpty(value))
        return collector.WhereParameterEqualsTo(paramId, value);

      using (var provider = new ParameterValueProvider(new ElementId(paramId)))
      using (var evaluator = new FilterStringBeginsWith())
      using (var rule = CompoundElementFilter.FilterStringRule(provider, evaluator, value))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }
  }
}
