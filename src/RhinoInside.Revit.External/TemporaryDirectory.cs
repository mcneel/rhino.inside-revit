using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RhinoInside.Revit
{
  internal interface ITemporaryPrefixGenerator
  {
    string PrefixOf(object value);
  }

  internal class TemporaryDirectory
  {
    class TemporaryPrefixGenerator : ITemporaryPrefixGenerator
    {
      private readonly ConditionalWeakTable<object, string> Prefixes = new ConditionalWeakTable<object, string>();
      private long Seed;

      public string PrefixOf(object value) => value is null ? null :
        Prefixes.GetValue(value, (k) => Interlocked.Increment(ref Seed).ToString("X16"));
    }

    public readonly DirectoryInfo Directory;
    readonly ITemporaryPrefixGenerator PrefixGenerator;

    public TemporaryDirectory(string path)
    {
      PrefixGenerator = new TemporaryPrefixGenerator();
      Directory = System.IO.Directory.CreateDirectory(path);
    }

    public TemporaryDirectory(string path, ITemporaryPrefixGenerator prefixGenerator)
    {
      if (prefixGenerator is null)
        throw new ArgumentNullException(nameof(prefixGenerator));

      PrefixGenerator = prefixGenerator;
      Directory = System.IO.Directory.CreateDirectory(path);
    }

    public string PrefixOf(object key) => PrefixGenerator.PrefixOf(key);

    public void Delete(object key)
    {
      try
      {
        Directory.Refresh();
        if (Directory.Exists)
        {
          foreach (var textureFile in Directory.EnumerateFiles($"{PrefixGenerator.PrefixOf(key)}*", SearchOption.TopDirectoryOnly))
          {
            if (!textureFile.Attributes.HasFlag(FileAttributes.Temporary))
              continue;

            try { textureFile.Delete(); } catch { }
          }
        }
      }
      catch { }
    }

    public FileInfo CopyFrom(object key, FileInfo sourceFile, string fileName)
    {
      fileName = fileName ?? Path.GetFileNameWithoutExtension(sourceFile.FullName);
      var extension = Path.GetExtension(sourceFile.FullName);
      var temporaryFilePath = Path.Combine(Directory.FullName, PrefixGenerator.PrefixOf(key) + fileName + extension);
      if (!sourceFile.FullName.Equals(temporaryFilePath, StringComparison.OrdinalIgnoreCase))
        sourceFile.CopyTo(temporaryFilePath, overwrite: true);

      var fileInfo = new FileInfo(temporaryFilePath);
      fileInfo.Attributes |= FileAttributes.Temporary;
      return fileInfo;
    }

    public FileInfo MoveFrom(object key, FileInfo sourceFile, string fileName)
    {
      fileName = fileName ?? Path.GetFileNameWithoutExtension(sourceFile.FullName);
      var extension = Path.GetExtension(sourceFile.FullName);
      var temporaryFilePath = Path.Combine(Directory.FullName, PrefixGenerator.PrefixOf(key) + fileName + extension);
      if (!sourceFile.FullName.Equals(temporaryFilePath, StringComparison.OrdinalIgnoreCase))
        sourceFile.MoveTo(temporaryFilePath, overwrite: true);

      var fileInfo = new FileInfo(temporaryFilePath);
      fileInfo.Attributes |= FileAttributes.Temporary;
      return fileInfo;
    }
  }
}
