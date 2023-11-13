using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal readonly struct ReadOnlyElementIdSet : ISet<ElementId>
  {
    readonly ICollection<ElementId> collection;
    internal ReadOnlyElementIdSet(ICollection<ElementId> source) => collection = source;

    public static readonly ReadOnlyElementIdSet Empty = new ReadOnlyElementIdSet(Array.Empty<ElementId>());

    #region IEnumerable
    public IEnumerator<ElementId> GetEnumerator() => collection.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => collection.GetEnumerator();
    #endregion

    #region ICollection
    public int Count => collection.Count;
    bool ICollection<ElementId>.IsReadOnly => true;

    public bool Contains(ElementId item)
    {
      if (collection is List<ElementId> list)
        return list.BinarySearch(item, ElementIdComparer.NoNullsAscending) >= 0;

      if (collection is ElementId[] array)
        return Array.BinarySearch(array, item, ElementIdComparer.NoNullsAscending) >= 0;

      return collection.Contains(item);
    }

    public void CopyTo(ElementId[] array, int arrayIndex) => collection.CopyTo(array, arrayIndex);

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

      if (other is ICollection<ElementId> otherCollection)
        return otherCollection.Count < Count;

      if (collection is ISet<ElementId> set)
        return set.IsSubsetOf(other);

      var (unique, mising) = CompareItems(other, breakOnMissing: false);
      return unique == Count && mising >= 0;
    }

    public bool IsSupersetOf(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (other is ICollection<ElementId> otherCollection)
        return Count < otherCollection.Count;

      if (collection is ISet<ElementId> set)
        return set.IsSupersetOf(other);

      return other.All(Contains);
    }

    public bool IsProperSubsetOf(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (Count == 0 && other is ICollection<ElementId> otherCollection)
        return otherCollection.Count > 0;

      if (collection is ISet<ElementId> set)
        return set.IsProperSubsetOf(other);

      var (unique, mising) = CompareItems(other, breakOnMissing: false);
      return unique == Count && mising > 0;
    }

    public bool IsProperSupersetOf(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (other is ICollection<ElementId> otherCollection && otherCollection.Count == 0)
        return Count > 0;

      if (collection is ISet<ElementId> set)
        return set.IsProperSupersetOf(other);

      var (unique, mising) = CompareItems(other, breakOnMissing: true);
      return unique < Count && mising == 0;
    }

    public bool Overlaps(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      if (Count == 0)
        return false;

      if (collection is ISet<ElementId> set)
        return set.Overlaps(other);

      return other.Any(Contains);
    }

    public bool SetEquals(IEnumerable<ElementId> other)
    {
      if (other is null)
        throw new ArgumentNullException(nameof(other));

      // If both are a sorted IList then each element should match on the same position.
      if (other is ReadOnlyElementIdSet otherSet && otherSet.collection is IList<ElementId> otherList && collection is IList<ElementId> thisList)
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

      if (collection is ISet<ElementId> set)
        return set.SetEquals(other);

      var (unique, mising) = CompareItems(other, breakOnMissing: true);
      return unique == Count && mising == 0;
    }

    private int IndexOf(ElementId item)
    {
      if (collection is List<ElementId> list)
        return list.BinarySearch(item, ElementIdComparer.NoNullsAscending);

      if (collection is ElementId[] array)
        return Array.BinarySearch(array, item, ElementIdComparer.NoNullsAscending);

      var index = int.MinValue;
      foreach (var id in collection)
      {
        index++;
        if (id == item) break;
      }

      return index;
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
