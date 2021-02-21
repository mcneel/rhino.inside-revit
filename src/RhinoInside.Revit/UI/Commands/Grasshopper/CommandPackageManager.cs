using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Grasshopper;
using Grasshopper.Kernel;
using Microsoft.Win32.SafeHandles;
using Rhino.PlugIns;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.GH.Bake;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPackageManager : GrasshopperCommand
  {
    public static string CommandName => "Package\nManager";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperPackageManager, Availability>(
        CommandName,
        "PackageManager-icon.png",
        "Shows Rhino/Grasshopper Package Manager"
      );
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://www.food4rhino.com/"));
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      Rhinoceros.RunCommandPackageManager();
      return OnCommandCompleted(Result.Succeeded);
    }

    /// <summary>
    /// Find packages that contain Grasshopper scripts for Rhino.Inside.Revit
    /// </summary>
    public static List<ScriptPkg> GetInstalledScriptPackages()
    {
      var pkgs = new List<ScriptPkg>();
      var pkgLocations = new List<DirectoryInfo>();
      pkgLocations.AddRange(Rhino.Runtime.HostUtils.GetActivePlugInVersionFolders(false));
      pkgLocations.AddRange(Rhino.Runtime.HostUtils.GetActivePlugInVersionFolders(true));
      foreach (var dirInfo in pkgLocations)
        if (GetInstalledScriptPackage(dirInfo.FullName) is ScriptPkg pkg)
          pkgs.Add(pkg);
      return pkgs;
    }

    /// <summary>
    /// Get script package on given location if exists
    /// </summary>
    public static ScriptPkg GetInstalledScriptPackage(string location)
    {
      // grab the name from the package directory
      // TODO: Could use the Yak core api to grab the package objects
      var version = Path.GetFileName(location);
      var target = Path.GetDirectoryName(location);
      var pkgName = Path.GetFileName(target);
      // Looks for Rhino.Inside/Revit/ or Rhino.Inside/Revit/x.x insdie the package
      var pkgAddinContents = Path.Combine(location, Addin.AddinName, "Revit");
      var pkgAddinSpecificContents = Path.Combine(pkgAddinContents, $"{Addin.Version.Major}.0");
      // load specific scripts if available, otherwise load for any Rhino.Inside.Revit
      if (new List<string> {
            pkgAddinSpecificContents,
            pkgAddinContents }.Where(d => Directory.Exists(d)).FirstOrDefault() is string pkgContentsDir)
      {
        return new ScriptPkg { Name = $"{pkgName} ({version})", Location = pkgContentsDir };
      }
      return null;
    }

    #region Events
    public static event EventHandler<Result> CommandCompleted;

    public Result OnCommandCompleted(Result res)
    {
      CommandCompleted?.Invoke(this, res);
      return res;
    }
    #endregion
  }
}
