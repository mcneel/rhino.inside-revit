namespace System.Collections.Generic.Extensions
{
  static class Extension
  {
    #region IEnumerator
    public static IEnumerable<K> Select<K, T>(this IEnumerator<T> e, Func<T, K> selector)
    {
      while (e.MoveNext())
        yield return selector(e.Current);
    }
    #endregion
  }
}
