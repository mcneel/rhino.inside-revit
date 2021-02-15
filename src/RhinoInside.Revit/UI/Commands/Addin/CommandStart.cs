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
using RhinoInside.Revit.External.UI.Extensions;

using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandStart : Command
  {
    static RibbonPanel _rhinoPanel;
    static RibbonPanel _grasshopperPanel;

    public static string CommandName => "Start";
    public static string CommandIcon => AddinUpdater.ActiveChannel.IsStable? "RIR-logo.png" : "RIR-WIP-logo.png";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandStart, AvailableWhenNotObsolete>(CommandName, CommandIcon, "");
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton(CommandName, pushButton);
        pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit/beta/"));

        SetTooltip(pushButton);

        if (Addin.StartupMode == AddinStartupMode.Disabled)
        {
          pushButton.Enabled = false;
          pushButton.ToolTip = "Addin Disabled";
        }
        else
        {
          if (Settings.KeyboardShortcuts.RegisterDefaultShortcut("Add-Ins", ribbonPanel.Name, typeof(CommandStart).Name, CommandName, "R#Ctrl+R"))
            External.ActivationGate.Exit += ShowShortcutHelp;
        }
      }

      // add listener for ui compact changes
      AddinOptions.CompactRibbonChanged += AddinOptions_CompactRibbonChanged;
      AddinOptions.UpdateChannelChanged += AddinOptions_UpdateChannelChanged;
    }

    static void SetTooltip(PushButton pushButton)
    {
      if (Addin.RhinoVersionInfo is FileVersionInfo rhInfo)
      {
        // try-catch to capture any property getters failing
        try
        {
          pushButton.ToolTip =
            $"Loads {rhInfo.ProductName} inside this Revit session";
          pushButton.LongDescription =
            $"Rhino: {rhInfo.ProductVersion} ({rhInfo.FileDescription}){Environment.NewLine}" +
            $"Rhino.Inside: {Addin.DisplayVersion}{Environment.NewLine}{rhInfo.LegalCopyright}";
        }
        catch (Exception) { }
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, Autodesk.Revit.DB.ElementSet elements) {
      if
      (
        (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
        (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
      )
        return ShowLoadError(data);

      if (Addin.CurrentStatus == Addin.Status.Ready)
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
        data.Application.CreateRibbonTab(Addin.AddinName);
        data.Application.ActivateRibbonTab(Addin.AddinName);
      }

      var result = Start(
        panelMaker: (tabName, panelName) => data.Application.CreateRibbonPanel(tabName, panelName)
        );

      if (result == Result.Failed)
        ShowLoadError(data);

      return result;
    }

    internal static Result Start(Func<string, string, RibbonPanel> panelMaker)
    {
      var result = Result.Failed;
      var button = RestoreButton(CommandName);

      switch (result = Revit.OnStartup(Revit.ApplicationUI))
      {
        case Result.Succeeded:
          // Update Rhino button Tooltip
          button.ToolTip = $"Restores previously visible Rhino windows on top of Revit window";
          button.LongDescription = $"Use CTRL key to open a Rhino model";
          // hide the button title
          if (button.GetAdwndRibbonButton() is Autodesk.Windows.RibbonButton adwndRadioButton)
            adwndRadioButton.ShowText = false;

          var assemblies = AppDomain.CurrentDomain.GetAssemblies();

          // Register UI on Revit
          if (assemblies.Any(x => x.GetName().Name == "RhinoCommon"))
          {
            _rhinoPanel = panelMaker(Addin.AddinName, Addin.RhinoVersionInfo?.ProductName ?? "Rhinoceros");
            CommandRhino.CreateUI(_rhinoPanel);
            CommandImport.CreateUI(_rhinoPanel);
            CommandToggleRhinoPreview.CreateUI(_rhinoPanel);
            CommandPython.CreateUI(_rhinoPanel);
            CommandRhinoOptions.CreateUI(_rhinoPanel);
          }

          if (assemblies.Any(x => x.GetName().Name == "Grasshopper"))
          {
            _grasshopperPanel = panelMaker(Addin.AddinName, "Grasshopper");
            CommandGrasshopper.CreateUI(_grasshopperPanel);
            CommandGrasshopperPreview.CreateUI(_grasshopperPanel);
            CommandGrasshopperSolver.CreateUI(_grasshopperPanel);
            CommandGrasshopperRecompute.CreateUI(_grasshopperPanel);
            CommandGrasshopperBake.CreateUI(_grasshopperPanel);
            _grasshopperPanel.AddSeparator();
            CommandGrasshopperPlayer.CreateUI(_grasshopperPanel);
            _grasshopperPanel.AddSlideOut();
            CommandGrasshopperPackageManager.CreateUI(_grasshopperPanel);
            CommandGrasshopperFolders.CreateUI(_grasshopperPanel);

            // create grasshopper scripts panels
            if (AddinOptions.Current.LoadScriptPackagesOnStartup)
              foreach (var pkg in GetInstalledScriptPackages())
                GrasshopperLinkedScriptsCommand.CreateUI(pkg, panelMaker);

            if (AddinOptions.Current.LoadScriptsOnStartup)
              foreach (var pkg in GetUserScriptPackages())
                  GrasshopperLinkedScriptsCommand.CreateUI(pkg, panelMaker);
          }

          UpdateRibbonCompact();

          result = Result.Succeeded;
          break;

        case Result.Cancelled:
          button.Enabled = false;

          if (Addin.CurrentStatus == Addin.Status.Unavailable)
            button.ToolTip = "Rhino.Inside failed to found a valid copy of Rhino 7 WIP installed.";
          else if (Addin.CurrentStatus == Addin.Status.Obsolete)
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
        _rhinoPanel?.Collapse(Addin.AddinName);
        _grasshopperPanel?.Collapse(Addin.AddinName);
      }
      else
      {
        _rhinoPanel?.Expand(Addin.AddinName);
        _grasshopperPanel?.Expand(Addin.AddinName);
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
          FooterText = "Current version: " + Addin.DisplayVersion
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
      int DebugLoggingEnabled = 0;
      int DebugLoggingSaveToFile = 0;

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

        try
        {
          using (var DebugLogging = Registry.CurrentUser.OpenSubKey(@"Software\McNeel\Rhinoceros\7.0\Global Options\Debug Logging", true))
          {
            DebugLoggingEnabled = (DebugLogging.GetValue("Enabled", 0) as int?).GetValueOrDefault();
            DebugLoggingSaveToFile = (DebugLogging.GetValue("SaveToFile", 0) as int?).GetValueOrDefault();

            DebugLogging.SetValue("Enabled", 1);
            DebugLogging.SetValue("SaveToFile", 1);
            DebugLogging.Flush();
          }

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
          using (var DebugLogging = Registry.CurrentUser.OpenSubKey(@"Software\McNeel\Rhinoceros\7.0\Global Options\Debug Logging", true))
          {
            DebugLogging.SetValue("Enabled", DebugLoggingEnabled);
            DebugLogging.SetValue("SaveToFile", DebugLoggingSaveToFile);
            DebugLogging.Flush();
          }
        }
      }
      finally
      {
        if (deleteKey)
          try { Registry.CurrentUser.DeleteSubKey(SDKRegistryKeyName); }
          catch (Exception) { }
      }
    }

    static public void NotifyUpdateAvailable(ReleaseInfo releaseInfo)
    {
      // button gets deactivated if options are readonly
      if (!AddinOptions.IsReadOnly)
      {
        if (RestoreButton(CommandName) is PushButton button)
        {
          button.Highlight();
          button.ToolTip = "New Release Available for Download!\n"
                         + $"Version: {releaseInfo.Version}\n"
                         + button.ToolTip;
        }
      }
    }

    static public void ClearUpdateNotifiy()
    {
      if (RestoreButton(CommandName) is PushButton button)
      {
        button.ClearHighlight();
        SetTooltip(button);
      }
    }

    static public List<ScriptPkg> GetUserScriptPackages()
    {
      var pkgs = new List<ScriptPkg>();
      foreach (var location in AddinOptions.Current.ScriptLocations)
        if (Directory.Exists(location))
          pkgs.Add(
            new ScriptPkg { Name = Path.GetFileName(location), Location = location }
            );
        return pkgs;
    }

    static public List<ScriptPkg> GetInstalledScriptPackages()
    {
      var pkgs = new List<ScriptPkg>();
      if (Directory.Exists(Addin.AutoInstallPluginPath))
      {
        foreach (var dir in Directory.GetDirectories(Addin.AutoInstallPluginPath))
        {
          var manifestFile = Path.Combine(dir, "manifest.txt");
          if (File.Exists(manifestFile))
          {
            var mf = File.ReadAllLines(manifestFile);
            var version = mf[0].Trim();
            var scriptsPath = Path.Combine(dir, version, "gh_revit");
            if (Directory.Exists(scriptsPath))
              pkgs.Add(
                new ScriptPkg { Name = $"{Path.GetFileName(dir)} ({version})", Location = scriptsPath }
                );
          }
        }
      }
      return pkgs;
    }
  }
}
