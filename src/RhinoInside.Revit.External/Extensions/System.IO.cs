using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.IO
{
#pragma warning disable CA1060 // Move pinvokes to native methods class
  static class PathExtension
  {
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
