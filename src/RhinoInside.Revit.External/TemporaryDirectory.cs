using System;
using System.IO;

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
      readonly System.Runtime.Serialization.ObjectIDGenerator Generator = new System.Runtime.Serialization.ObjectIDGenerator();
      public string PrefixOf(object value) => Generator.GetId(value, out var _).ToString("X16");
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
      GC.AddMemoryPressure(fileInfo.Length);
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
      GC.AddMemoryPressure(fileInfo.Length);
      return fileInfo;
    }
  }
}
