using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using RhinoInside.Revit.External.UI.Extensions;
using RhinoInside.Revit.Settings;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandStart : Command
  {
    static RibbonPanel rhinoPanel;
    static RibbonPanel grasshopperPanel;

    public static string CommandName => "Start";
    public static string CommandIcon => AddinUpdater.ActiveChannel.IsStable? "RIR-logo.png" : "RIR-WIP-logo.png";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandStart, AvailableWhenNotObsolete>(CommandName, CommandIcon, "");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton(CommandName, pushButton);
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit/beta/"));

        SetupButton(pushButton);

        if (AddIn.StartupMode != AddinStartupMode.Disabled)
        {
          if (Settings.KeyboardShortcuts.RegisterDefaultShortcut("Add-Ins", ribbonPanel.Name, typeof(CommandStart).Name, CommandName, "R#Ctrl+R"))
            External.ActivationGate.Exit += ShowShortcutHelp;
        }
      }

      // add listener for ui compact changes
      AddinOptions.CompactRibbonChanged += AddinOptions_CompactRibbonChanged;
      AddinOptions.UpdateChannelChanged += AddinOptions_UpdateChannelChanged;
    }

    static void SetupButton(PushButton pushButton)
    {
      if (AddIn.RhinoVersionInfo is FileVersionInfo rhInfo)
      {
        // try-catch to capture any property getters failing
        try
        {
          pushButton.ToolTip =
            $"Loads {rhInfo.ProductName} inside this Revit session";

          pushButton.LongDescription =
            $"Rhino: {rhInfo.ProductVersion} ({rhInfo.FileDescription}){Environment.NewLine}" +
            $"Rhino.Inside: {AddIn.DisplayVersion}{Environment.NewLine}{rhInfo.LegalCopyright}";

          if (AddIn.StartupMode == AddinStartupMode.Disabled)
          {
            pushButton.Enabled = false;
            pushButton.ToolTip = "Addin Disabled";
          }
        }
        catch (Exception) { }
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, Autodesk.Revit.DB.ElementSet elements)
    {
      if
      (
        (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
        (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
      )
        return ShowLoadError(data);

      if (AddIn.CurrentStatus == AddIn.Status.Ready)
      {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
          return Rhinoceros.RunCommandAbout();

        if
        (
          Rhinoceros.MainWindow.Visible ||
          Rhinoceros.MainWindow.ActivePopup?.IsInvalid == false
        )
        {
          Rhinoceros.MainWindow.BringToFront();
          return Result.Succeeded;
        }

        return Result.Succeeded;
      }

      if (AddinOptions.Current.CompactTab)
      {
        data.Application.CreateRibbonTab(AddIn.AddinName);
        data.Application.ActivateRibbonTab(AddIn.AddinName);
      }

      var result = Start(new RibbonHandler(data.Application));

      if (result == Result.Failed)
        ShowLoadError(data);

      return result;
    }

    internal static Result Start(RibbonHandler ribbon)
    {
      var result = Result.Failed;
      var button = RestoreButton(CommandName);

      switch (result = Revit.OnStartup(Revit.ApplicationUI))
      {
        case Result.Succeeded:
          // Update Rhino button Tooltip
          button.ToolTip = $"Restores previously visible Rhino windows on top of Revit window";
          button.LongDescription = $"Use CTRL key to open a Rhino model";
          button.ShowText(false);

          var assemblies = AppDomain.CurrentDomain.GetAssemblies();

          // Register UI on Revit
          if (assemblies.Any(x => x.GetName().Name == "RhinoCommon"))
          {
            rhinoPanel = ribbon.CreateAddinPanel(AddIn.RhinoVersionInfo?.ProductName ?? "Rhinoceros");
            CommandRhino.CreateUI(rhinoPanel);
            CommandImport.CreateUI(rhinoPanel);
            CommandToggleRhinoPreview.CreateUI(rhinoPanel);
            CommandPython.CreateUI(rhinoPanel);
            CommandRhinoOptions.CreateUI(rhinoPanel);
          }

          if (assemblies.Any(x => x.GetName().Name == "Grasshopper"))
          {
            grasshopperPanel = ribbon.CreateAddinPanel("Grasshopper");
            CommandGrasshopper.CreateUI(grasshopperPanel);
            CommandGrasshopperPreview.CreateUI(grasshopperPanel);
            CommandGrasshopperSolver.CreateUI(grasshopperPanel);
            CommandGrasshopperRecompute.CreateUI(grasshopperPanel);
            CommandGrasshopperBake.CreateUI(grasshopperPanel);
            grasshopperPanel.AddSeparator();
            CommandGrasshopperPlayer.CreateUI(grasshopperPanel);
            grasshopperPanel.AddSlideOut();
            CommandGrasshopperPackageManager.CreateUI(grasshopperPanel);
            CommandGrasshopperFolders.CreateUI(grasshopperPanel);

            // Script Packages UI
            UpdateScriptPkgUI(ribbon);

            // Setup listeners, and in either case, update the packages ui
            // listed for changes in installed packages
            Rhino.Commands.Command.EndCommand += PackageManagerCommand_EndCommand;
            // listen for changes to user-script paths in options
            AddinOptions.ScriptLocationsChanged += AddinOptions_ScriptLocationsChanged;
          }

          UpdateRibbonCompact();

          result = Result.Succeeded;
          break;

        case Result.Cancelled:
          button.Enabled = false;

          if (AddIn.CurrentStatus == AddIn.Status.Unavailable)
            button.ToolTip = "Rhino.Inside failed to found a valid copy of Rhino installed.";
          else if (AddIn.CurrentStatus == AddIn.Status.Obsolete)
            button.ToolTip = "Rhino.Inside has expired.";
          else
            button.ToolTip = "Rhino.Inside load was cancelled.";

          button.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit"));
          break;

        case Result.Failed:
          button.Enabled = false;
          button.ToolTip = "Rhino.Inside failed to load.";
          return Result.Failed;
      }
      return result;
    }

    private static void AddinOptions_CompactRibbonChanged(object sender, EventArgs e) => UpdateRibbonCompact();

    static void UpdateRibbonCompact()
    {
      // collapse panel if in compact mode
      if (AddinOptions.Current.CompactRibbon)
      {
        rhinoPanel?.Collapse(AddIn.AddinName);
        grasshopperPanel?.Collapse(AddIn.AddinName);
      }
      else
      {
        rhinoPanel?.Expand(AddIn.AddinName);
        grasshopperPanel?.Expand(AddIn.AddinName);
      }
    }

    private static void AddinOptions_UpdateChannelChanged(object sender, EventArgs e)
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        button.Image = ImageBuilder.LoadRibbonButtonImage(CommandIcon, true);
        button.LargeImage = ImageBuilder.LoadRibbonButtonImage(CommandIcon);
      }
    }

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

    Result ShowLoadError(ExternalCommandData data)
    {
      using
      (
        var taskDialog = new TaskDialog("Oops! Something went wrong :(")
        {
          Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
          MainIcon = UIX.TaskDialogIcons.IconError,
          TitleAutoPrefix = true,
          AllowCancellation = true,
          MainInstruction = "Rhino.Inside failed to load",
          MainContent = $"Please run some tests before reporting.{Environment.NewLine}Those tests would help us figure out what happened.",
          ExpandedContent = "This problem use to be due an incompatibility with other installed add-ins.\n\n" +
                            "While running on these modes you may see other add-ins errors and it may take longer to load, don't worry about that no persistent change will be made on your computer.",
          VerificationText = "Exclude installed add-ins list from the report.",
          FooterText = "Current version: " + AddIn.DisplayVersion
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "1. Run Revit without other Addins…", "Good for testing if Rhino.Inside would load if no other add-in were installed.");
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "2. Run Rhino.Inside in verbose mode…", "Enables all logging mechanisms built in Rhino for support purposes.");
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "3. Send report…", "Reports this problem by email to tech@mcneel.com");
        taskDialog.DefaultButton = TaskDialogResult.CommandLink3;

        while (true)
          switch (taskDialog.Show())
          {
            case TaskDialogResult.CommandLink1: RunWithoutAddIns(data); break;
            case TaskDialogResult.CommandLink2: RunVerboseMode(data); break;
            case TaskDialogResult.CommandLink3:
              ErrorReport.SendEmail
              (
                data.Application,
                "Rhino.Inside Revit failed to load",
                !taskDialog.WasVerificationChecked(),
                new string[]
                {
                  data.Application.Application.RecordingJournalFilename,
                  RhinoDebugMessages_txt,
                  RhinoAssemblyResolveLog_txt
                }
              );
              return Result.Succeeded;
            default: return Result.Cancelled;
          }
      }
    }

    void RunWithoutAddIns(ExternalCommandData data)
    {
      var SafeModeFolder = Path.Combine(data.Application.Application.CurrentUserAddinsLocation, "RhinoInside.Revit", "SafeMode");
      Directory.CreateDirectory(SafeModeFolder);

      Settings.AddIns.GetInstalledAddins(data.Application.Application.VersionNumber, out var AddinFiles);
      if (AddinFiles.Where(x => Path.GetFileName(x) == "RhinoInside.Revit.addin").FirstOrDefault() is string RhinoInsideRevitAddinFile)
      {
        var SafeModeAddinFile = Path.Combine(SafeModeFolder, Path.GetFileName(RhinoInsideRevitAddinFile));
        File.Copy(RhinoInsideRevitAddinFile, SafeModeAddinFile, true);

        if (Settings.AddIns.LoadFrom(SafeModeAddinFile, out var SafeModeAddin))
        {
          SafeModeAddin.First().Assembly = Assembly.GetCallingAssembly().Location;
          Settings.AddIns.SaveAs(SafeModeAddin, SafeModeAddinFile);
        }

        var journalFile = Path.Combine(SafeModeFolder, "RhinoInside.Revit-SafeMode.txt");
        using (var journal = File.CreateText(journalFile))
        {
          journal.WriteLine("' ");
          journal.WriteLine("Dim Jrn");
          journal.WriteLine("Set Jrn = CrsJournalScript");
          journal.WriteLine(" Jrn.RibbonEvent \"TabActivated:Add-Ins\"");
          journal.WriteLine(" Jrn.RibbonEvent \"Execute external command:CustomCtrl_%CustomCtrl_%Add-Ins%Rhinoceros%CommandRhinoInside:RhinoInside.Revit.UI.CommandRhinoInside\"");
        }

        var batchFile = Path.Combine(SafeModeFolder, "RhinoInside.Revit-SafeMode.bat");
        using (var batch = File.CreateText(batchFile))
        {
          batch.WriteLine($"\"{Process.GetCurrentProcess().MainModule.FileName}\" \"{Path.GetFileName(journalFile)}\"");
        }

        var si = new ProcessStartInfo()
        {
          FileName = Process.GetCurrentProcess().MainModule.FileName,
          Arguments = $"\"{journalFile}\""
        };
        using (var RevitApp = Process.Start(si)) { RevitApp.WaitForExit(); }
      }
    }

    static readonly string RhinoDebugMessages_txt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RhinoDebugMessages.txt");
    static readonly string RhinoAssemblyResolveLog_txt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RhinoAssemblyResolveLog.txt");

    void RunVerboseMode(ExternalCommandData data)
    {
      const string SDKRegistryKeyName = @"Software\McNeel\Rhinoceros\SDK";

      if (File.Exists(RhinoDebugMessages_txt))
        File.Delete(RhinoDebugMessages_txt);

      if (File.Exists(RhinoAssemblyResolveLog_txt))
        File.Delete(RhinoAssemblyResolveLog_txt);

      using (File.Create(RhinoAssemblyResolveLog_txt)) { }

      bool deleteKey = false;

      try
      {
        using (var existingSDK = Registry.CurrentUser.OpenSubKey(SDKRegistryKeyName))
          if (existingSDK is null)
          {
            using (var newSDK = Registry.CurrentUser.CreateSubKey(SDKRegistryKeyName))
              if (newSDK is null)
                return;

            deleteKey = true;
          }

        var DebugLogging = Settings.DebugLogging.Current;
        try
        {
          Settings.DebugLogging.Current = new DebugLogging
          {
            Enabled = true,
            SaveToFile = true
          };

          var si = new ProcessStartInfo()
          {
            FileName = Process.GetCurrentProcess().MainModule.FileName,
            Arguments = "/nosplash",
            UseShellExecute = false
          };
          si.EnvironmentVariables["RhinoInside_RunScript"] = "_Grasshopper";

          using (var RevitApp = Process.Start(si)) { RevitApp.WaitForExit(); }
        }
        finally
        {
          Settings.DebugLogging.Current = DebugLogging;
        }
      }
      finally
      {
        if (deleteKey)
          try { Registry.CurrentUser.DeleteSubKey(SDKRegistryKeyName); }
          catch { }
      }
    }

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

    private static HashSet<ScriptPkg> _lastState = new HashSet<ScriptPkg>();

    private static void UpdateScriptPkgUI(RibbonHandler ribbon)
    {
      // determine which packages need to be loaded
      var curState = new HashSet<ScriptPkg>();
      if (AddinOptions.Current.LoadInstalledScriptPackages)
        curState.UnionWith(CommandGrasshopperPackageManager.GetInstalledScriptPackages());

      if (AddinOptions.Current.LoadUserScriptPackages)
        curState.UnionWith(ScriptPkg.GetUserScriptPackages());

      // create a combined set of both last and current states to iterate over
      var pkgs = new HashSet<ScriptPkg>(curState);
      pkgs.UnionWith(_lastState);

      // update the package ui
      foreach (var pkg in pkgs)
      {
        // skip existing packages
        if (curState.Contains(pkg) && _lastState.Contains(pkg))
          continue;

        // create new packages
        else if (curState.Contains(pkg) && !_lastState.Contains(pkg))
        {
          if (LinkedScripts.HasUI(pkg, ribbon))
            TaskDialog.Show(
              AddIn.AddinName,
              $"Package \"{pkg.Name}\" has been previously loaded in to the Revit UI." +
              "Restart Revit for changes to take effect."
              );
          else
            LinkedScripts.CreateUI(pkg, ribbon);
        }

        // or remove, removed packages
        else if (!curState.Contains(pkg) && _lastState.Contains(pkg))
        {
          if (LinkedScripts.HasUI(pkg, ribbon))
            LinkedScripts.RemoveUI(pkg, ribbon);
        }
      }

      _lastState = curState;
    }

    private static async void AddinOptions_ScriptLocationsChanged(object sender, EventArgs e)
    {
      // wait for Revit to be ready and get uiApp
      var uiApp = await External.ActivationGate.Yield();
      UpdateScriptPkgUI(new RibbonHandler(uiApp));
    }

    private static async void PackageManagerCommand_EndCommand(object sender, Rhino.Commands.CommandEventArgs e)
    {
      if (e.CommandEnglishName == "PackageManager")
      {
        // wait for Revit to be ready and get uiApp
        var uiApp = await External.ActivationGate.Yield();
        UpdateScriptPkgUI(new RibbonHandler(uiApp));
      }
    }
  }
}
