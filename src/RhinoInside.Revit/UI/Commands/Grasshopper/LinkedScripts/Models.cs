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
    public string Name;
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

    /// <summary>
    /// Find package
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
