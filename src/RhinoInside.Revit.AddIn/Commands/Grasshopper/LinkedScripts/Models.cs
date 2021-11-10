using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace RhinoInside.Revit.AddIn.Commands
{
  /// <summary>
  /// Types of scripts that can be executed
  /// </summary>
  public enum ScriptType
  {
    GhFile = 0,
    GhxFile,
  }

  /// <summary>
  /// Generic linked item
  /// </summary>
  public abstract class LinkedItem
  {
    public string Text
    {
      get
      {
        string displayName = string.Empty;
        var nameLines = Name.Split('-');
        for (int l = 0; l < nameLines.Length; ++l)
        {
          var line = nameLines[l].
            Trim(' ').                    // Remove trailing spaces 
            Replace(' ', (char) 0x00A0);  // Replace spaces by non-breaking-spaces
          if (line == string.Empty) continue;

          displayName += line.TripleDot(12);
          if (l < nameLines.Length - 1)
            displayName += Environment.NewLine;
        }

        return displayName;
      }
    }

    public string Name { get; set; } = string.Empty;
    public string Tooltip { get; set; } = string.Empty;
  }

  /// <summary>
  /// Group of linked items
  /// </summary>
  public class LinkedItemGroup : LinkedItem
  {
    public string GroupPath { get; set; }
    public List<LinkedItem> Items { get; set; } = new List<LinkedItem>();
  }

  /// <summary>
  /// Linked script
  /// </summary>
  public class LinkedScript : LinkedItem
  {
    public ScriptType ScriptType { get; set; }
    public string ScriptPath { get; set; }
    public Type ScriptCommandType { get; set; }

    public string Description { get; set; } = null;

    static readonly string[] SupportedExtensions = new string[] { ".gh", ".ghx" };
    public static LinkedScript FromPath(string scriptPath)
    {
      var ext = Path.GetExtension(scriptPath).ToLower();
      if (File.Exists(scriptPath) && SupportedExtensions.Contains(ext))
      {
        var script = new LinkedScript
        {
          ScriptType = ext == ".gh" ? ScriptType.GhFile : ScriptType.GhxFile,
          ScriptPath = scriptPath,
          Name = Path.GetFileNameWithoutExtension(scriptPath),
        };

        // now that base props are setup, read the rest from the file
        var archive = new GH_IO.Serialization.GH_Archive();
        if (archive.ReadFromFile(script.ScriptPath))
        {
          // find gh document properties
          var defProps = archive.GetRootNode.FindChunk("Definition")?.FindChunk("DefinitionProperties");
          if (defProps != null)
          {
            // find description
            if (defProps.ItemExists("Description"))
              script.Description = defProps.GetString("Description");

            // find icon
            if (defProps.ItemExists("IconImageData"))
            {
              var iconImgData = defProps.GetString("IconImageData");
              if (iconImgData is string && !string.IsNullOrEmpty(iconImgData))
                script.IconImageData = iconImgData;
            }
          }
        }

        return script;
      }
      else
        return null;
    }

    string IconImageData = null;
    public BitmapSource GetScriptIcon(int width, int height)
    {
      if (IconImageData is string)
      {
        // if SVG
        if (IconImageData.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) > 0)
        {
          return Rhino.UI.DrawingUtilities.BitmapFromSvg(
            IconImageData, width, height
            ).ToBitmapImage();
        }
        // else assume bitmap
        else
        {
          return new System.Drawing.Bitmap(
            new System.IO.MemoryStream(
              System.Convert.FromBase64String(IconImageData)
              )
            ).ToBitmapImage(width, height);
        }
      }
      else
        return null;
    }
  }

  /// <summary>
  /// Package of Scripts for this addin
  /// </summary>
  public class ScriptPkg
  {
    public string Name;
    public string Location;
    public List<LinkedItem> FindLinkedItems() => FindLinkedItemsRecursive(Location);

    public static bool operator ==(ScriptPkg lp, ScriptPkg rp) => lp.Location.Equals(rp.Location, StringComparison.InvariantCultureIgnoreCase);
    public static bool operator !=(ScriptPkg lp, ScriptPkg rp) => !lp.Location.Equals(rp.Location, StringComparison.InvariantCultureIgnoreCase);
    public override bool Equals(object obj) {
      if (obj is ScriptPkg pkg)
        return this == pkg;
      return false;
    }
    public override int GetHashCode() => Location.GetHashCode();

    /// <summary>
    /// Find all user script packages
    /// </summary>
    /// <returns></returns>
    public static List<ScriptPkg> GetUserScriptPackages()
    {
      var pkgs = new List<ScriptPkg>();
      foreach (var location in Properties.AddInOptions.Current.ScriptLocations)
        if (Directory.Exists(location))
          pkgs.Add(
            new ScriptPkg { Name = Path.GetFileName(location), Location = location }
            );
      return pkgs;
    }

    /// <summary>
    /// Find user script package by name
    /// </summary>
    /// <returns></returns>
    public static ScriptPkg GetUserScriptPackageByName(string name)
    {
      foreach (var pkg in GetUserScriptPackages())
        if (pkg.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
          return pkg;
      return null;
    }

    /// <summary>
    /// Find user script package by location
    /// </summary>
    /// <returns></returns>
    public static ScriptPkg GetUserScriptPackageByLocation(string location)
    {
      foreach (var pkg in GetUserScriptPackages())
        if (pkg.Location.Equals(location, StringComparison.InvariantCultureIgnoreCase))
          return pkg;
      return null;
    }

    private static List<LinkedItem> FindLinkedItemsRecursive(string location)
    {
      var items = new List<LinkedItem>();

      foreach (var subDir in Directory.GetDirectories(location))
      {
        // only go one level deep
        items.Add(
          new LinkedItemGroup
          {
            GroupPath = subDir,
            Name = Path.GetFileName(subDir),
            Items = FindLinkedItemsRecursive(subDir),
          }
        );
      }

      foreach (var entry in Directory.GetFiles(location))
        if (LinkedScript.FromPath(entry) is LinkedScript script)
          items.Add(script);

      return items.OrderBy(x => x.Name).ToList();
    }
  }
}
