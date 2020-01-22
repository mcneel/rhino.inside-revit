using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace RhinoInside.Revit.Settings
{
  public static class AddIns
  {
    [Serializable()]
    public class AddIn
    {
      [XmlAttribute()] public string Type { get; set; }
      public string Assembly { get; set; }
      public string FullClassName { get; set; }
      public string AddInId { get; set; }
      public string Name { get; set; }
      public string Text { get; set; }
      public string VendorId { get; set; }
      public string VendorDescription { get; set; }
      public string Description { get; set; }
      public string VisibilityMode { get; set; }
      public string Discipline { get; set; }
      public string AvailabilityClassName { get; set; }
      public string LargeImage { get; set; }
      public string SmallImage { get; set; }
      public string LongDescription { get; set; }
      public string TooltipImage { get; set; }
      public string LanguageType { get; set; }
      public string AllowLoadIntoExistingSession { get; set; }
    }

    [Serializable(), XmlRoot("RevitAddIns", Namespace = "")]
    public class RevitAddIns : List<AddIn>
    {
      [NonSerialized]
      public Environment.SpecialFolder Folder;
      [NonSerialized]
      public string AddinFilePath;
    }

    internal static bool LoadFrom(string addinManifestPath, out RevitAddIns addins)
    {
      try
      {
        using (var ReadFileStream = new FileStream(addinManifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
          var serializer = new XmlSerializer(typeof(RevitAddIns));
          addins = serializer.Deserialize(ReadFileStream) as RevitAddIns;
          foreach (var addin in addins)
          {
            addin.Assembly = addin.Assembly.Trim('\"');
            if (!Path.IsPathRooted(addin.Assembly))
              addin.Assembly = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(addinManifestPath), addin.Assembly));
          }
          return true;
        }
      }
      catch (FileNotFoundException)
      {
        addins = null;
        return false;
      }
    }

    public static void GetSystemAddins(string versionNumber, out List<string> manifests)
    {
      var addinFolders = new DirectoryInfo[]
      {
        // %ProgramFiles%\Autodesk\Revit 20XX\Addins\
        new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", $"Revit {versionNumber}", "Addins")),
      };

      var manifestsDictionary = new List<KeyValuePair<string, string>>();

      foreach (var folder in addinFolders)
      {
        IEnumerable<FileInfo> addinFiles;
        try { addinFiles = folder.EnumerateFiles("*.addin", SearchOption.AllDirectories); }
        catch (System.IO.DirectoryNotFoundException) { continue; }

        foreach (var manifest in addinFiles)
        {
          var index = manifestsDictionary.FindIndex(x => x.Key == manifest.Name);
          if (index >= 0)
            manifestsDictionary.RemoveAt(index);

          manifestsDictionary.Add(new KeyValuePair<string, string>(manifest.Name, manifest.FullName));
        }
      }

      manifests = manifestsDictionary.Select(x => x.Value).ToList();
    }

    public static void GetInstalledAddins(string versionNumber, out List<string> manifests)
    {
      var addinFolders = new DirectoryInfo[]
      {
        // %APPDATA%\Autodesk\Revit\Addins\20XX
        new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins", versionNumber)),
        // %ProgramData%\Autodesk\Revit\Addins\20XX
        new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "Revit", "Addins", versionNumber)),
      };

      var manifestsDictionary = new Dictionary<string, string>();

      foreach (var folder in addinFolders)
      {
        IEnumerable<FileInfo> addinFiles;
        try { addinFiles = folder.EnumerateFiles("*.addin", SearchOption.AllDirectories); }
        catch (System.IO.DirectoryNotFoundException) { continue; }

        foreach (var manifest in addinFiles)
        {
          try { manifestsDictionary.Add(manifest.Name, manifest.FullName); }
          catch (System.ArgumentException) { }
        }
      }

      manifests = manifestsDictionary.OrderBy(x => x.Key).Select(x => x.Value).ToList();
    }

    public static void GetAllAddins(string versionNumber, out List<string> manifests)
    {
      GetSystemAddins(versionNumber, out var systemManifests);
      GetInstalledAddins(versionNumber, out var installedManifests);

      systemManifests.AddRange(installedManifests);
      manifests = systemManifests;
    }
  }

  internal class LockAddIns : IDisposable
  {
    FileStream[] streams;
    public LockAddIns(string versionNumber)
    {
      AddIns.GetInstalledAddins(versionNumber, out var manifests);
      streams = manifests.
                Where(x => string.Compare(Path.GetFileName(x), "RhinoInside.Revit.addin", StringComparison.OrdinalIgnoreCase) != 0).
                Select(x => new FileStream(x, FileMode.Open, FileAccess.Read, FileShare.None)).
                ToArray();
    }
    void IDisposable.Dispose()
    {
      foreach (var stream in streams)
        stream.Dispose();
    }
  }
}
