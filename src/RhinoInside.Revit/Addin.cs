using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using UIX = RhinoInside.Revit.External.UI;
using RhinoInside.Revit.External.UI.Extensions;

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

  public class Addin : UIX.Application
  {
    #region Status
    internal enum Status
    {
      Crashed       = int.MinValue,     // Loaded and crashed
      Failed        = int.MinValue + 1, // Failed to load
      Obsolete      = int.MinValue + 2, // Rhino.Inside is obsolete version
      Expired       = int.MinValue + 3, // License is expired

      Unavailable   = 0,                // Not installed or not supported version
      Available     = 1,                // Available to load
      Ready         = int.MaxValue,     // Fully functional
    }
    static Status status = default;

    internal static Status CurrentStatus
    {
      get =>  status;

      set
      {
        if (status < Status.Available && value > status)
          throw new ArgumentException();

        status = value;
      }
    }
    #endregion

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

    #region Constructor
    static readonly string SystemDir =
      Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path", null) as string ??
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP", "System");

    internal static readonly string RhinoExePath = Path.Combine(SystemDir, "Rhino.exe");
    internal static readonly FileVersionInfo RhinoVersionInfo = File.Exists(RhinoExePath) ? FileVersionInfo.GetVersionInfo(RhinoExePath) : null;
    static readonly Version MinimumRhinoVersion = new Version(7, 0, 20056);
    static readonly Version RhinoVersion = new Version
    (
      RhinoVersionInfo?.FileMajorPart ?? 0,
      RhinoVersionInfo?.FileMinorPart ?? 0,
      RhinoVersionInfo?.FileBuildPart ?? 0,
      RhinoVersionInfo?.FilePrivatePart ?? 0
    );

    static Addin()
    {
      if (StartupMode == AddinStartupMode.Cancelled)
        return;

      if (RhinoVersion >= MinimumRhinoVersion)
        status = Status.Available;

      if (DaysUntilExpiration < 1)
        status = Status.Obsolete;

      if (CurrentStatus == Status.Available)
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
    }

    public Addin() : base(new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74")) { }
    #endregion

    #region IExternalApplication Members
    internal static UIControlledApplication ApplicationUI { get; private set; }

    protected override Result OnStartup(UIControlledApplication applicationUI)
    {
      if (StartupMode == AddinStartupMode.Cancelled)
        return Result.Cancelled;

      ApplicationUI = applicationUI;

      if (applicationUI.IsLateAddinLoading)
      {
        EventHandler<Autodesk.Revit.UI.Events.IdlingEventArgs> applicationIdling = null;
        ApplicationUI.Idling += applicationIdling = (sender, args) =>
        {
          if (sender is UIApplication app)
          {
            ApplicationUI.Idling -= applicationIdling;
            DoStartUp(app.Application);
          }
        };
      }
      else
      {
        EventHandler<ApplicationInitializedEventArgs> applicationInitialized = null;
        ApplicationUI.ControlledApplication.ApplicationInitialized += applicationInitialized = (sender, args) =>
        {
          ApplicationUI.ControlledApplication.ApplicationInitialized -= applicationInitialized;
          DoStartUp(sender as Autodesk.Revit.ApplicationServices.Application);
        };
      }

      // Add launch RhinoInside push button
      UI.CommandRhinoInside.CreateUI(applicationUI.CreateRibbonPanel("Rhinoceros"));

      return Result.Succeeded;
    }

    void DoStartUp(Autodesk.Revit.ApplicationServices.Application app)
    {
      Revit.ActiveUIApplication = new UIApplication(app);

      if (StartupMode < AddinStartupMode.AtStartup)
        return;

      if (Revit.OnStartup(Revit.ApplicationUI) == Result.Succeeded)
      {
        if (StartupMode == AddinStartupMode.Scripting)
          Revit.ActiveUIApplication.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit));
      }
    }

    protected override Result OnShutdown(UIControlledApplication applicationUI)
    {
      try
      {
        return Revit.OnShutdown(applicationUI);
      }
      catch
      {
        return Result.Failed;
      }
      finally
      {
        ApplicationUI = null;
      }
    }

    public override bool CatchException(Exception e, UIApplication app, object sender)
    {
      // There is a wild pointer somewhere, is better to close Revit.
      bool fatal = e is AccessViolationException;

      if (fatal)
        CurrentStatus = Status.Crashed;

      var RhinoInside_dmp = Path.Combine
      (
        Path.GetDirectoryName(app.Application.RecordingJournalFilename),
        Path.GetFileNameWithoutExtension(app.Application.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
      );

      return MiniDumper.Write(RhinoInside_dmp);
    }

    public override void ReportException(Exception e, UIApplication app, object sender)
    {
      if (MessageBox.Show
      (
        caption: $"{app.ActiveAddInId.GetAddInName()} {Version} - Oops! Something went wrong :(",
        icon: MessageBoxImage.Error,
        messageBoxText: $"'{e.GetType().FullName}' at {e.Source}." + Environment.NewLine +
                        Environment.NewLine + e.Message + Environment.NewLine +
                        Environment.NewLine + "Do you want to report this problem by email to tech@mcneel.com?",
        button: MessageBoxButton.YesNo,
        defaultResult: MessageBoxResult.Yes
      ) == MessageBoxResult.Yes)
      {
        var RhinoInside_dmp = Path.Combine
        (
          Path.GetDirectoryName(app.Application.RecordingJournalFilename),
          Path.GetFileNameWithoutExtension(app.Application.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
        );

        ErrorReport.SendEmail
        (
          app,
          false,
          new string[]
          {
            app.Application.RecordingJournalFilename,
            RhinoInside_dmp
          }
        );
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
        var taskDialog = new TaskDialog("Days left")
        {
          Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
          MainIcon = UIX.TaskDialogIcons.IconInformation,
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
        var taskDialog = new TaskDialog("Update Rhino")
        {
          Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
          MainIcon = UIX.TaskDialogIcons.IconInformation,
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

      return Result.Cancelled;
    }

    static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
    public static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());
    public static int DaysUntilExpiration => Math.Max(0, 45 - (DateTime.Now - BuildDate).Days);

    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
    public static string DisplayVersion => $"{Version} ({BuildDate})";
    #endregion
  }
}

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoInside : Command
  {
    static PushButton Button;
    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      const string CommandName = "Rhino";

      var buttonData = NewPushButtonData<CommandRhinoInside, Availability>(CommandName);
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
          if(Settings.KeyboardShortcuts.RegisterDefaultShortcut("Add-Ins", ribbonPanel.Name, typeof(CommandRhinoInside).Name, CommandName, "R#Ctrl+R"))
            External.ActivationGate.Exit += ShowShortcutHelp;
        }
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

      string rhinoTab = Addin.RhinoVersionInfo?.ProductName ?? "Rhinoceros";

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

        // If no windows are visible we show the Ribbon tab
        return data.Application.ActivateRibbonTab(rhinoTab) ? Result.Succeeded : Result.Failed;
      }

      var result = Result.Failed;
      switch (result = Revit.OnStartup(Revit.ApplicationUI))
      {
        case Result.Succeeded:
          // Update Rhino button Tooltip
          Button.ToolTip = $"Restores previously visible Rhino windows on top of Revit window";
          Button.LongDescription = $"Use CTRL key to open a Rhino model";

          // Register UI on Revit
          data.Application.CreateRibbonTab(rhinoTab);

          var RhinocerosPanel = data.Application.CreateRibbonPanel(rhinoTab, "Rhinoceros");
          HelpCommand.CreateUI(RhinocerosPanel);
          RhinocerosPanel.AddSeparator();
          CommandRhino.CreateUI(RhinocerosPanel);
          CommandRhinoPreview.CreateUI(RhinocerosPanel);
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

        if(Settings.AddIns.LoadFrom(SafeModeAddinFile, out var SafeModeAddin))
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
  }
}
