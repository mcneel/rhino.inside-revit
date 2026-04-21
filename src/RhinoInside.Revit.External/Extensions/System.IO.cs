using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.IO
{
  static class PathExtension
  {
    internal static readonly StringComparer Comparer = new PathComparer();

    private class PathComparer : StringComparer
    {
      private static readonly StringComparer StringComparer = OrdinalIgnoreCase;

      public PathComparer() { }

      private static string Normalize(string path) => Path.GetFullPath(path);

      internal static string Canonicalize(string path) => new FileInfo(path).FullName;

      public override int Compare(string x, string y)
      {
        var comparison = StringComparer.Compare(x, y);
        if (comparison == 0) return 0;

        try
        {
          if (StringComparer.Compare(Normalize(x), Normalize(y)) == 0) return 0;
          return StringComparer.Compare(Canonicalize(x), Canonicalize(y));
        }
        catch { return comparison; }
      }

      public override bool Equals(string x, string y)
      {
        if (StringComparer.Equals(x, y)) return true;
        try
        {
          if (StringComparer.Equals(Normalize(x), Normalize(y))) return true;
          return StringComparer.Equals(Canonicalize(x), Canonicalize(y));
        }
        catch { return false; }
      }

      public override int GetHashCode(string obj)
      {
        try { return obj is null ? 0 : StringComparer.GetHashCode(Canonicalize(obj)); }
        catch { return int.MinValue; }
      }
    }

    public static bool TryGetFullyQualifiedPath(ref string path)
    {
      if (string.IsNullOrWhiteSpace(path)) return false;
      if (!IsFullyQualifiedPath(path)) return false;
      try
      {
        path = PathComparer.Canonicalize(path);
        return true;
      }
      catch { return false; }
    }

    public static bool IsFullyQualifiedPath(this string path)
    {
      if (path == null) return false;

#if NET5_0_OR_GREATER
      return Path.IsPathFullyQualified(path);
#else
      if (path.Length < 2) return false;

      return IsDirectorySeparator(path[0]) ?
        IsDirectorySeparator(path[1]) :
        (
          path.Length >= 3 &&
          path[1] == Path.VolumeSeparatorChar &&
          IsDirectorySeparator(path[2]) &&
          IsValidDriveChar(path[0])
        );

      bool IsValidDriveChar(char value) => ('A' <= value && value <= 'Z') || ('a' <= value && value <= 'z');
      bool IsDirectorySeparator(char c) => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
#endif
    }

    internal static string TripleDotPath(this string path, int maxLength)
    {
      if (path is null) return null;
      var builder = new Text.StringBuilder(maxLength + 1);
      SafeNativeMethods.PathCompactPathExW(builder, path, maxLength, 0);
      return builder.ToString();
    }

    static class SafeNativeMethods
    {
      [DllImport("SHLWAPI", CharSet = CharSet.Unicode)]
      public static extern bool PathCompactPathExW([Out] Text.StringBuilder pszOut, string szPath, int cchMax, int dwFlags);
    }
  }

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
