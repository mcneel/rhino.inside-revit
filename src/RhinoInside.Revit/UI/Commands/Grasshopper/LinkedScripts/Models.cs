using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit.UI
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
  abstract public class LinkedItem
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

    public string Name = string.Empty;
    public string Tooltip = string.Empty;
  }

  /// <summary>
  /// Group of linked items
  /// </summary>
  public class LinkedItemGroup : LinkedItem
  {
    public string GroupPath;
    public List<LinkedItem> Items = new List<LinkedItem>();
  }

  /// <summary>
  /// Linked script
  /// </summary>
  public class LinkedScript : LinkedItem
  {
    public ScriptType ScriptType;
    public string ScriptPath;
    public Type ScriptCommandType;
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
      foreach (var location in AddinOptions.Current.ScriptLocations)
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
            Name = Path.GetFileName(subDir),
            Items = FindLinkedItemsRecursive(subDir),
          }
        );
      }

      foreach (var entry in Directory.GetFiles(location))
      {
        var ext = Path.GetExtension(entry).ToLower();
        if (new string[] { ".gh", ".ghx" }.Contains(ext))
        {
          items.Add(
            new LinkedScript
            {
              ScriptType = ext == ".gh" ? ScriptType.GhFile : ScriptType.GhxFile,
              ScriptPath = entry,
              Name = Path.GetFileNameWithoutExtension(entry),
            }
          );
        }
      }

      return items.OrderBy(x => x.Name).ToList();
    }
  }
}
