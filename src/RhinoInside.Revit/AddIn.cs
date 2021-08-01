using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using RhinoInside.Revit.External.ApplicationServices.Extensions;
using RhinoInside.Revit.Native;
using RhinoInside.Revit.Settings;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit
{
  enum AddInStartupMode
  {
    Cancelled = -2,
    Disabled = -1,
    Default = 0,
    WhenNeeded = 1,
    AtStartup = 2,
    Scripting = 3
  }

  public class AddIn : UIX.ExternalApplication
  {
    #region AddInInfo
    public static string AddinCompany => "McNeel";
    public static string AddinName => "Rhino.Inside";
    public static string AddinWebSite => @"https://www.rhino3d.com/inside/revit/1.0/";
    #endregion

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
      get => status;

      set
      {
        if (status < Status.Available && value > status)
          throw new ArgumentException();

        status = value;
      }
    }
    #endregion

    #region StartupMode
    static AddInStartupMode GetStartupMode()
    {
      if (!Enum.TryParse(Environment.GetEnvironmentVariable("RhinoInside_StartupMode"), out AddInStartupMode mode))
        mode = AddInStartupMode.Default;

      if (mode == AddInStartupMode.Default)
        mode = AddInStartupMode.WhenNeeded;

      return mode;
    }
    internal static readonly AddInStartupMode StartupMode = GetStartupMode();
    #endregion

    #region Constructor
    static readonly string SystemDir =
#if DEBUG
      Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\McNeel\Rhinoceros\7.0-WIP-Developer-Debug-trunk\Install", "Path", null) as string ??
#endif
      Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path", null) as string ??
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP", "System");

    internal static readonly string RhinoExePath = Path.Combine(SystemDir, "Rhino.exe");
    internal static readonly FileVersionInfo RhinoVersionInfo = File.Exists(RhinoExePath) ? FileVersionInfo.GetVersionInfo(RhinoExePath) : null;
    static readonly Version RhinoVersion = new Version
    (
      RhinoVersionInfo?.FileMajorPart ?? 0,
      RhinoVersionInfo?.FileMinorPart ?? 0,
      RhinoVersionInfo?.FileBuildPart ?? 0,
      RhinoVersionInfo?.FilePrivatePart ?? 0
    );
    static Version MinimumRhinoVersion
    {
      get
      {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(x => x.Name == "RhinoCommon").FirstOrDefault().Version;
        return new Version(assemblyVersion.Major, assemblyVersion.Minor, 0);
      }
    }

    static AddIn()
    {
      if (StartupMode == AddInStartupMode.Cancelled)
        return;

      if (IsExpired())
        status = Status.Obsolete;
      else if (RhinoVersion >= MinimumRhinoVersion)
        status = NativeLoader.IsolateOpenNurbs() ? Status.Available : Status.Unavailable;

      Logger.LogTrace
      (
        $"{AddinName} Loaded",
        $"AddIn.StartupMode = {StartupMode}",
        $"AddIn.CurrentStatus = {CurrentStatus}"
      );
    }

    public AddIn() : base(new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74")) => Instance = this;

    ~AddIn() => Instance = default;

    internal static AddIn Instance { get; set; }
    public static AddInId Id => Instance;
    #endregion

    #region IExternalApplication Members
    static UIX.UIHostApplication host;
    internal static UIX.UIHostApplication Host
    {
      get => host;
      private set { if (!ReferenceEquals(host, value)) { host?.Dispose(); host = value; } }
    }

    protected override Result OnStartup(UIControlledApplication uiCtrlApp)
    {
      using (Logger.LogScope())
      {
        // Check if the AddIn can startup
        {
          var result = CanStartup(uiCtrlApp);
          if (result != Result.Succeeded) return result;
        }

        ErrorReport.OnLoadStackTraceFilePath =
          Path.ChangeExtension(uiCtrlApp.ControlledApplication.RecordingJournalFilename, "log.md");

        Host = uiCtrlApp;
        External.ActivationGate.SetHostWindow(Host.MainWindowHandle);
        AssemblyResolver.Enabled = true;

        // Initialize DB
        {
          // Register Revit Failures
          External.DB.ExternalFailures.CreateFailureDefinitions();

          if (uiCtrlApp.IsLateAddinLoading)
          {
            EventHandler<Autodesk.Revit.UI.Events.IdlingEventArgs> applicationIdling = null;
            Host.Idling += applicationIdling = (sender, args) =>
            {
              if (sender is UIApplication app)
              {
                Host.Idling -= applicationIdling;
                DoStartUp(app.Application);
              }
            };
          }
          else
          {
            EventHandler<ApplicationInitializedEventArgs> applicationInitialized = null;
            Host.Services.ApplicationInitialized += applicationInitialized = (sender, args) =>
            {
              Host.Services.ApplicationInitialized -= applicationInitialized;
              if (CurrentStatus < Status.Available) return;
              DoStartUp(sender as Autodesk.Revit.ApplicationServices.Application);
            };
          }
        }

        // Initialize UI
        {
          UI.CommandStart.CreateUI(uiCtrlApp);
        }

        // Check For Updates
        {
          AddinOptions.UpdateChannelChanged += (sender, args) => CheckForUpdates();
          if (AddinOptions.Current.CheckForUpdatesOnStartup) CheckForUpdates();
        }

        return Result.Succeeded;
      }
    }

    void DoStartUp(Autodesk.Revit.ApplicationServices.Application app)
    {
      Host = new UIApplication(app);

      if (StartupMode < AddInStartupMode.AtStartup && !AddinOptions.Session.LoadOnStartup)
        return;

      if (UI.CommandStart.Start() == Result.Succeeded)
      {
        if (StartupMode == AddInStartupMode.Scripting)
          Host.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit));
      }
    }

    protected override Result OnShutdown(UIControlledApplication uiCtrlApp)
    {
      using (Logger.LogScope())
      {
        try
        {
          return Revit.Shutdown();
        }
        catch
        {
          return Result.Failed;
        }
        finally
        {
          AssemblyResolver.Enabled = false;
          External.ActivationGate.SetHostWindow(IntPtr.Zero);
          Host = null;
        }
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
      // A serious error has occurred. The current action has ben cancelled.
      // It is stringly recommended that you save your work in a new file before continuing.
      //
      // Would you like to save a recovery file? "{TileName}(Recovery)".rvt

      var RhinoInside_dmp = Path.Combine
      (
        Path.GetDirectoryName(app.Application.RecordingJournalFilename),
        Path.GetFileNameWithoutExtension(app.Application.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
      );

      ReportException(e, app, new string[] { RhinoInside_dmp });
    }

    public static void ReportException(Exception e, UIX.UIHostApplication app, IEnumerable<string> attachments)
    {
      // Show the most inner exception
      while (e.InnerException is object)
        e = e.InnerException;

      if (MessageBox.Show
      (
        owner: app.GetMainWindow(),
        caption: $"{app.ActiveAddInId.GetAddInName()} {Version} - Oops! Something went wrong :(",
        icon: MessageBoxImage.Error,
        messageBoxText: $"'{e.GetType().FullName}' at {e.Source}." + Environment.NewLine +
                        Environment.NewLine + e.Message + Environment.NewLine +
                        Environment.NewLine + "Do you want to report this problem by email to tech@mcneel.com?",
        button: MessageBoxButton.YesNo,
        defaultResult: MessageBoxResult.Yes
      ) == MessageBoxResult.Yes)
      {
        ErrorReport.SendEmail
        (
          app,
          $"Rhino.Inside Revit failed - {e.GetType().FullName}",
          false,
          Enumerable.Repeat(app.Services.RecordingJournalFilename, 1).Concat(attachments)
        );
      }
    }
    #endregion

    #region Version
#if REVIT_2022
    static readonly Version MinimumRevitVersion = new Version(2022, 0);
#elif REVIT_2021
    static readonly Version MinimumRevitVersion = new Version(2021, 0);
#elif REVIT_2020
    static readonly Version MinimumRevitVersion = new Version(2020, 0);
#elif REVIT_2019
    static readonly Version MinimumRevitVersion = new Version(2019, 1);
#elif REVIT_2018
    static readonly Version MinimumRevitVersion = new Version(2018, 2);
#endif

    static Result CanStartup(UIControlledApplication app)
    {
      if (StartupMode == AddInStartupMode.Cancelled)
        return Result.Cancelled;

      // Check if Revit.exe is a supported version
      var RevitVersion = new Version(app.ControlledApplication.GetSubVersionNumber());
      if (RevitVersion < MinimumRevitVersion)
      {
        using
        (
          var taskDialog = new TaskDialog("Update Revit")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.UpdateRevit",
            MainIcon = UIX.TaskDialogIcons.IconInformation,
            AllowCancellation = true,
            MainInstruction = "Unsupported Revit version",
            MainContent = $"Expected Revit version is ({MinimumRevitVersion}) or above.",
            ExpandedContent =
            (RhinoVersionInfo is null ? "Rhino\n" :
            $"{RhinoVersionInfo.ProductName} {RhinoVersionInfo.ProductMajorPart}\n") +
            $"• Version: {RhinoVersion}\n" +
            $"• Path: '{SystemDir}'" + (!File.Exists(RhinoExePath) ? " (not found)" : string.Empty) + "\n" +
            $"\n{app.ControlledApplication.VersionName}\n" +
            $"• Version: {RevitVersion} ({app.ControlledApplication.VersionBuild})\n" +
            $"• Path: {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\n" +
            $"• Language: {app.ControlledApplication.Language}",
            FooterText = $"Current Revit version: {RevitVersion}"
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, $"Revit {RevitVersion.Major} Product Updates…");
          if (taskDialog.Show() == TaskDialogResult.CommandLink1)
          {
            using (Process.Start($@"https://knowledge.autodesk.com/support/revit-products/troubleshooting/caas/downloads/content/autodesk-revit-{RevitVersion.Major}-product-updates.html")) { }
          }
        }

        return Result.Cancelled;
      }

      return Result.Succeeded;
    }

    static async void CheckForUpdates()
    {
      var releaseInfo = await AddinUpdater.GetReleaseInfoAsync();

      // if release info is received, and
      // if current version on the active update channel is newer
      if (releaseInfo is ReleaseInfo && releaseInfo.Version > Version)
      {
        // ask UI to notify user of updates
        if (!AddinOptions.Session.CompactTab)
          UI.CommandStart.NotifyUpdateAvailable(releaseInfo);

        UI.CommandAddinOptions.NotifyUpdateAvailable(releaseInfo);
      }
      else
      {
        // otherwise clear updates
        UI.CommandStart.ClearUpdateNotifiy();
        UI.CommandAddinOptions.ClearUpdateNotifiy();
      }
    }

    static bool CheckIsExpired(int minDaysUntilExpiration = int.MaxValue)
    {
      var expired = IsExpired(out var daysUntilExpiration);
      if (!expired && daysUntilExpiration >= minDaysUntilExpiration)
        return false;

      using
      (
        var taskDialog = new TaskDialog("Days left")
        {
          Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
          MainIcon = UIX.TaskDialogIcons.IconInformation,
          TitleAutoPrefix = true,
          AllowCancellation = true,
          MainInstruction = expired ?
          $"{AddinName} has expired" :
          $"{AddinName} expires in {daysUntilExpiration} days",
          MainContent = $"While in WIP phase, you do need to update {AddinName} every {ExpirationPeriod} days.",
          FooterText = "Current version: " + DisplayVersion
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Check for updates…", $"Open {AddinName} download page");
        if (taskDialog.Show() == TaskDialogResult.CommandLink1)
        {
          using (Process.Start(@"https://www.rhino3d.com/download/rhino.inside-revit/7")) { }
        }
      }

      return expired;
    }

    internal static Result CheckSetup()
    {
      var services = Host.Services;

      // Check if Rhino.Inside is expired
      if (CheckIsExpired(minDaysUntilExpiration: 10))
        return Result.Cancelled;

      // Check if Rhino.exe is a supported version
      if (RhinoVersion < MinimumRhinoVersion)
      {
        using
        (
          var taskDialog = new TaskDialog("Update Rhino")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.UpdateRhino",
            MainIcon = UIX.TaskDialogIcons.IconInformation,
            AllowCancellation = true,
            MainInstruction = "Unsupported Rhino version",
            MainContent = $"Expected Rhino version is ({MinimumRhinoVersion}) or above.",
            ExpandedContent =
            (RhinoVersionInfo is null ? "Rhino\n" :
            $"{RhinoVersionInfo.ProductName} {RhinoVersionInfo.ProductMajorPart}\n") +
            $"• Version: {RhinoVersion}\n" +
            $"• Path: '{SystemDir}'" + (!File.Exists(RhinoExePath) ? " (not found)" : string.Empty) + "\n" +
            $"\n{services.VersionName}\n" +
            $"• Version: {services.SubVersionNumber} ({services.VersionBuild})\n" +
            $"• Path: {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\n" +
            $"• Language: {services.Language}",
            FooterText = $"Current Rhino version: {RhinoVersion}"
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Download latest Rhino…");
          if (taskDialog.Show() == TaskDialogResult.CommandLink1)
          {
            using (Process.Start(@"https://www.rhino3d.com/download/rhino/7.0/latest")) { }
          }
        }

        return Result.Cancelled;
      }

      return Result.Succeeded;
    }

    static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
    internal static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());
    static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);

    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public static string DisplayVersion => $"{Version} ({BuildDate})";

    // Set ExpirationPeriod to 0 for non expirable builds.
    const int ExpirationPeriod = 90; // days.

    internal static bool IsExpired() => IsExpired(out var _);
    internal static bool IsExpired(out int daysUntilExpiration)
    {
#pragma warning disable CS0162 // Unreachable code detected
      if (ExpirationPeriod > 0)
      {
        daysUntilExpiration = Math.Max(0, ExpirationPeriod - (DateTime.Now - BuildDate).Days);
        return daysUntilExpiration < 1;
      }
      else
      {
        daysUntilExpiration = int.MaxValue;
        return false;
      }
#pragma warning restore CS0162 // Unreachable code detected
    }
    #endregion
  }
}
