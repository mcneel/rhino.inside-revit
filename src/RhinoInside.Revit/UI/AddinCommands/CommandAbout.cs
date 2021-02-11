using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using RhinoInside.Revit.Native;
using RhinoInside.Revit.Settings;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAbout : Command
  {
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      const string CommandName = "About";

      var buttonData = NewPushButtonData<CommandAbout, AlwaysAvailable>(CommandName, "Resources.About-icon.png", "");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit/beta/"));
      }
    }

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
