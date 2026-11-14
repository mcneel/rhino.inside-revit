using System.Collections.Generic;

namespace System.Linq
{
  public static class EnumerableExtensions
  {
#if !NET
    public static T FirstOrDefault<T>(this IEnumerable<T> values, T value)
    {
      if (values is IList<T> list)
      {
        if (list.Count > 0) return list[0];
      }
      else if (values is IReadOnlyList<T> readOnlyList)
      {
        if (readOnlyList.Count > 0) return readOnlyList[0];
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
#endif
  }
}
