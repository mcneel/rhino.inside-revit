namespace System
{
  static class TypeExtensions
  {
    public static bool IsGenericSubclassOf(this Type type, Type baseGenericType)
    {
      for (; type != typeof(object); type = type.BaseType)
      {
        var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (baseGenericType == cur)
          return true;
      }

      return false;
    }
  }

  static class StringExtensions
  {
    /// <summary>
    /// Ensures string is no longer than the given length. Cuts the string and adds ellipsis at the end if longer
    /// </summary>
    /// <param name="sourceString"></param>
    /// <param name="maxLength">Maxmium length of the string</param>
    /// <returns></returns>
    public static string TripleDot(this string sourceString, int maxLength)
    {
      maxLength = Math.Max(1, maxLength);

      return sourceString.Length > maxLength ?
        sourceString.Substring(0, maxLength - 1) + '…' :
        sourceString;
    }
  }
}

namespace RhinoInside.Revit.Extended
{
  using static System.Math;
  using static System.Double;

  static class Math
  {
    //public static int Clamp(this int v, int lo, int hi)
    //{
    //  return hi < v ? hi : v < lo ? lo : v;
    //}

    //public static double Clamp(this double v, double lo, double hi)
    //{
    //  return hi < v ? hi : v < lo ? lo : v;
    //}

    public static bool IsPositive(double value)
    {
      switch (Sign(value))
      {
        case -1: return false;
        case +1: return true;
      }

      return IsPositiveInfinity(1.0 / value);
    }

    public static bool IsNegative(double value)
    {
      switch (Sign(value))
      {
        case -1: return true;
        case +1: return false;
      }

      return IsNegativeInfinity(1.0 / value);
    }
  }
}
