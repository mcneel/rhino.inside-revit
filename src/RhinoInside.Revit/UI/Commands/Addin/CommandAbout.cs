using System;
using System.Reflection;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAbout : Command
  {
    public static string CommandName = "About";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandAbout, AlwaysAvailable>(CommandName, "About-icon.png", string.Empty);
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit/"));
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var details = new StringBuilder();

      var rhino = AddIn.RhinoVersionInfo;
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
          MainContent = $"Rhino.Inside Revit: {AddIn.DisplayVersion}",
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
            using (System.Diagnostics.Process.Start(@"https://www.rhino3d.com/inside/revit/")) { }
            break;
          case TaskDialogResult.CommandLink2:
            using (System.Diagnostics.Process.Start(@"https://github.com/mcneel/rhino.inside-revit/blob/1.x/LICENSE")) { }
            break;
          case TaskDialogResult.CommandLink3:
            using (System.Diagnostics.Process.Start(@"https://github.com/mcneel/rhino.inside-revit")) { }
            break;
        }
      }

      return Result.Succeeded;
    }
  }
}
