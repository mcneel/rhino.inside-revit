using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using RhinoInside.Revit.Diagnostics;
using RhinoInside.Revit.External.ApplicationServices.Extensions;
using RhinoInside.Revit.Native;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit
{
  enum CoreStartupMode
  {
    Cancelled = -2,
    Disabled = -1,
    Default = 0,
    WhenNeeded = 1,
    OnStartup = 2,
    Scripting = 3
  }

  internal static class Core
  {
    #region ProductInfo
    public static string Company => "McNeel";
    public static string Product => "Rhino.Inside";
    public static string WebSite => $@"https://www.rhino3d.com/inside/revit/{Version.Major}.0/";

    internal static readonly string SwapFolder = Path.Combine(Path.GetTempPath(), Company, Product, $"V{Core.Version.Major}.{Core.Version.Minor}");
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

    #region Startup Settings
    static CoreStartupMode GetStartupMode()
    {
      if (!Enum.TryParse(Environment.GetEnvironmentVariable("RhinoInside_StartupMode"), out CoreStartupMode mode))
        mode = CoreStartupMode.Default;

      if (mode == CoreStartupMode.Default)
        mode = CoreStartupMode.WhenNeeded;

      return mode;
    }
    internal static readonly CoreStartupMode StartupMode = GetStartupMode();
    internal static bool IsolateSettings = true;
    internal static bool UseHostLanguage = true;
    internal static bool KeepUIOnTop = true;
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

    static Core()
    {
      if (StartupMode == CoreStartupMode.Cancelled)
        return;

      if (IsExpired())
        status = Status.Obsolete;
      else if (RhinoVersion >= MinimumRhinoVersion)
        status = NativeLoader.IsolateOpenNurbs() ? Status.Available : Status.Unavailable;

      Logger.LogTrace
      (
        $"{Product} Loaded",
        $"{nameof(Core)}.StartupMode = {StartupMode}",
        $"{nameof(Core)}.CurrentStatus = {CurrentStatus}"
      );
    }
    #endregion

    #region IExternalApplication Members
    static UIX.UIHostApplication host;
    internal static UIX.UIHostApplication Host
    {
      get => host;
      private set { if (!ReferenceEquals(host, value)) { host?.Dispose(); host = value; } }
    }

    internal static Result OnStartup
    (
      UIControlledApplication uiCtrlApp
    )
    {
      if (uiCtrlApp.IsLateAddinLoading) return Result.Failed;

      using (Logger.LogScope())
      {
        // Check if the AddIn can startup
        {
          var result = CanStartup(uiCtrlApp);
          if (result != Result.Succeeded) return result;
        }

        // Ensure we have a SwapFolder
        Directory.CreateDirectory(SwapFolder);

        ErrorReport.OnLoadStackTraceFilePath =
          Path.ChangeExtension(uiCtrlApp.ControlledApplication.RecordingJournalFilename, "log.md");

        Host = uiCtrlApp;
        External.ActivationGate.SetHostWindow(Host.MainWindowHandle);
        AssemblyResolver.Enabled = true;

        // Initialize DB
        {
          // Register Revit Failures
          External.DB.ExternalFailures.CreateFailureDefinitions();

          EventHandler<ApplicationInitializedEventArgs> applicationInitialized = null;
          Host.Services.ApplicationInitialized += applicationInitialized = (sender, args) =>
          {
            Host.Services.ApplicationInitialized -= applicationInitialized;
            if (CurrentStatus < Status.Available) return;
            Host = new UIApplication(sender as Autodesk.Revit.ApplicationServices.Application);
          };
        }

        return Result.Succeeded;
      }
    }

    internal static Result OnShutdown(UIControlledApplication uiCtrlApp)
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

    public static void ReportException(Exception e, UIX.UIHostApplication app)
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
        var attachments = e.Data["Attachments"] as IEnumerable<string> ?? Enumerable.Empty<string>();
        ErrorReport.SendEmail
        (
          app,
          $"Rhino.Inside Revit failed - {e.GetType().FullName}",
          false,
          attachments.Prepend(app.Services.RecordingJournalFilename)
        );
      }
    }
    #endregion

    #region Version
#if REVIT_2022
    static readonly Version MinimumRevitVersion = new Version(2022, 0);
#elif REVIT_2021
    static readonly Version MinimumRevitVersion = new Version(2021, 1);
#elif REVIT_2020
    static readonly Version MinimumRevitVersion = new Version(2020, 0);
#elif REVIT_2019
    static readonly Version MinimumRevitVersion = new Version(2019, 1);
#elif REVIT_2018
    static readonly Version MinimumRevitVersion = new Version(2018, 2);
#endif

    static Result CanStartup(UIControlledApplication app)
    {
      if (StartupMode == CoreStartupMode.Cancelled)
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
    #endregion

    #region Expiration
    const int NeverExpire = 0;

    /// <summary>
    /// Expiration period in days. 0 means never expire.
    /// </summary>
    static readonly int ExpirationPeriod =
      Assembly.GetExecutingAssembly().
      GetCustomAttribute<AssemblyInformationalVersionAttribute>().
      InformationalVersion.ToLowerInvariant().
      Contains("-wip") ? 90 : NeverExpire;

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
          $"{Product} has expired" :
          $"{Product} expires in {daysUntilExpiration} days",
          MainContent = $"While in WIP phase, you do need to update {Product} every {ExpirationPeriod} days.",
          FooterText = "Current version: " + DisplayVersion
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Check for updates…", $"Open {Product} download page");
        if (taskDialog.Show() == TaskDialogResult.CommandLink1)
        {
          using (Process.Start(@"https://www.rhino3d.com/download/rhino.inside-revit/7")) { }
        }
      }

      return expired;
    }

    internal static bool IsExpired() => IsExpired(out var _);
    internal static bool IsExpired(out int daysUntilExpiration)
    {
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
    }
    #endregion
  }
}
