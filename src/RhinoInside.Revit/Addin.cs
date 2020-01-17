using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace RhinoInside.Revit
{
  enum AddinStartupMode
  {
    Cancelled = -2,
    Disabled = -1,
    Default = 0,
    WhenNeeded = 1,
    AtStartup = 2,
    Scripting = 3
  }

  public class Addin : IExternalApplication
  {
    #region StartupMode
    static AddinStartupMode GetStartupMode()
    {
      if (!Enum.TryParse(Environment.GetEnvironmentVariable("RhinoInside_StartupMode"), out AddinStartupMode mode))
        mode = AddinStartupMode.Default;

      if (mode == AddinStartupMode.Default)
        mode = AddinStartupMode.WhenNeeded;

      return mode;
    }
    internal static readonly AddinStartupMode StartupMode = GetStartupMode();
    #endregion

    #region Static constructor
    static readonly string SystemDir =
      Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path", null) as string ??
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP", "System");

    internal static readonly string RhinoExePath = Path.Combine(SystemDir, "Rhino.exe");
    internal static readonly FileVersionInfo RhinoVersionInfo = File.Exists(RhinoExePath) ? FileVersionInfo.GetVersionInfo(RhinoExePath) : null ;
    static readonly Version MinimumRhinoVersion = new Version(7, 0, 19344);
    static readonly Version RhinoVersion = new Version
    (
      RhinoVersionInfo?.FileMajorPart ?? 0,
      RhinoVersionInfo?.FileMinorPart ?? 0,
      RhinoVersionInfo?.FileBuildPart ?? 0,
      RhinoVersionInfo?.FilePrivatePart ?? 0
    );

    static Addin()
    {
      ResolveEventHandler OnRhinoCommonResolve = null;
      AppDomain.CurrentDomain.AssemblyResolve += OnRhinoCommonResolve = (sender, args) =>
      {
        const string rhinoCommonAssemblyName = "RhinoCommon";
        var assemblyName = new AssemblyName(args.Name).Name;

        if (assemblyName != rhinoCommonAssemblyName)
          return null;

        AppDomain.CurrentDomain.AssemblyResolve -= OnRhinoCommonResolve;
        return Assembly.LoadFrom(Path.Combine(SystemDir, rhinoCommonAssemblyName + ".dll"));
      };
    }
    #endregion

    #region IExternalApplication Members
    Result IExternalApplication.OnStartup(UIControlledApplication applicationUI)
    {
      if (StartupMode == AddinStartupMode.Cancelled)
        return Result.Cancelled;

      ApplicationUI = applicationUI;

      EventHandler<ApplicationInitializedEventArgs> applicationInitialized = null;
      ApplicationUI.ControlledApplication.ApplicationInitialized += applicationInitialized = (sender, args) =>
      {
        ApplicationUI.ControlledApplication.ApplicationInitialized -= applicationInitialized;
        Revit.ActiveUIApplication = new UIApplication(sender as Autodesk.Revit.ApplicationServices.Application);

        if (StartupMode < AddinStartupMode.AtStartup)
          return;

        if (Revit.OnStartup(Revit.ApplicationUI) == Result.Succeeded)
        {
          if (StartupMode == AddinStartupMode.Scripting)
            Revit.ActiveUIApplication.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit));
        }
      };

      // Add launch RhinoInside push button
      UI.CommandRhinoInside.CreateUI(applicationUI.CreateRibbonPanel("Rhinoceros"));

      return Result.Succeeded;
    }

    Result IExternalApplication.OnShutdown(UIControlledApplication applicationUI)
    {
      try
      {
        return Revit.OnShutdown(applicationUI);
      }
      catch (Exception)
      {
        return Result.Failed;
      }
      finally
      {
        ApplicationUI = null;
      }
    }
    #endregion

    #region Version
    public static bool IsExpired(bool quiet = true)
    {
      if (DaysUntilExpiration > 0 && quiet)
        return false;

      using
      (
        var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
        {
          Title = "Days left",
          MainIcon = TaskDialogIcons.IconInformation,
          TitleAutoPrefix = true,
          AllowCancellation = true,
          MainInstruction = DaysUntilExpiration < 1 ?
          "This WIP build has expired" :
          $"This WIP build expires in {DaysUntilExpiration} days",
          FooterText = "Current version: " + DisplayVersion
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Check for updates…");
        if (taskDialog.Show() == TaskDialogResult.CommandLink1)
        {
          using (Process.Start(@"https://www.rhino3d.com/download/rhino.inside-revit/7/wip")) { }
        }
      }

      return DaysUntilExpiration < 1;
    }
    internal static Result CheckSetup(Autodesk.Revit.ApplicationServices.ControlledApplication app)
    {
      if (RhinoVersion >= MinimumRhinoVersion)
        return IsExpired() ? Result.Cancelled : Result.Succeeded;

      using
      (
        var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
        {
          Title = "Update Rhino",
          MainIcon = TaskDialogIcons.IconInformation,
          AllowCancellation = true,
          MainInstruction = "Unsupported Rhino WIP version",
          MainContent = $"Expected Rhino version is ({MinimumRhinoVersion}) or above.",
          ExpandedContent =
          RhinoVersionInfo is null ? "Rhino\n" :
          $"{RhinoVersionInfo.ProductName} {RhinoVersionInfo.ProductMajorPart}\n" +
          $"• Version: {RhinoVersion}\n" +
          $"• Path: '{SystemDir}'" + (!File.Exists(RhinoExePath) ? " (not found)" : string.Empty) + "\n" +
          $"\n{app.VersionName}\n" +
#if REVIT_2019
          $"• Version: {app.SubVersionNumber} ({app.VersionBuild})\n" +
#else
          $"• Version: {app.VersionNumber} ({app.VersionBuild})\n" +
#endif
          $"• Path: {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\n" +
          $"• Language: {app.Language.ToString()}",
          FooterText = $"Current Rhino WIP version: {RhinoVersion}"
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Download latest Rhino WIP…");
        if (taskDialog.Show() == TaskDialogResult.CommandLink1)
        {
          using (Process.Start(@"https://www.rhino3d.com/download/rhino/wip")) { }
        }
      }

      return Result.Failed;
    }

    static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
    public static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());
    public static int DaysUntilExpiration => Math.Max(0, 45 - (DateTime.Now - BuildDate).Days);

    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
    public static string DisplayVersion => $"{Version} ({BuildDate})";
    #endregion

    internal static UIControlledApplication ApplicationUI { get; private set; }
  }
}

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoInside : ExternalCommand
  {
    static PushButton Button;
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      const string CommandName = "Rhino";
      const string Shortcuts = "R#Ctrl+R";

      var buttonData = NewPushButtonData<CommandRhinoInside, AllwaysAvailable>(CommandName);
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        Button = pushButton;

        if (Addin.RhinoVersionInfo is null)
        {
          pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://www.rhino3d.com/download/rhino/wip"));
          pushButton.Image = ImageBuilder.LoadBitmapImage("RhinoInside.Resources.Rhino-logo.png", true);
          pushButton.LargeImage = ImageBuilder.LoadBitmapImage("RhinoInside.Resources.Rhino-logo.png");
        }
        else
        {
          pushButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit/beta/"));
          using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(Addin.RhinoExePath))
          {
            pushButton.Image = icon.ToBitmapSource(true);
            pushButton.LargeImage = icon.ToBitmapSource();
          }

          try
          {
            var versionInfo = Addin.RhinoVersionInfo;
            pushButton.ToolTip = $"Loads {versionInfo.ProductName} inside this Revit session";
            pushButton.LongDescription = $"Rhino: {versionInfo.ProductVersion} ({versionInfo.FileDescription}){Environment.NewLine}Rhino.Inside: {Addin.DisplayVersion}{Environment.NewLine}{versionInfo.LegalCopyright}";
          }
          catch (Exception) { }
        }

        if (Addin.StartupMode == AddinStartupMode.Disabled)
        {
          Button.Enabled = false;
          Button.ToolTip = "Addin Disabled";
        }
        else
        {
          RegisterShortcut("Add-Ins", ribbonPanel.Name, typeof(CommandRhinoInside).Name, CommandName, Shortcuts);
        }
      }
    }

    static void RegisterShortcut(string tabName, string panelName, string commandId, string commandName, string commandShortcuts)
    {
      commandId = $"CustomCtrl_%CustomCtrl_%{tabName}%{panelName}%{commandId}";

      string keyboardShortcutsPath = Path.Combine(Revit.CurrentUsersDataFolderPath, "KeyboardShortcuts.xml");
      if (!File.Exists(keyboardShortcutsPath))
        keyboardShortcutsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", $"RVT {Revit.ApplicationUI.ControlledApplication.VersionNumber}", "UserDataCache", "KeyboardShortcuts.xml");

      if (!Serialization.KeyboardShortcuts.LoadFrom(keyboardShortcutsPath, out var shortcuts))
        Serialization.KeyboardShortcuts.LoadFromResources($"RhinoInside.Resources.RVT{Revit.ApplicationUI.ControlledApplication.VersionNumber}.KeyboardShortcuts.xml", out shortcuts);

#if DEBUG
      // Those lines generate the KeyboardShortcuts.xml template file when new Revit version is supported
      string keyboardShortcutsTemplatePath = Path.Combine(Addin.SourceCodePath, "Resources", $"RVT{Revit.ApplicationUI.ControlledApplication.VersionNumber}", "KeyboardShortcuts.xml");
      var info = new FileInfo(keyboardShortcutsTemplatePath);
      if (info.Length == 0)
      {
        var shortcutsSummary = new Serialization.KeyboardShortcuts.Shortcuts();
        foreach (var shortcutItem in shortcuts.OrderBy(x => x.CommandId))
        {
          if (!string.IsNullOrEmpty(shortcutItem.Shortcuts))
          {
            var shortcutDefinition = new Serialization.KeyboardShortcuts.ShortcutItem
            {
              CommandId = shortcutItem.CommandId,
              Shortcuts = shortcutItem.Shortcuts
            };
            shortcutsSummary.Add(shortcutDefinition);
          }
        }

        Serialization.KeyboardShortcuts.SaveAs(shortcutsSummary, keyboardShortcutsTemplatePath);
      }
#endif

      try
      {
        var shortcutItem = shortcuts.Where(x => x.CommandId == commandId).First();
        if (shortcutItem.Shortcuts is null)
        {
          shortcutItem.Shortcuts = commandShortcuts;
          Rhinoceros.ModalScope.Exit += ShowShortcutHelp;
        }
      }
      catch (InvalidOperationException)
      {
        var shortcutItem = new Serialization.KeyboardShortcuts.ShortcutItem()
        {
          CommandName = commandName,
          CommandId = commandId,
          Shortcuts = commandShortcuts,
          Paths = $"{tabName}>{panelName}"
        };
        shortcuts.Add(shortcutItem);
        Rhinoceros.ModalScope.Exit += ShowShortcutHelp;
      }

      Serialization.KeyboardShortcuts.SaveAs(shortcuts, Path.Combine(Revit.CurrentUsersDataFolderPath, "KeyboardShortcuts.xml"));
    }

    static void ShowShortcutHelp(object sender, EventArgs e)
    {
      Rhinoceros.ModalScope.Exit -= ShowShortcutHelp;

      using
      (
        var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
        {
          Title = "New Shortcut",
          MainIcon = TaskDialogIcons.IconInformation,
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

    public override Result Execute(ExternalCommandData data, ref string message, Autodesk.Revit.DB.ElementSet elements)
    {
      var result = Result.Failed;
      string rhinoTab = Addin.RhinoVersionInfo?.ProductName ?? "Rhinoceros";

      if (RhinoCommand.Availability.Available)
      {
        if (Keyboard.IsKeyDown(Key.LeftCtrl))
          return Rhinoceros.RunCommandAbout();

        using (var modal = new Rhinoceros.ModalScope())
          result = modal.Run(false, true);

        // If no windows are visible we show the Ribbon tab
        if (result == Result.Cancelled)
          result = data.Application.ActivateRibbonTab(rhinoTab) ? Result.Succeeded : Result.Failed;

        return result;
      }

      switch(result = Revit.OnStartup(Revit.ApplicationUI))
      {
        case Result.Succeeded:
          RhinoCommand.Availability.Available = true;

          // Update Rhino button Tooltip
          Button.ToolTip = $"Restores previously visible Rhino windows on top of Revit window";
          Button.LongDescription = $"Use CTRL key to open a Rhino model";

          // Register UI on Revit
          data.Application.CreateRibbonTab(rhinoTab);

          var RhinocerosPanel = data.Application.CreateRibbonPanel(rhinoTab, "Rhinoceros");
          HelpCommand.CreateUI(RhinocerosPanel);
          RhinocerosPanel.AddSeparator();
          CommandRhino.CreateUI(RhinocerosPanel);
          CommandPython.CreateUI(RhinocerosPanel);

          var GrasshopperPanel = data.Application.CreateRibbonPanel(rhinoTab, "Grasshopper");
          CommandGrasshopper.CreateUI(GrasshopperPanel);
          CommandGrasshopperPlayer.CreateUI(GrasshopperPanel);
          CommandGrasshopperPreview.CreateUI(GrasshopperPanel);
          CommandGrasshopperRecompute.CreateUI(GrasshopperPanel);
          CommandGrasshopperBake.CreateUI(GrasshopperPanel);

          var SamplesPanel = data.Application.CreateRibbonPanel(rhinoTab, "Samples");
          Samples.Sample1.CreateUI(SamplesPanel);
          Samples.Sample4.CreateUI(SamplesPanel);
          Samples.Sample6.CreateUI(SamplesPanel);
          Samples.Sample8.CreateUI(SamplesPanel);

          result = data.Application.ActivateRibbonTab(rhinoTab) ? Result.Succeeded : Result.Failed;
          break;
        case Result.Cancelled:
          Button.Enabled = false;
          Button.ToolTip = "Rhino.Inside has expired.";
          Button.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/download/rhino.inside-revit/7/wip"));
          break;
        case Result.Failed:
          Button.Enabled = false;
          Button.ToolTip = "Rhino.Inside failed to load.";
          ShowLoadError(data);
          break;
      }

      return result;
    }

    void ShowLoadError(ExternalCommandData data)
    {
      using
      (
        var taskDialog = new TaskDialog("Ups! Something went wrong :(")
        {
          Id = MethodBase.GetCurrentMethod().DeclaringType.FullName,
          MainIcon = TaskDialogIcons.IconError,
          TitleAutoPrefix = true,
          AllowCancellation = false,
          MainInstruction = "Rhino.Inside failed to load",
          MainContent = "Do you want to report this by email to tech@mcneel.com?",
          ExpandedContent = "This problem use to be due an incompatibility with other installed Addins.\n\n" +
                            "While running on these modes you may see other Addins errors and it may take longer to load, don't worry about that no persistent change will be made on your computer.",
          CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
          DefaultButton = TaskDialogResult.Yes,
          VerificationText = "Exclude installed Addins list from the report.",
          FooterText = "Current version: " + Addin.DisplayVersion
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Run Revit without other Addins…", "Good for testing if Rhino.Inside would load if no other Addin were installed.");
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Run Rhino.Inside in verbose mode…", "Enables all logging mechanisms built in Rhino for support purposes.");

        var keepAsking = true;
        while(keepAsking)
          switch (taskDialog.Show())
          {
            case TaskDialogResult.CommandLink1: RunWithoutAddIns(data); break;
            case TaskDialogResult.CommandLink2: RunVerboseMode(data); break;
            case TaskDialogResult.Yes: ReportAddins(data, !taskDialog.WasVerificationChecked()); keepAsking = false; break;
            default: keepAsking = false; break;
          }
      }
    }

    void RunVerboseMode(ExternalCommandData data)
    {
      const string SDKRegistryKeyName = @"Software\McNeel\Rhinoceros\SDK";

      var RhinoDebugMessages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RhinoDebugMessages.txt");
      if (File.Exists(RhinoDebugMessages))
        File.Delete(RhinoDebugMessages);

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
            DebugLoggingEnabled =    (DebugLogging.GetValue("Enabled", 0) as int?).GetValueOrDefault();
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

    void RunWithoutAddIns(ExternalCommandData data)
    {
      using (new Serialization.LockAddIns(data.Application.Application.VersionNumber))
      {
        var si = new ProcessStartInfo()
        {
          FileName = Process.GetCurrentProcess().MainModule.FileName,
          Arguments = "/nosplash",
          UseShellExecute = false
        };
        si.EnvironmentVariables["RhinoInside_RunScript"] = "_Grasshopper";

        using (var RevitApp = Process.Start(si)) { RevitApp.WaitForExit(); }
      }
    }

    void ReportAddins(ExternalCommandData data, bool includeAddinsList)
    {
      var mailtoURI = @"mailto:tech@mcneel.com?subject=Rhino.Inside%20Revit%20failed%20to%20load&body=";

      var mailBody = @"Please give us any additional info you see fit here..." + Environment.NewLine + Environment.NewLine;

      var RhinoDebugMessages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RhinoDebugMessages.txt");
      if (File.Exists(RhinoDebugMessages))
        mailBody += $"<Please attach '{RhinoDebugMessages}' file here>" + Environment.NewLine + Environment.NewLine;

      mailBody += $"Rhino.Inside: {Addin.DisplayVersion}" + Environment.NewLine;
      var rhino = Addin.RhinoVersionInfo;
      mailBody += $"Rhino: {rhino.ProductVersion} ({rhino.FileDescription})" + Environment.NewLine;
      var revit = data.Application.Application;

#if REVIT_2019
      mailBody += $"Revit: {revit.SubVersionNumber} ({revit.VersionBuild})" + Environment.NewLine;
#else
      mailBody += $"Revit: {revit.VersionNumber} ({revit.VersionBuild})" + Environment.NewLine;
#endif

      if (includeAddinsList)
      {
        mailBody += @"Revit Addins:" + Environment.NewLine + Environment.NewLine;

        Serialization.AddIns.GetInstalledAddins(data.Application.Application.VersionNumber, out var manifests);
        foreach (var manifest in manifests)
        {
          mailBody += $"Manifest: {manifest}" + Environment.NewLine;
          if (Serialization.AddIns.LoadFrom(manifest, out var revitAddins))
          {
            foreach (var addin in revitAddins)
            {
              if (!string.IsNullOrEmpty(addin.Type))
                mailBody += $"Type: {addin.Type}" + Environment.NewLine;

              if (!string.IsNullOrEmpty(addin.Name))
                mailBody += $"Name: {addin.Name}" + Environment.NewLine;

              if (!string.IsNullOrEmpty(addin.VendorDescription))
                mailBody += $"VendorDescription: {addin.VendorDescription}" + Environment.NewLine;

              if (!string.IsNullOrEmpty(addin.Assembly))
              {
                mailBody += $"Assembly: {addin.Assembly}" + Environment.NewLine;

                var versionInfo = File.Exists(addin.Assembly) ? FileVersionInfo.GetVersionInfo(addin.Assembly) : null;
                mailBody += $"FileVersion: {versionInfo?.FileVersion}" + Environment.NewLine;
              }
            }

            mailBody += Environment.NewLine;
          }
        }
      }

      mailBody = Uri.EscapeDataString(mailBody);

      using (Process.Start(mailtoURI + mailBody)) { }
    }
  }
}
