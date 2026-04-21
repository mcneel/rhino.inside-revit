using System.Linq;

namespace System.Collections.Generic
{
  internal static class ListExtensions
  {
    public static void AddRange<T>(this List<T> list, IEnumerable<T> collection, int collectionCount)
    {
      if (list.Capacity < list.Count + collectionCount)
        list.Capacity = list.Count + collectionCount;

      list.AddRange(collection);
    }
  }
  internal static class IListExtensions
  {
    public static T ElementAtOrLast<T>(this IList<T> list, int index)
    {
      var count = list.Count;      
      return index < count ? list[index] : count == 0 ? default : list[count - 1];
    }
  }

  internal static class EnumerableExtensions
  {
    public static IEnumerable<T> As<T>(this IEnumerable values)
    {
      foreach (var value in values)
        yield return value is T t ? t : default;
    }

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

      var comparer = EqualityComparer<T>.Default;
      foreach (var item in values)
      {
        if (comparer.Equals(item, value))
          yield return index;

        index++;
      }
    }

    public static IEnumerable<T> TakeButLast<T>(this IEnumerable<T> source, out T last) where T : class
    {
      if (source == null) throw new ArgumentNullException(nameof(source));

      last = default;
      using var e = source.GetEnumerator();
      if (!e.MoveNext()) Array.Empty<T>();

      var count = (source as IList)?.Count - 1 ?? 0;
      var queue = new Queue<T>(count);
      last = e.Current;
      while (e.MoveNext())
      {
        queue.Enqueue(last);
        last = e.Current;
      }
      return queue;
    }

    public static IEnumerable<T> TakeButLast<T>(this IEnumerable<T> source, out T? last) where T : struct
    {
      if (source == null) throw new ArgumentNullException(nameof(source));

      last = default;
      using var e = source.GetEnumerator();
      if (!e.MoveNext()) Array.Empty<T>();

      var count = (source as IList)?.Count - 1 ?? 0;
      var queue = new Queue<T>(count);
      last = e.Current;
      while (e.MoveNext())
      {
        queue.Enqueue(last.Value);
        last = e.Current;
      }
      return queue;
    }

    public static IEnumerable<TResult> ZipOrLast<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
    {
      if (first is null) throw new ArgumentNullException(nameof(first));
      if (second is null) throw new ArgumentNullException(nameof(second));
      if (resultSelector is null) throw new ArgumentNullException(nameof(resultSelector));

      using (var e1 = first.GetEnumerator())
      using (var e2 = second.GetEnumerator())
      {
        var next1 = true;
        var next2 = true;
        var last1 = default(TFirst);
        var last2 = default(TSecond);
        while ((next1 && (next1 = e1.MoveNext())) | (next2 && (next2 = e2.MoveNext())))
        {
          yield return resultSelector(next1 ? (last1 = e1.Current) : last1, next2 ? (last2 = e2.Current) : last2);
        }
      }
    }

    public static IEnumerable<(TFirst, TSecond)> ZipOrLast<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
    {
      if (first is null) throw new ArgumentNullException(nameof(first));
      if (second is null) throw new ArgumentNullException(nameof(second));

      using (var e1 = first.GetEnumerator())
      using (var e2 = second.GetEnumerator())
      {
        var next1 = true;
        var next2 = true;
        var last1 = default(TFirst);
        var last2 = default(TSecond);
        while ((next1 && (next1 = e1.MoveNext())) | (next2 && (next2 = e2.MoveNext())))
        {
          yield return (next1 ? (last1 = e1.Current) : last1, next2 ? (last2 = e2.Current) : last2);
        }
      }
    }

    public static IEnumerable<(T0, T1, T2, T3, T4, T5)> ZipOrLast<T0, T1, T2, T3, T4, T5>
    (
      this IEnumerable<T0> enumerable0,
      IEnumerable<T1> enumerable1,
      IEnumerable<T2> enumerable2,
      IEnumerable<T3> enumerable3,
      IEnumerable<T4> enumerable4,
      IEnumerable<T5> enumerable5
    )
    {
      if (enumerable0 is null) throw new ArgumentNullException(nameof(enumerable0));
      if (enumerable1 is null) throw new ArgumentNullException(nameof(enumerable1));
      if (enumerable2 is null) throw new ArgumentNullException(nameof(enumerable2));
      if (enumerable3 is null) throw new ArgumentNullException(nameof(enumerable3));
      if (enumerable4 is null) throw new ArgumentNullException(nameof(enumerable4));
      if (enumerable5 is null) throw new ArgumentNullException(nameof(enumerable5));

      using (var e0 = enumerable0.GetEnumerator())
      using (var e1 = enumerable1.GetEnumerator())
      using (var e2 = enumerable2.GetEnumerator())
      using (var e3 = enumerable3.GetEnumerator())
      using (var e4 = enumerable4.GetEnumerator())
      using (var e5 = enumerable5.GetEnumerator())
      {
        var next0 = true; var next1 = true; var next2 = true; var next3 = true; var next4 = true; var next5 = true;
        var last0 = default(T0); var last1 = default(T1); var last2 = default(T2); var last3 = default(T3); var last4 = default(T4); var last5 = default(T5);
        while
        (
          (next0 && (next0 = e0.MoveNext())) |
          (next1 && (next1 = e1.MoveNext())) |
          (next2 && (next2 = e2.MoveNext())) |
          (next3 && (next3 = e3.MoveNext())) |
          (next4 && (next4 = e4.MoveNext())) |
          (next5 && (next5 = e5.MoveNext()))
        )
        {
          yield return
          (
            next0 ? (last0 = e0.Current) : last0,
            next1 ? (last1 = e1.Current) : last1,
            next2 ? (last2 = e2.Current) : last2,
            next3 ? (last3 = e3.Current) : last3,
            next4 ? (last4 = e4.Current) : last4,
            next5 ? (last5 = e5.Current) : last5
          );
        }
      }
    }
  }
}
