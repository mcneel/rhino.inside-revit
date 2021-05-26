using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RhinoInside.Revit.Settings;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandAddinOptions : Command
  {
    public static string CommandName = "Options";
    public static string CommandTooltip = "Open Rhino.Inside.Revit Options Window";

    static ReleaseInfo LatestReleaseInfo = null;

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandAddinOptions, AlwaysAvailable>
      (
        name: CommandName,
        iconName: "Options.png",
        tooltip: CommandTooltip,
        url: "reference/rir-interface#rhinoinsiderevit-options"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        // setup button
        StoreButton(CommandName, pushButton);

        // disable the button if options are readonly
        pushButton.Enabled = !AddinOptions.IsReadOnly && AddIn.IsEtoFrameworkReady;

        if (AddIn.StartupMode == AddInStartupMode.Disabled)
        {
          pushButton.Enabled = false;
          pushButton.ToolTip = "Add-In is disabled";
        }
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      // try opening options window
      if (!AddinOptions.IsReadOnly)
      {
        var optWindow = new AddInOptionsDialog(data.Application);
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
    public static void NotifyUpdateAvailable(ReleaseInfo releaseInfo)
    {
      // button gets deactivated if options are readonly
      if (!AddinOptions.IsReadOnly)
      {
        if (RestoreButton(CommandName) is PushButton button)
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
      if (RestoreButton(CommandName) is PushButton button)
      {
        button.ClearHighlight();
        button.ToolTip = CommandTooltip;
      }
      LatestReleaseInfo = null;
    }
  }
}
