using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn
{
  class RhinocerosDistribution
  {
    public RhinocerosDistribution(int majorVersion) => MajorVersion = majorVersion;

    public readonly int MajorVersion;
    public bool Available => GetRhinoVersionInfo()?.Major == MajorVersion;

    string RegistryPath => $@"SOFTWARE\McNeel\Rhinoceros\{MajorVersion}.0";

    string SystemDir =>
#if DEBUG
      Microsoft.Win32.Registry.GetValue($@"HKEY_CURRENT_USER\{RegistryPath}-WIP-Developer-Debug-trunk\Install", "Path", null) as string ??
#endif
      Microsoft.Win32.Registry.GetValue($@"HKEY_LOCAL_MACHINE\{RegistryPath}\Install", "Path", null) as string;

    public string BuildType =>
#if DEBUG
      Microsoft.Win32.Registry.GetValue($@"HKEY_CURRENT_USER\{RegistryPath}-WIP-Developer-Debug-trunk\Install", "BuildType", null) as string ??
#endif
      Microsoft.Win32.Registry.GetValue($@"HKEY_LOCAL_MACHINE\{RegistryPath}\Install", "BuildType", null) as string;

    public string DisplayBuildType => BuildType?.ToUpperInvariant() == "COMMERCIAL" ? string.Empty : BuildType;

    string RhinoExePath => Path.Combine(SystemDir, "Rhino.exe");

    public Version GetRhinoVersionInfo()
    {
      var RhinoVersionInfo = File.Exists(RhinoExePath) ? FileVersionInfo.GetVersionInfo(RhinoExePath) : null;
      return new Version
      (
        RhinoVersionInfo?.FileMajorPart ?? 0,
        RhinoVersionInfo?.FileMinorPart ?? 0,
        RhinoVersionInfo?.FileBuildPart ?? 0,
        RhinoVersionInfo?.FilePrivatePart ?? 0
      );
    }
  }

  public class Loader : IExternalApplication
  {
    class OwnerWindow : IWin32Window
    {
      public OwnerWindow(IntPtr hWnd) => Handle = hWnd;
      public IntPtr Handle { get; private set; }
    }

    internal static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
    internal static readonly Guid AddInId = new Guid($"02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74");
    IExternalApplication _ExternalApplication;

    Result IExternalApplication.OnStartup(UIControlledApplication controlledApplication)
    {
      if (controlledApplication.ActiveAddInId.GetGUID() != AddInId)
        return Result.Failed;

      var assembly = Assembly.GetExecutingAssembly();
      var directory = Path.GetDirectoryName(assembly.Location);

      if (GetDistribution() is RhinocerosDistribution distribution)
      {
        try
        {
          var path = Path.Combine(directory, $"R{distribution.MajorVersion}", "RhinoInside.Revit.AddIn.dll");
          var objectHandle = Activator.CreateInstanceFrom(path, typeof(Loader).FullName);
          _ExternalApplication = objectHandle?.Unwrap() as IExternalApplication;
        }
        catch { }
      }

      return _ExternalApplication?.OnStartup(controlledApplication) ?? Result.Failed;
    }

    Result IExternalApplication.OnShutdown(UIControlledApplication controlledApplication)
    {
      return _ExternalApplication?.OnShutdown(controlledApplication) ?? Result.Failed;
    }

    RhinocerosDistribution GetDistribution()
    {
      var distributions = new RhinocerosDistribution[]
      {
        new RhinocerosDistribution(8),
        new RhinocerosDistribution(7),
      };

      var available = distributions.Where(x => x.Available).ToArray();
      if (available.Length == 0) return distributions[0];

      using
      (
        var taskDialog = new TaskDialog("Loading…")
        {
          Id = typeof(Loader).FullName,
          MainIcon = TaskDialogIcon.TaskDialogIconInformation,
          TitleAutoPrefix = true,
          AllowCancellation = false,
          MainInstruction = "Looks like you have many supported Rhino versions installed.",
          MainContent = "Please pick which one you want to use.",
          //VerificationText = "Do not show again"
        }
      )
      {
        for (int d = 0; d < 4 && d < available.Length; d++)
        {
          var distribution = available[d];
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1 + d, $"Rhino {distribution.MajorVersion} {distribution.DisplayBuildType}", $"{distribution.GetRhinoVersionInfo()}");
        }

        taskDialog.DefaultButton = TaskDialogResult.CommandLink1;

        var result = taskDialog.Show();

        if (TaskDialogResult.CommandLink1 <= result && result <= TaskDialogResult.CommandLink4)
          return distributions[result - TaskDialogResult.CommandLink1];
      }

      return null;
    }
  }
}
