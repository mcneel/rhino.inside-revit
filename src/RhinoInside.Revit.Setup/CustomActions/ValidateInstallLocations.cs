using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using OS = System.Environment;

namespace RhinoInside.Revit.Setup
{
  public static partial class Actions
  {
    [CustomAction]
    public static ActionResult ValidateInstallLocations(Session session)
    {
      if (!session.EvaluateCondition("Installed"))
      {
        ValidateInstallLocation(session, "REVIT2018_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2019_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2020_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2021_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2022_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2023_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2024_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2025_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2026_INSTALLLOCATION");
        ValidateInstallLocation(session, "REVIT2027_INSTALLLOCATION");
      }

      return ActionResult.Success;
    }

    private static void ValidateInstallLocation(Session session, string property)
    {
      //System.Diagnostics.Debugger.Launch();

      var installLocation = session[property] ?? string.Empty;

      // If installLocation is relative we assume is under "%PROGRAMFILES%\Autodesk"
      if (installLocation.Length < 3 || installLocation.Substring(1, 2) != ":\\")
        installLocation = Path.Combine(session["ProgramFiles64Folder"], "Autodesk", installLocation);

      // If Revit.exe is not at installLocation we assume Revit is not installed
      if (File.Exists(Path.Combine(installLocation, "Revit.exe")))
        session[property] = installLocation;
      else
        session[property] = string.Empty;
    }
  }
}
