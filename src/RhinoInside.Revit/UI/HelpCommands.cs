using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;

namespace RhinoInside.Revit.UI
{
  abstract class HelpCommand : Command
  {
    static protected PulldownButton helpButton = null;
    internal static void CreateUI(RibbonPanel ribbonPanel)
    {
      if (ribbonPanel.AddItem(new PulldownButtonData("cmdRhinoInside.Help", "Help")) is PulldownButton pullDownButton)
      {
        helpButton = pullDownButton;
        helpButton.Image = ImageBuilder.BuildImage("?");
        helpButton.LargeImage = ImageBuilder.BuildLargeImage("?");

        AddPushButton<CommandSampleFiles,       AllwaysAvailable> (helpButton, "Sample files",      "Opens sample files folder");
        AddPushButton<CommandAPIDocs,           AllwaysAvailable> (helpButton, "APIDocs",           "Opens apidocs.co website");
        AddPushButton<CommandTheBuildingCoder,  AllwaysAvailable> (helpButton, "TheBuildingCoder",  "Opens thebuildingcoder.typepad.com website");
        helpButton.AddSeparator();
        AddPushButton<CommandRhinoDevDocs,      AllwaysAvailable> (helpButton, "Rhino Dev Docs",    "Opens developer.rhino3d.com website");
        AddPushButton<CommandDiscourse,         AllwaysAvailable> (helpButton, "McNeel Discourse",  "Opens discourse.mcneel.com website");
        helpButton.AddSeparator();
        AddPushButton<CommandCheckForUpdates,   AllwaysAvailable> (helpButton, "Updates",           "Checks if there are updates in GitHub");
        AddPushButton<CommandAbout,             AllwaysAvailable> (helpButton, "About…",            "Shows Rhino.Inside Revit version information");
      }

      CommandCheckForUpdates.CheckUpdates();
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandSampleFiles : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
#if DEBUG
      var location = Path.Combine(Addin.SourceCodePath, "Samples");
#else
      var location = Path.Combine
      (
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "McNeel",
        "Rhino.Inside",
        "Revit",
        $"{Addin.Version.Major}.{Addin.Version.Minor}",
        "Samples"
      );
#endif
      using (System.Diagnostics.Process.Start(location)) { }

      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAPIDocs : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://www.apidocs.co/apps/")) { }

      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandTheBuildingCoder : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://thebuildingcoder.typepad.com/")) { }

      return Result.Succeeded;
    }
  }
  
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoDevDocs : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://developer.rhino3d.com/")) { }

      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandDiscourse : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      using (System.Diagnostics.Process.Start(@"https://discourse.mcneel.com/c/rhino-inside/Revit")) { }

