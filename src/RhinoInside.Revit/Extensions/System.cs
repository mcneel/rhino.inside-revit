using System;
using System.Collections.Generic;

namespace RhinoInside.Revit
{
  public static partial class Extension
  {
    #region System
    public static int Clamp(this int v, int lo, int hi)
    {
      return hi < v ? hi : v < lo ? lo : v;
    }

    public static double Clamp(this double v, double lo, double hi)
    {
      return hi < v ? hi : v < lo ? lo : v;
    }

    public static bool IsPositive(this double value)
    {
      switch (Math.Sign(value))
      {
        case -1: return false;
        case +1: return true;
      }

      return double.IsPositiveInfinity(1.0 / value);
    }

    public static bool IsNegative(this double value)
    {
      switch (Math.Sign(value))
      {
        case -1: return true;
        case +1: return false;
      }

      return double.IsNegativeInfinity(1.0 / value);
    }
    #endregion

    #region string
    public static string FirstCharUpper(this string text)
    {
      if (char.IsUpper(text, 0))
        return text;

      var chars = text.ToCharArray();
      chars[0] = char.ToUpperInvariant(chars[0]);
      return new string(chars);
    }
    #endregion

    #region Linq
    public static IEnumerable<K> Select<K, T>(this IEnumerator<T> e, Func<T, K> selector)
    {
      while (e.MoveNext())
        yield return selector(e.Current);
    }
    #endregion
  }
}
