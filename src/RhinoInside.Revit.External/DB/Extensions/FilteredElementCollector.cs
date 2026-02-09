using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal readonly struct ReadOnlyElementIdSet : ISet<ElementId>
  {
    readonly ICollection<ElementId> Values;
    internal ReadOnlyElementIdSet(ICollection<ElementId> source)
    {
      Values = (source is ReadOnlyElementIdSet set) ? set.Values : source;
    }

    public static readonly ReadOnlyElementIdSet Empty = new ReadOnlyElementIdSet(Array.Empty<ElementId>());

    #region IEnumerable
    public IEnumerator<ElementId> GetEnumerator() => Values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Values.GetEnumerator();
    #endregion

    #region ICollection
    public int Count => Values.Count;
    bool ICollection<ElementId>.IsReadOnly => true;

    public bool Contains(ElementId item)
    {
      if (Values is List<ElementId> list)
        return list.BinarySearch(item, ElementIdComparer.NoNullsAscending) >= 0;

      if (Values is ElementId[] array)
        return Array.BinarySearch(array, item, ElementIdComparer.NoNullsAscending) >= 0;

      return Values.Contains(item);
    }

    public void CopyTo(ElementId[] array, int arrayIndex) => Values.CopyTo(array, arrayIndex);

    void ICollection<ElementId>.Add(ElementId item) => throw new InvalidOperationException("Collection is read-only");
    bool ICollection<ElementId>.Remove(ElementId item) => throw new InvalidOperationException("Collection is read-only");
    void ICollection<ElementId>.Clear() => throw new InvalidOperationException("Collection is read-only");
    #endregion

    #region ISet
    bool ISet<ElementId>.Add(ElementId item) => throw new InvalidOperationException("Collection is read-only");

    void ISet<ElementId>.UnionWith(IEnumerable<ElementId> other) => throw new InvalidOperationException("Collection is read-only");
    void ISet<ElementId>.IntersectWith(IEnumerable<ElementId> other) => throw new InvalidOperationException("Collection is read-only");
    void ISet<ElementId>.ExceptWith(IEnumerable<ElementId> other) => throw new InvalidOperationException("Collection is read-only");
    void ISet<ElementId>.SymmetricExceptWith(IEnumerable<ElementId> other) => throw new InvalidOperationException("Collection is read-only");

    public bool IsSubsetOf(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (Values is ISet<ElementId> set)
        return set.IsSubsetOf(other);

      if (other is ICollection<ElementId> otherCollection && otherCollection.Count < Count)
        return false;

      var (unique, missing) = CompareItems(other, breakOnMissing: false);
      return unique == Count && missing >= 0;
    }

    public bool IsSupersetOf(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (Values is ISet<ElementId> set)
        return set.IsSupersetOf(other);

      if (other is ICollection<ElementId> otherCollection && Count < otherCollection.Count)
        return false;

      return other.All(Contains);
    }

    public bool IsProperSubsetOf(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (Values is ISet<ElementId> set)
        return set.IsProperSubsetOf(other);

      if (Count == 0 && other is ICollection<ElementId> otherCollection)
        return otherCollection.Count > 0;

      var (unique, missing) = CompareItems(other, breakOnMissing: false);
      return unique == Count && missing > 0;
    }

    public bool IsProperSupersetOf(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (Values is ISet<ElementId> set)
        return set.IsProperSupersetOf(other);

      if (other is ICollection<ElementId> otherCollection && otherCollection.Count == 0)
        return Count > 0;

      var (unique, missing) = CompareItems(other, breakOnMissing: true);
      return unique < Count && missing == 0;
    }

    public bool Overlaps(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (Values is ISet<ElementId> set)
        return set.Overlaps(other);

      if (Count == 0)
        return false;

      return other.Any(Contains);
    }

    public bool SetEquals(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      // If both are a sorted IList then each element should match on the same position.
      if (other is ReadOnlyElementIdSet otherSet && otherSet.Values is IList<ElementId> otherList && Values is IList<ElementId> thisList)
      {
        if (thisList.Count != otherList.Count) return false;

        var count = thisList.Count;
        for (int i = 0; i < count; ++i)
        {
          if (thisList[i] != otherList[i])
            return false;
        }

        return true;
      }

      if (other is ICollection<ElementId> otherCollection && otherCollection.Count != Count)
        return false;

      if (Values is ISet<ElementId> set)
        return set.SetEquals(other);

      var (unique, missing) = CompareItems(other, breakOnMissing: true);
      return unique == Count && missing == 0;
    }

    private int IndexOf(ElementId item)
    {
      if (Values is List<ElementId> list)
        return list.BinarySearch(item, ElementIdComparer.NoNullsAscending);

      if (Values is ElementId[] array)
        return Array.BinarySearch(array, item, ElementIdComparer.NoNullsAscending);

      var index = 0;
      foreach (var id in Values)
      {
        if (id == item) return index;
        index++;
      }

      return int.MinValue;
    }

    private (int Unique, int Missing) CompareItems(IEnumerable<ElementId> other, bool breakOnMissing)
    {
      if (Count == 0)
        return (0, other.Count());

      var unique = 0;
      var missing = 0;
      var hits = new bool[Count];

      foreach (var item in other)
      {
        var index = IndexOf(item);
        if (index < 0)
        {
          missing++;
          if (breakOnMissing) break;
        }
        else if (!hits[index])
        {
          hits[index] = true;
          unique++;
        }
      }

      return (unique, missing);
    }
    #endregion
  }

  public static class FilteredElementCollectorExtension
  {
    internal static IReadOnlyList<ElementId> AsReadOnlyElementIdList(this ICollection<ElementId> source)
    {
      return source is IReadOnlyList<ElementId> list ? list : source.ToArray();
    }

    /// <summary>
    /// Used internally to wrap an <see cref="ICollection{ElementId}"/> into an <see cref="ISet{Elementd}"/>.
    /// </summary>
    /// <remarks>Use only if you are sure <paramref name="collection"/> is an <see cref="ISet{ElementId}"/> or an ordered <see cref="IList{ElementId}"/></remarks>
    /// <param name="collection"></param>
    /// <returns></returns>
    internal static ReadOnlyElementIdSet AsReadOnlyElementIdSet(this ICollection<ElementId> collection)
    {
      return collection is ReadOnlyElementIdSet set ? set :
        new ReadOnlyElementIdSet(collection);
    }

    internal static ISet<ElementId> ToReadOnlyElementIdSet(this IEnumerable<ElementId> source)
    {
      return source is ReadOnlyElementIdSet set ? set :
        new ReadOnlyElementIdSet(source as ISet<ElementId> ?? new HashSet<ElementId>(source));
    }

    public static ISet<ElementId> ToReadOnlyElementIdSet(this FilteredElementCollector collector)
    {
      return new ReadOnlyElementIdSet(collector.ToElementIds());
    }

    /// <summary>
    /// FilteredElementCollector that fires an Autodesk.Revit.Exceptions.ArgumentException.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    internal static FilteredElementCollector Invalid(Document document) => new FilteredElementCollector(document, ElementIdExtension.Invalid);

    /// <summary>
    /// FilteredElementCollector that contains no element.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static FilteredElementCollector Empty(Document document) => new FilteredElementCollector(document).WherePasses(CompoundElementFilter.Empty);

    /// <summary>
    /// FilteredElementCollector that contains all elements.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public static FilteredElementCollector Universe(Document document) => new FilteredElementCollector(document).WherePasses(CompoundElementFilter.Universe);

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
