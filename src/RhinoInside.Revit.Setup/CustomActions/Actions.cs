using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using OS = System.Environment;

namespace RhinoInside.Revit.Setup
{
  public static class Actions
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

    [CustomAction]
    public static ActionResult WIPExpirationDateWarning(Session session)
    {
      //var message = new Record(1);
      //message.FormatString = $"buildVersion ={buildVersion}";// session["WIPExpirationDateWarning"];
      //session.Message(InstallMessage.Error, message);
      //return ActionResult.Failure;

      var buildVersion = new Version(session["ProductVersion"]).Build;
      var expirationDate = new DateTime(2000, 1, 1).AddDays(buildVersion + int.Parse(session["ProductExpirationPeriod"]));
      var daysLeft = (expirationDate - DateTime.Now).Days;

      var record = new Record(1);
      if (daysLeft < 0)
      {
        record.FormatString = "This Work-In-Progress installer expired on" + OS.NewLine +
                              $"{expirationDate:D}." + OS.NewLine + OS.NewLine +
                              "Please check for updates at 'https://www.rhino3d.com/inside/revit/'";

        session.Message(InstallMessage.Error, record);
        return ActionResult.Failure;
      }

      if (daysLeft == 0)
      {
        record.FormatString = "This Work-In-Progress installer expired today." + OS.NewLine + OS.NewLine +
                              "Please check for updates at 'https://www.rhino3d.com/inside/revit/'";

        session.Message(InstallMessage.Warning, record);
      }

      if (daysLeft > 0)
      {
        record.FormatString = $"This installer contains a Work-In-Progress version of {session["ProductName"]}." + OS.NewLine + OS.NewLine +
                              $"It will expire on {expirationDate:D}," + OS.NewLine +
                              $"{daysLeft} days from now.";

        session.Message(InstallMessage.User, record);
      }

      return ActionResult.Success;
    }
  }
}
