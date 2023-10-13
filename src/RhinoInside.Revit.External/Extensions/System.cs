using System.Runtime.InteropServices;

namespace System
{
  static class EnumExtensions
  {
    public static T WithFlag<T>(this T @enum, T flag, bool value) where T : struct, Enum
    {
      var underlyingType = Enum.GetUnderlyingType(typeof(T));
      dynamic e = Convert.ChangeType(@enum, underlyingType);
      dynamic f = Convert.ChangeType(flag, underlyingType);

      return (T) (value ? (e | f) : (e & ~f));
    }
  }

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

  static class ArrayExtension
  {
    public static bool ItemsEqual<T>(this T[] left, T[] right)
    {
      if (ReferenceEquals(left, right)) return true;
      if (left is null || right is null) return false;
      if (left.Length != right.Length) return false;

      for(int i=0; i < left.Length; ++i)
        if (!left[i].Equals(right[i])) return false;

      return true;
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
  }
}

namespace System.IO
{
  static class PathExtension
  {
    public static bool IsFullyQualifiedPath(this string path)
    {
      if (path == null) return false;
      if (path.Length < 2) return false;
      if (IsDirectorySeparator(path[0]))
        return path[1] == '?' || IsDirectorySeparator(path[1]);

      return
      (
        (path.Length >= 3) &&
        (path[1] == Path.VolumeSeparatorChar) &&
        IsDirectorySeparator(path[2]) &&
        IsValidDriveChar(path[0])
      );

      bool IsValidDriveChar(char value) => ('A' <= value && value <= 'Z') || ('a' <= value && value <= 'z');
      bool IsDirectorySeparator(char c) => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
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

  internal static class FileInfoExtension
  {
    public static void MoveTo(this FileInfo source, string destFileName, bool overwrite)
    {
      Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(source.FullName, destFileName, overwrite);
    }
  }
}
