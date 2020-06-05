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
    public static string TripleDot(this string sourceString, uint maxLength)
    {
      if (sourceString.Length > maxLength && maxLength > 3)
        return sourceString.Substring(0, (int) maxLength - 3) + "…";
      else
        return sourceString;
    }
  }
}
