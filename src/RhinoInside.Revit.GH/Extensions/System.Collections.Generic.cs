using System.Linq;

namespace System.Collections.Generic
{
  internal static class ListExtensions
  {
    public static void AddCollection<T>(this List<T> list, ICollection<T> collection)
    {
      if (list.Capacity < list.Count + collection.Count)
        list.Capacity = list.Count + collection.Count;

      list.AddRange(collection);
    }

    public static void AddRange<T>(this List<T> list, IEnumerable<T> collection, int collectionCount)
    {
      if (list.Capacity < list.Count + collectionCount)
        list.Capacity = list.Count + collectionCount;

      list.AddRange(collection);
    }
  }

  internal static class EnumerableExtensions
  {
    public static T FirstOr<T>(this IEnumerable<T> values, T value)
    {
      if (values is IList<T> list)
      {
        if (list.Count > 0) return list[0];
      }
      else
      {
        using (var e = values.GetEnumerator())
        {
          if (e.MoveNext()) return e.Current;
        }
      }

      return value;
    }

    public static IEnumerable<int> IndexOf<T>(this IEnumerable<T> values, T value, int index)
    {
      if (index < 0)
        throw new ArgumentOutOfRangeException(nameof(index), string.Empty);

      if (index > 0)
        values = values.Skip(index);

      foreach (var item in values)
      {
        if (item.Equals(value))
          yield return index;

        index++;
      }
    }
  }
}
