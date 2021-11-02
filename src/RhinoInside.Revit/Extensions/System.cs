using System.Runtime.InteropServices;

namespace System
{
  static class TypeExtension
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

    public static bool IsGenericSubclassOf(this Type type, Type baseGenericType, out Type genericType)
    {
      for (; type != typeof(object); type = type.BaseType)
      {
        var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (baseGenericType == cur)
        {
          genericType = type;
          return true;
        }
      }

      genericType = default;
      return false;
    }
  }

  static class StringExtension
  {
    /// <summary>
    /// Ensures string is no longer than the given length. Cuts the string and adds ellipsis at the end if longer.
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

    [DllImport("SHLWAPI", CharSet = CharSet.Unicode)]
    static extern bool PathCompactPathExW([Out] Text.StringBuilder pszOut, string szPath, int cchMax, int dwFlags);
    internal static string TripleDotPath(this string path, int maxLength)
    {
      if (path is null) return null;
      var builder = new Text.StringBuilder(maxLength + 1);
      PathCompactPathExW(builder, path, maxLength, 0);
      return builder.ToString();
    }
  }

  internal static class FileExtension
  {
    public static void MoveFile(string sourceFileName, string destinationFileName, bool overwrite)
    {
      Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(sourceFileName, destinationFileName, overwrite);
    }
  }
}
