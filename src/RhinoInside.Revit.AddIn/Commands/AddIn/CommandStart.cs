using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using static RhinoInside.Revit.Diagnostics;
using DB = Autodesk.Revit.DB;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandStart : Command
  {
    static RibbonPanel rhinoPanel;
    static RibbonPanel grasshopperPanel;

    public static string CommandName => "Start";

    // determine which RIR icon to use
    public static string CommandIcon => Distribution.Updater.ActiveChannel?.IsStable == false ?
      "RIR-WIP-logo.png" :
      "RIR-logo.png";

    /// <summary>
    /// Initialize the Ribbon tab and first panel
    /// </summary>
    /// <param name="uiCtrlApp"></param>
    public static void CreateUI(UIControlledApplication uiCtrlApp)
    {
      CreateMainPanel(uiCtrlApp);

      // Add the rest of the UI.
      // They will all be 'unavailable' (set by the Availability type) since
      // RiR is not loaded yet. This allows keyboard shortcuts to be assigned.
      if (!Properties.AddInOptions.Session.CompactTab)
      {
        Core.Host.ActivateRibbonTab(Core.Product);

        if (CreateRhinocerosPanel())
          CreateGrasshopperPanel();
      }
    }

    static void SetupButton(PushButton pushButton)
    {
      if (Core.RhinoVersionInfo is FileVersionInfo rhInfo)
      {
        pushButton.ToolTip = $"Loads {rhInfo.ProductName} inside this Revit session";
        pushButton.LongDescription =
          $"Rhino: {rhInfo.ProductVersion} ({rhInfo.FileDescription}){Environment.NewLine}" +
          $"Rhino.Inside: {Core.DisplayVersion}{Environment.NewLine}{rhInfo.LegalCopyright}";
      }

      if (Core.StartupMode == CoreStartupMode.Disabled)
      {
        pushButton.Enabled = false;
        pushButton.ToolTip = "Add-In is disabled";
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      if
      (
        (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
        (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
      )
        return ErrorReport.ShowLoadError();

      switch (Core.CurrentStatus)
      {
        case Core.Status.Ready:
          if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            return Rhinoceros.RunCommandAbout();

          if
          (
            Rhinoceros.MainWindow.Visible ||
            Rhinoceros.MainWindow.ActivePopup?.IsInvalid == false
          )
          {
            Rhinoceros.MainWindow.BringToFront();
          }
          else
          {
            Core.Host.ActivateRibbonTab(Core.Product);
          }

          return Result.Succeeded;

        case Core.Status.Available:
          return Start();

        case Core.Status.Unavailable:
          return Core.CheckSetup();
      }

      return Result.Failed;
    }

    internal static Result Start()
    {
      var result = Result.Failed;
      var button = RestoreButton(CommandName);

      switch (result = Revit.OnStartup())
      {
        case Result.Succeeded:
          // Update Rhino button Tooltip
          button.ToolTip = $"Restores previously visible Rhino windows on top of Revit window";
          button.LongDescription = $"Use CTRL key to open a Rhino model";
          button.ShowText(false);

          if (Properties.AddInOptions.Session.CompactTab)
          {
            Core.Host.CreateRibbonTab(Core.Product);

            // Register UI on Revit
            if (CreateRhinocerosPanel())
              CreateGrasshopperPanel();
          }

          CreateScriptsPanel();
          UpdateRibbonCompact();
          break;

        case Result.Cancelled:
          button.Enabled = false;

          if (Core.CurrentStatus == Core.Status.Unavailable)
            button.ToolTip = "Rhino.Inside failed to found a valid copy of Rhino installed.";
          else if (Core.CurrentStatus == Core.Status.Obsolete)
            button.ToolTip = "Rhino.Inside has expired.";
          else
            button.ToolTip = "Rhino.Inside load was cancelled.";

          break;

        case Result.Failed:
          button.Enabled = false;
          button.ToolTip = "Rhino.Inside failed to load.";
          break;
      }

      return (result == Result.Failed) ? ErrorReport.ShowLoadError() : result;
    }

    #region UI Panels and Buttons
    static void CreateMainPanel(UIControlledApplication uiCtrlApp)
    {
      RibbonPanel ribbonPanel;

      void CreateStartButton(string tabName)
      {
        var buttonData = NewPushButtonData<CommandStart, AvailableEvenObsolete>(CommandName, CommandIcon, "");
        if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
        {
          StoreButton(CommandName, pushButton);
          SetupButton(pushButton);

          if (Core.CurrentStatus >= Core.Status.Available && Core.StartupMode != CoreStartupMode.Disabled)
          {
            try
            {
              if (Settings.KeyboardShortcuts.RegisterShortcut
              (
                uiCtrlApp.ControlledApplication,
                tabName, ribbonPanel.Name,
                typeof(CommandStart).Name,
                CommandName, "R#Ctrl+R")
              )
                External.ActivationGate.Exit += ShowShortcutHelp;
            }
            catch (Exception e) { Core.ReportException(e, uiCtrlApp); }
          }
        }

        // add listener for ui compact changes
        Properties.AddInOptions.CompactRibbonChanged += AddinOptions_CompactRibbonChanged;
        Properties.AddInOptions.UpdateChannelChanged += AddinOptions_UpdateChannelChanged;
      }

      if (Properties.AddInOptions.Session.CompactTab)
      {
        ribbonPanel = uiCtrlApp.CreateRibbonPanel(Core.Product);

        // Add launch RhinoInside push button,
        CreateStartButton("Add-Ins");

        // AddIn Options.
        CommandAddInOptions.CreateUI(ribbonPanel);
      }
      else
      {
        uiCtrlApp.CreateRibbonTab(Core.Product);
        ribbonPanel = uiCtrlApp.CreateRibbonPanel(Core.Product, "More");

        // Add launch RhinoInside push button.
        CreateStartButton(Core.Product);
      }

      // add slideout and the rest of the buttons
      ribbonPanel.AddSlideOut();

      // about and help links
      CommandAbout.CreateUI(ribbonPanel);
      CommandGuides.CreateUI(ribbonPanel);
      CommandForums.CreateUI(ribbonPanel);
      CommandHelpLinks.CreateUI(ribbonPanel);

      if (!Properties.AddInOptions.Session.CompactTab)
      {
        ribbonPanel.AddSeparator();
        CommandAddInOptions.CreateUI(ribbonPanel);
      }
    }

    static bool CreateRhinocerosPanel()
    {
      if (!AssemblyResolver.References.ContainsKey("RhinoCommon"))
        return false;

      rhinoPanel = Core.Host.CreateRibbonPanel(Core.Product, Core.RhinoVersionInfo?.ProductName ?? "Rhinoceros");

      CommandRhino.CreateUI(rhinoPanel);
      CommandRhinoOpenViewport.CreateUI(rhinoPanel);
      CommandToggleRhinoPreview.CreateUI(rhinoPanel);
      CommandPython.CreateUI(rhinoPanel);

      rhinoPanel.AddSlideOut();
      CommandImport.CreateUI(rhinoPanel);
      CommandRhinoOptions.CreateUI(rhinoPanel);

      return true;
    }

    static void CreateGrasshopperPanel()
    {
      if (!AssemblyResolver.References.ContainsKey("Grasshopper"))
        return;

      grasshopperPanel = Core.Host.CreateRibbonPanel(Core.Product, "Grasshopper");
      CommandGrasshopper.CreateUI(grasshopperPanel);
      CommandGrasshopperPreview.CreateUI(grasshopperPanel);
      CommandGrasshopperSolver.CreateUI(grasshopperPanel);
      CommandGrasshopperRecompute.CreateUI(grasshopperPanel);
      //CommandGrasshopperCaptureElements.CreateUI(grasshopperPanel);
      CommandGrasshopperReleaseElements.CreateUI(grasshopperPanel);
      grasshopperPanel.AddSeparator();
      CommandGrasshopperPlayer.CreateUI(grasshopperPanel);
      grasshopperPanel.AddSlideOut();
      CommandGrasshopperPackageManager.CreateUI(grasshopperPanel);
      CommandGrasshopperFolders.CreateUI(grasshopperPanel);
      CommandGrasshopperBake.CreateUI(grasshopperPanel);
    }

    static void CreateScriptsPanel()
    {
      if (!AssemblyResolver.References.ContainsKey("Grasshopper"))
        return;

      // Script Packages UI
      LinkedScripts.CreateUI(new RibbonHandler(Core.Host));
    }
    #endregion

    #region Shortcuts
    static void ShowShortcutHelp(object sender, EventArgs e)
    {
      if (sender is IExternalCommand)
      {
        External.ActivationGate.Exit -= ShowShortcutHelp;

        using
        (
          var taskDialog = new TaskDialog("New Shortcut")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
            MainIcon = UIX.TaskDialogIcons.IconInformation,
            TitleAutoPrefix = true,
            AllowCancellation = true,
            MainInstruction = $"Keyboard shortcut 'R' is now assigned to Rhino",
            MainContent = $"You can use R key to restore previously visible Rhino windows over Revit window every time you need them.",
            FooterText = "This is a one time message",
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Customize keyboard shortcuts…");
          if (taskDialog.Show() == TaskDialogResult.CommandLink1)
          {
            Revit.ActiveUIApplication.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.KeyboardShortcuts));
          }
        }
      }
    }
    #endregion

    #region Update
    private static void AddinOptions_CompactRibbonChanged(object sender, EventArgs e) => UpdateRibbonCompact();

    static void UpdateRibbonCompact()
    {
      // collapse panel if in compact mode
      if (Properties.AddInOptions.Current.CompactRibbon)
      {
        rhinoPanel?.Collapse(Core.Product);
        grasshopperPanel?.Collapse(Core.Product);
      }
      else
      {
        rhinoPanel?.Expand(Core.Product);
        grasshopperPanel?.Expand(Core.Product);
      }
    }

    private static void AddinOptions_UpdateChannelChanged(object sender, EventArgs e)
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        button.Image = LoadRibbonButtonImage(CommandIcon, true);
        button.LargeImage = LoadRibbonButtonImage(CommandIcon);
      }
    }

    public static void NotifyUpdateAvailable(Distribution.ReleaseInfo releaseInfo)
    {
      // button gets deactivated if options are readonly
      if (!Properties.AddInOptions.IsReadOnly)
      {
        if (RestoreButton(CommandName) is PushButton button)
        {
          ClearUpdateNotifiy();
          button.Highlight();
          button.ToolTip = "New Release Available for Download!\n"
                         + $"Version: {releaseInfo.Version}\n"
                         + button.ToolTip;
        }
      }
    }

    public static void ClearUpdateNotifiy()
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        button.ClearHighlight();
        // init resets the tooltip to default
        SetupButton(button);
      }
    }
    #endregion
  }
}
