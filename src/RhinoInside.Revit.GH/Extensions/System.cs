using System;
using System.Collections.Generic;

namespace RhinoInside.Revit
{
  static partial class Extension
  {
    #region Linq
    public static IEnumerable<K> Select<K, T>(this IEnumerator<T> e, Func<T, K> selector)
    {
      while (e.MoveNext())
        yield return selector(e.Current);
    }
    #endregion
  }
}
