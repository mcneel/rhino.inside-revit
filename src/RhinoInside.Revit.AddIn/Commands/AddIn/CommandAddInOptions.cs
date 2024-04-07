using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAddInOptions : Command
  {
    public static string CommandName = "Options";
    public static string CommandTooltip = "Open Rhino.Inside.Revit Options Window";
    static PushButton Button;

    static Deployment.ReleaseInfo LatestReleaseInfo = null;

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandAddInOptions, AlwaysAvailable>
      (
        name: CommandName,
        iconName: "Options.png",
        tooltip: CommandTooltip,
        url: "reference/rir-interface#rhinoinsiderevit-options"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        Button = pushButton;

        // disable the button if options are readonly
        pushButton.Enabled = !Properties.AddInOptions.IsReadOnly && AssemblyResolver.References.ContainsKey("Eto");

        if (Core.StartupMode == CoreStartupMode.Disabled)
        {
          pushButton.Enabled = false;
          pushButton.ToolTip = "Add-In is disabled";
        }
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      // try opening options window
      if (!Properties.AddInOptions.IsReadOnly)
      {
        var optWindow = new Forms.AddInOptionsDialog(data.Application);
        if (LatestReleaseInfo != null)
        {
          optWindow.UpdatesPanel.SetReleaseInfo(LatestReleaseInfo);
          optWindow.ActivateUpdatesTab();
        }
        optWindow.ShowModal();
      }
      else
        TaskDialog.Show(CommandName, "Contact your system admin to change the options");

      return Result.Succeeded;
    }

    /// <summary>
    /// Mark button with highlighter dot using Autodesk.Windows api
    /// </summary>
    public static void NotifyUpdateAvailable(Deployment.ReleaseInfo releaseInfo)
    {
      // button gets deactivated if options are readonly
      if (!Properties.AddInOptions.IsReadOnly)
      {
        if (Button is PushButton button)
        {
          ClearUpdateNotifiy();
          button.Highlight();
          button.ToolTip = "New Release Available for Download!\n"
                         + $"Version: {releaseInfo.Version}\n"
                         + CommandTooltip;
        }
        LatestReleaseInfo = releaseInfo;
      }
    }

    public static void ClearUpdateNotifiy()
    {
      if (Button is PushButton button)
      {
        button.ClearHighlight();
        button.ToolTip = CommandTooltip;
      }
      LatestReleaseInfo = null;
    }
  }
}
