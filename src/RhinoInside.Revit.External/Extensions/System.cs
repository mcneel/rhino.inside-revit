using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

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
    /// <summary>
    /// Determines whether two sequences are equivalent by comparing the elements using <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/> comparer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>true if the two sequences are equivalent; otherwise, false.</returns>
    /// <remarks>Empty arrays and null array references are considered equivalent sequences.</remarks>
    public static bool SequenceEquivalent<T>(this T[] left, T[] right)
    {
#if NET
      return ((ReadOnlySpan<T>) left).SequenceEqual(right, null);
#else
      var leftLength = left?.Length ?? 0;
      var rightLength = right?.Length ?? 0;
      if (leftLength != rightLength) return false;

      for (int i=0; i < leftLength; ++i)
        if (!Collections.Generic.EqualityComparer<T>.Default.Equals(left[i], right[i])) return false;

      return true;
#endif
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

    #region Escaping
    internal static string Escape(this string value, params char[] forced)
    {
      var literal = new StringBuilder(value.Length);
      foreach (var c in value)
      {
        switch (c)
        {
          case '\0': literal.Append("\\\0"); break;
          case '\a': literal.Append("\\\a"); break;
          case '\b': literal.Append("\\\b"); break;
          case '\f': literal.Append("\\\f"); break;
          case '\n': literal.Append("\\\n"); break;
          case '\r': literal.Append("\\\r"); break;
          case '\t': literal.Append("\\\t"); break;
          case '\v': literal.Append("\\\v"); break;
          case '\\': literal.Append("\\\\"); break;
          default:
            if (char.GetUnicodeCategory(c) != UnicodeCategory.Control)
            {
              if (forced is object && Array.IndexOf(forced, c) != -1)
              {
                literal.Append('\\');
                literal.Append(c);
              }
              else literal.Append(c);
            }
            else
            {
              literal.Append("\\x");

              var codepoint = (ushort) c;
              if (codepoint <= byte.MaxValue)
                literal.Append(codepoint.ToString("x2"));
              else
                literal.Append(codepoint.ToString("x4"));
            }
            break;
        }
      }
      return literal.ToString();
    }

    internal static string Unescape(this string value, params char[] allowed)
    {
      if (string.IsNullOrEmpty(value)) return value;

      var retval = new StringBuilder(value.Length);
      for (int i = 0; i < value.Length;)
      {
        int j = value.IndexOf('\\', i);
        if (j < 0 || j == value.Length - 1) j = value.Length;
        retval.Append(value, i, j - i);

        if (j >= value.Length) break;
        var c = value[j + 1];
        switch (c)
        {
          case '\\': retval.Append('\\'); break;
          case '0': retval.Append('\0'); break;
          case 'a': retval.Append('\a'); break;
          case 'b': retval.Append('\b'); break;
          case 'f': retval.Append('\f'); break;
          case 'n': retval.Append('\n'); break;
          case 'r': retval.Append('\r'); break;
          case 't': retval.Append('\t'); break;
          case 'v': retval.Append('\v'); break;
          case 'x':
            int k = 0;
            const string Hex = "0123456789ABCDEFabcdef";
            while (k < 4 && j + 2 + k < value.Length && Hex.Contains($"{value[j + 2 + k]}")) ++k;

            if (k > 0 && ushort.TryParse(value.Substring(j + 2, k), NumberStyles.AllowHexSpecifier, null, out var codepoint))
              retval.Append((char) codepoint);
            else
              retval.Append((char)0xFFFD);
            j += k;
            break;
          default:
            if (allowed is null || Array.IndexOf(allowed, c) != -1)
              retval.Append(c);
            else
              retval.Append((char)0xFFFD);
            break;
        }
        i = j + 2;
      }
      return retval.ToString();
    }

    public static string ToControlEscaped(this string value) => Escape(value, Array.Empty<char>());
    public static string ToControlUnescaped(this string value) => Unescape(value, Array.Empty<char>());

    internal static string ToStringLiteral(this string value) => Escape(value, '\"');
    internal static string ToStringVerbatim(this string value) => Unescape(value, '\'', '\"', '?');
    #endregion
  }
}

namespace System.IO
{
#pragma warning disable CA1060 // Move pinvokes to native methods class
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
#pragma warning restore CA1060 // Move pinvokes to native methods class

  internal static class DirectoryInfoExtension
  {
    public static IEnumerable<FileInfo> EnumerateFilesByExtension(this DirectoryInfo value, string extension, SearchOption searchOptions = SearchOption.TopDirectoryOnly)
    {
      foreach (var file in value.EnumerateFiles($"*{extension}", searchOptions))
      {
#if NETFRAMEWORK
        if (extension.Length == 3)
        {
          // https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netframework-4.8
          // If the specified extension is exactly three characters long,
          // the method returns files with extensions that begin with the specified extension.
          // For example, "*.xls" returns both "book.xls" and "book.xlsx"
          if (!file.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase)) continue;
        }
#endif
        yield return file;
      }
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
