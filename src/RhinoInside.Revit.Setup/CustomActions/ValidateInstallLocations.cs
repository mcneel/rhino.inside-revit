using System.IO;
using Microsoft.Deployment.WindowsInstaller;

namespace RhinoInside.Revit.Setup
{
  public static partial class Actions
  {
    [CustomAction]
    public static ActionResult ValidateInstallLocations(Session session)
    {
      if (!session.EvaluateCondition("Installed"))
      {
        for (int version = 2018; version <= 2027; ++ version)
          ValidateInstallLocation(session, $"REVIT{version}_INSTALLLOCATION");
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
