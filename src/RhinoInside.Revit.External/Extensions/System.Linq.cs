using System.Collections.Generic;

namespace System.Linq
{
  public static class EnumerableExtensions
  {
#if !NET
    public static T FirstOrDefault<T>(this IEnumerable<T> source, T value)
    {
      if (source is IList<T> list)
      {
        if (list.Count > 0) return list[0];
      }
      else if (source is IReadOnlyList<T> readOnlyList)
      {
        if (readOnlyList.Count > 0) return readOnlyList[0];
      }
      else
      {
        using (var e = source.GetEnumerator())
        {
          if (e.MoveNext()) return e.Current;
        }
      }

      return value;
    }
#endif

    #region AsReversed
    /// <summary>
    /// Inverts the order of the elements in a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <param name="source">A sequence of values to enumerate in reverse order.</param>
    /// <returns>A sequence whose elements correspond to those of the input sequence in reverse order.</returns>
    public static IEnumerable<TSource> AsReversed<TSource>(this IEnumerable<TSource> source)
    {
      switch (source)
      {
        case TSource[] array:             return AsReversed(array);
        case IList<TSource> list:         return AsReversed(list);
        case IReadOnlyList<TSource> list: return AsReversed(list);
        default:                          return Enumerable.Reverse(source);
      }
    }

    public static IEnumerable<TSource> AsReversed<TSource>(TSource[] array)
    {
      var count = array.Length;
      for (int i = count - 1; i >= 0; i--)
        yield return array[i];
    }

    private static IEnumerable<TSource> AsReversed<TSource>(IList<TSource> list)
    {
      var count = list.Count;
      for (int i = count - 1; i >= 0; i--)
      {
        if (count != list.Count)
          throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

        yield return list[i];
      }
    }

    private static IEnumerable<TSource> AsReversed<TSource>(IReadOnlyList<TSource> list)
    {
      var count = list.Count;
      for (int i = count - 1; i >= 0; i--)
      {
        if (count != list.Count)
          throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

        yield return list[i];
      }
    }
    #endregion
  }
}
