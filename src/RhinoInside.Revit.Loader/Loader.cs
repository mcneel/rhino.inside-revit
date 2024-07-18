using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn
{
  public sealed class Loader : IExternalApplication
  {
    internal static readonly Guid AddInId = new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74");
    IExternalApplication _ExternalApplication;

    Result IExternalApplication.OnStartup(UIControlledApplication controlledApplication)
    {
      if (controlledApplication.ActiveAddInId.GetGUID() != AddInId)
        return Result.Failed;

      var assembly = Assembly.GetExecutingAssembly();
      var directory = Path.GetDirectoryName(assembly.Location);

      if (PickDistribution() is Distribution distribution)
      {
        try
        {
          var path = Path.Combine(directory, $"R{distribution.MajorVersion}", "RhinoInside.Revit.AddIn.dll");
          var objectHandle = Activator.CreateInstanceFrom(path, typeof(Loader).FullName);
          _ExternalApplication = objectHandle?.Unwrap() as IExternalApplication;

          Distribution.CurrentKey = distribution.RegistryKey;
        }
        catch { }
      }

      return _ExternalApplication?.OnStartup(controlledApplication) ?? Result.Cancelled;
    }

    Result IExternalApplication.OnShutdown(UIControlledApplication controlledApplication)
    {
      return _ExternalApplication?.OnShutdown(controlledApplication) ?? Result.Cancelled;
    }

    static Distribution PickDistribution()
    {
      var distributions = new Distribution[]
      {
        new Distribution(8),
#if NETFRAMEWORK
        new Distribution(7),
#endif
#if DEBUG
        new Distribution(9, dev: true),
        new Distribution(8, dev: true),
#if NETFRAMEWORK
        new Distribution(7, dev: true),
#endif
#endif
      };

      var currentKey = Distribution.CurrentKey;
      var available = distributions.Where(x => x.Available && (currentKey is null || x.RegistryKey == currentKey)).ToArray();

      switch (available.Length)
      {
        case 0:
          using
          (
            var taskDialog = new TaskDialog("Install Rhino")
            {
              Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.InstallRhino",
              MainIcon = TaskDialogIcon.TaskDialogIconWarning,
              AllowCancellation = true,
              MainInstruction = "Rhino is not available",
              MainContent = "Rhino.Inside Revit requires Rhino 7 or 8 installed on the computer.",
              CommonButtons = TaskDialogCommonButtons.Close
            }
          )
          {
            taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Download Rhino…");
            if (taskDialog.Show() == TaskDialogResult.CommandLink1)
            {
              try
              {
                Process.Start(new ProcessStartInfo($@"https://www.rhino3d.com/download/rhino/")
                {
                  UseShellExecute = true,
                  WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                });
              }
              catch { }
            }
          }

          break;

        case 1:
          return available[0];

        default:
          using
          (
            var taskDialog = new TaskDialog("Loading…")
            {
              Id = typeof(Distribution).FullName,
              MainIcon = TaskDialogIcon.TaskDialogIconInformation,
              TitleAutoPrefix = true,
              AllowCancellation = false,
              MainInstruction = "Looks like you have many supported Rhino versions installed.",
              MainContent = "Please pick which one you want to use in this Revit session.",
              //VerificationText = "Do not show again"
            }
          )
          {
            for (int d = 0; d < 4 && d < available.Length; d++)
            {
              var distribution = available[d];
              taskDialog.AddCommandLink
              (
                TaskDialogCommandLinkId.CommandLink1 + d,
                distribution.VersionInfo.FileDescription,
                $"{distribution.ExeVersion()}"
              );
            }

            taskDialog.DefaultButton = TaskDialogResult.CommandLink1;

            var result = taskDialog.Show();

            if (TaskDialogResult.CommandLink1 <= result && result <= TaskDialogResult.CommandLink4)
              return available[result - TaskDialogResult.CommandLink1];
          }
          break;
      }

      return null;
    }
  }
}
