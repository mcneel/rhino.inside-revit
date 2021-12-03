using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPackageManager : GrasshopperCommand
  {
    public static string CommandName => "Package\nManager";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperPackageManager, Availability>
      (
        name: CommandName,
        iconName: "PackageManager-icon.png",
        tooltip: "Shows Rhino/Grasshopper Package Manager",
        url: "https://www.food4rhino.com/"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton)
      {
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      return Rhinoceros.RunCommandPackageManager();
    }

    /// <summary>
    /// Find packages that contain Grasshopper scripts for Rhino.Inside.Revit
    /// </summary>
    public static List<ScriptPkg> GetInstalledScriptPackages()
    {
      var pkgs = new List<ScriptPkg>();
      var pkgLocations = new List<DirectoryInfo>();
      {
        pkgLocations.AddRange(Rhino.Runtime.HostUtils.GetActivePlugInVersionFolders(false));
        pkgLocations.AddRange(Rhino.Runtime.HostUtils.GetActivePlugInVersionFolders(true));
      }

      foreach (var dirInfo in pkgLocations)
      {
        if (GetInstalledScriptPackage(dirInfo.FullName) is ScriptPkg pkg)
          pkgs.Add(pkg);
      }

      return pkgs;
    }

    /// <summary>
    /// Get script package on given location if exists
    /// </summary>
    public static ScriptPkg GetInstalledScriptPackage(string location)
    {
      // grab the name from the package directory
      var pkgName = Path.GetFileName(Path.GetDirectoryName(location));

      // Looks for Rhino.Inside/Revit/ or Rhino.Inside/Revit/x.x insdie the package
      var pkgAddinContents = Path.Combine(location, Core.Product, Core.Platform);
      var pkgAddinSpecificContents = Path.Combine(pkgAddinContents, $"{Core.Version.Major}.0");

      // load specific scripts if available, otherwise load for any Rhino.Inside.Revit
      if
      (
        new string[] { pkgAddinSpecificContents, pkgAddinContents }.
        Where(d => Directory.Exists(d)).
        FirstOrDefault() is string pkgContentsDir
      )
      {
        return new ScriptPkg { Name = pkgName, Location = pkgContentsDir };
      }

      return null;
    }
  }
}