      return Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandCheckForUpdates : HelpCommand
  {
    static public int CheckUpdates(bool quiet = true, bool forceFetch = false)
    {
#if DEBUG
      int retCode = -1;
      using (var powerShell = PowerShell.Create())
      {
        powerShell.AddScript(string.Format(@"Set-Location {0}", Addin.SourceCodePath));
        powerShell.AddScript(@"git rev-parse --absolute-git-dir");
        var gitdir = powerShell.Invoke();
        if (powerShell.HadErrors)
          return -1;

        powerShell.AddScript(@"git rev-parse --abbrev-ref HEAD");
        var currentBranch = powerShell.Invoke();
        if (currentBranch.Count == 1)
        {
          using
          (
            var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
            {
              Title = "Updates",
              MainIcon = External.UI.TaskDialogIcons.IconInformation,
              TitleAutoPrefix = true,
              AllowCancellation = true,
              FooterText = Addin.SourceCodePath
            }
          )
          {
            // In quiet mode fetch just once per hour.
            // if not in quiet mode forceFetch it's OK since the TaskDialog will stop execution for some time.
            {
              powerShell.Streams.Error.Clear();
              string gitPath = Path.GetFullPath(Path.Combine(gitdir[0].ToString(), "FETCH_HEAD"));

              if ((!quiet && forceFetch) || (DateTime.UtcNow - File.GetLastWriteTimeUtc(gitPath)).Hours > 0)
              {
                powerShell.AddScript(@"git fetch");
                powerShell.Invoke();
              }
            }

            if (forceFetch && powerShell.HadErrors)
            {
              taskDialog.MainIcon = External.UI.TaskDialogIcons.IconError;
              taskDialog.MainInstruction = "Failed to fetch changes from the repository";

              foreach (var f in powerShell.Streams.Error)
              {
                var line = f.ToString();
                taskDialog.ExpandedContent += line + "\n";
              }
            }
            else
            {
              powerShell.AddScript(string.Format(@"git log HEAD..origin/{0} --oneline .", currentBranch[0].ToString()));
              var results = powerShell.Invoke();

              retCode = results.Count;
              if (retCode == 0)
              {
                taskDialog.MainInstruction = "You are up to date!!";
              }
              else
              {
                taskDialog.MainInstruction = string.Format("There are {0} changes in the repository", results.Count);

                foreach (var result in results.Take(12))
                {
                  var line = result.ToString();
                  var comment = line.Substring(line.IndexOf(' ') + 1);
                  taskDialog.ExpandedContent += "- " + comment + "\n";
                }
              }
            }

            if (!quiet)
              taskDialog.Show();
          }
        }
      }

      if (helpButton != null)
      {
        helpButton.LargeImage = retCode > 0 ? ImageBuilder.BuildLargeImage(retCode.ToString(), System.Windows.Media.Colors.DarkRed) : ImageBuilder.BuildLargeImage("?");
        helpButton.ToolTip = retCode > 0 ? string.Format("There are {0} changes in the repository", retCode) : string.Empty;
      }

      return retCode;
#else
      Addin.CheckIsExpired(quiet);

      if (helpButton != null)
      {
        helpButton.LargeImage = Addin.DaysUntilExpiration <= 15 ? ImageBuilder.BuildLargeImage(Addin.DaysUntilExpiration.ToString(), System.Windows.Media.Colors.DarkRed) : ImageBuilder.BuildLargeImage("?");
        helpButton.ToolTip = Addin.DaysUntilExpiration > 1 ? string.Format("This WIP build expires in {0} days", Addin.DaysUntilExpiration) : "This WIP build has expired";
      }

      return (Addin.DaysUntilExpiration < 1) ? 1 : 0;
#endif
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      return CheckUpdates(false, true) < 0 ? Result.Failed : Result.Succeeded;
    }
  }

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAbout : HelpCommand
  {
    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var details = new StringBuilder();

      var rhino = Addin.RhinoVersionInfo;
      details.AppendLine($"Rhino: {rhino.ProductVersion} ({rhino.FileDescription})");

      var revit = data.Application.Application;
#if REVIT_2019
      details.AppendLine($"Revit: {revit.SubVersionNumber} ({revit.VersionBuild})");
#else
      details.AppendLine($"Revit: {revit.VersionNumber} ({revit.VersionBuild})");
#endif

      details.AppendLine($"CLR: {ErrorReport.CLRVersion}");
      details.AppendLine($"OS: {Environment.OSVersion}");

      using
      (
        var taskDialog = new TaskDialog("About")
        {
          Id = MethodBase.GetCurrentMethod().DeclaringType.FullName,
          MainIcon = External.UI.TaskDialogIcons.IconInformation,
          TitleAutoPrefix = true,
          AllowCancellation = true,
          MainInstruction = $"Rhino.Inside© for Revit",
          MainContent = $"Rhino.Inside Revit: {Addin.DisplayVersion}",
          ExpandedContent = details.ToString(),
          CommonButtons = TaskDialogCommonButtons.Ok,
          DefaultButton = TaskDialogResult.Ok,
          FooterText = "Press CTRL+C to copy this information to Clipboard"
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Web site");
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Read license");
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "See source code");

        switch (taskDialog.Show())
        {
          case TaskDialogResult.CommandLink1:
            using (System.Diagnostics.Process.Start(@"https://www.rhino3d.com/inside/revit/beta/")) { }
            break;
          case TaskDialogResult.CommandLink2:
            using (System.Diagnostics.Process.Start(@"https://github.com/mcneel/rhino.inside-revit/blob/master/LICENSE")) { }
            break;
          case TaskDialogResult.CommandLink3:
            using (System.Diagnostics.Process.Start(@"https://github.com/mcneel/rhino.inside-revit/tree/master/src")) { }
            break;
        }
      }

      return Result.Succeeded;
    }
  }
}
