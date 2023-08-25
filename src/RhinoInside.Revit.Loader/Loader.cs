using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn
{
  public class Loader : IExternalApplication
  {
    internal static readonly Guid AddInId = new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74");
    IExternalApplication _ExternalApplication;

    Result IExternalApplication.OnStartup(UIControlledApplication controlledApplication)
    {
      if (controlledApplication.ActiveAddInId.GetGUID() != AddInId)
        return Result.Failed;

      var assembly = Assembly.GetExecutingAssembly();
      var directory = Path.GetDirectoryName(assembly.Location);

      if (PickDistribution(7, 8) is Distribution distribution)
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

      return _ExternalApplication?.OnStartup(controlledApplication) ?? Result.Failed;
    }

    Result IExternalApplication.OnShutdown(UIControlledApplication controlledApplication)
    {
      return _ExternalApplication?.OnShutdown(controlledApplication) ?? Result.Failed;
    }

    static Distribution PickDistribution(int min, int max)
    {
      var distributions = new Distribution[]
      {
        new Distribution(7),
        new Distribution(8),
#if DEBUG
        new Distribution(7, dev: true),
        new Distribution(8, dev: true),
#endif
      };

      var currentKey = Distribution.CurrentKey;
      var available = distributions.Where(x => x.Available && (currentKey is null || x.RegistryKey == currentKey)).ToArray();
      if (available.Length == 0) return distributions[0];
      if (available.Length == 1) return available[0];

      using
      (
        var taskDialog = new TaskDialog("Loading…")
        {
          Id = typeof(Distribution).FullName,
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
          return distributions[result - TaskDialogResult.CommandLink1];
      }

      return null;
    }
  }
}
