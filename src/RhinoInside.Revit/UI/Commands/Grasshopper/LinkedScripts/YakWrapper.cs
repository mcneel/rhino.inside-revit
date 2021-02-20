using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace RhinoInside.Revit.UI
{
  public static class YakWrapper
  {
    /// <summary>
    /// type of YakClient from Yak.Core assembly
    /// </summary>
    private static Type _yakClient = null;

    /// <summary>
    /// Initialize the Yak.Core assembly. This method must be called once
    /// so all the event handlers are bound and called. If Yak.Core assembly
    /// is not found, the handlers aren't bound and they will not be called
    /// </summary>
    static YakWrapper()
    {
      if (_yakClient is null)
      {
        // load yak core api assembly
        var yakCoreDllPath = Path.Combine(Addin.SystemDir, "Yak.Core.dll");
        if (File.Exists(yakCoreDllPath))
        {
          var yak = Assembly.LoadFrom(yakCoreDllPath);
          _yakClient = yak.GetType("Yak.YakClient") ?? yak.GetType("Yak.YakClient");

          Action<string, string> addHndlr = (eventName, handlerName) => {
            EventInfo ei = _yakClient.GetEvent(eventName, BindingFlags.Public | BindingFlags.Static);
            MethodInfo mi = typeof(YakWrapper).GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Static);
            if (ei is EventInfo && mi is MethodInfo)
              ei.AddEventHandler(_yakClient, Delegate.CreateDelegate(ei.EventHandlerType, mi));
          };

          if (_yakClient is Type)
          {
            addHndlr("PackageInstalled", "OnPackageInstalled");
            addHndlr("PackageRemoved", "OnPackageRemoved");
          }
        }
      }
    }

    /// <summary>
    /// Helper method to grab string properties from event handler argument objects
    /// </summary>
    private static string GetStringProp(object obj, string propname)
    {
      if (obj.GetType().GetProperty(propname) is PropertyInfo pInfo)
        return (string) pInfo.GetValue(obj, null);
      return string.Empty;
    }

    /// <summary>
    /// Private handler method that is bound to YakClient.PackageInstalled static event
    /// </summary>
    private static void OnPackageInstalled(object sender, object args)
    {
      var location = GetStringProp(args, "Path");
      if (GetInstalledScriptPackage(location) is ScriptPkg pkg)
        PackageInstalled?.Invoke(null, pkg);
    } 

    /// <summary>
    /// Private handler method that is bound to YakClient.PackageRemoved static event
    /// </summary>
    private static void OnPackageRemoved(object sender, object args)
    {
      var location = GetStringProp(args, "Path");
      if (GetInstalledScriptPackage(location) is ScriptPkg pkg)
        PackageRemoved?.Invoke(null, pkg);
    }

    /// <summary>
    /// Package Installed Event
    /// </summary>
    public static event EventHandler<ScriptPkg> PackageInstalled;

    /// <summary>
    /// Package Removed Event
    /// </summary>
    public static event EventHandler<ScriptPkg> PackageRemoved;

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
  }
}
