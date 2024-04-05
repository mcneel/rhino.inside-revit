using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;
using ERUI = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit
{
  using static Diagnostics;

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
    public static string Platform => "Revit";
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
    internal static ARUI.ExternalEvent ActivationEvent = default;
    #endregion

    #region Constructor
    static Version _MinimumRhinoVersion;
    static Version MinimumRhinoVersion
    {
      get
      {
        if (_MinimumRhinoVersion is null)
        {
          var referencedRhinoCommonVersion = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(x => x.Name == "RhinoCommon").FirstOrDefault().Version;
          _MinimumRhinoVersion = new Version(referencedRhinoCommonVersion.Major, referencedRhinoCommonVersion.Minor, 0);
        }

        return _MinimumRhinoVersion;
      }
    }

    internal static readonly Distribution Distribution = Distribution.Default(MinimumRhinoVersion.Major);
    static readonly Version RhinoVersion = Distribution.ExeVersion();

    static Core()
    {
      if (StartupMode == CoreStartupMode.Cancelled)
        return;

      if (IsExpired())
        status = Status.Obsolete;
      else if (RhinoVersion >= MinimumRhinoVersion)
        status = Status.Available;

      Logger.LogTrace
      (
        $"{Product} Loaded",
        $"{nameof(Core)}.StartupMode = {StartupMode}",
        $"{nameof(Core)}.CurrentStatus = {CurrentStatus}"
      );
    }
    #endregion

    #region IExternalApplication
    internal static ERUI.UIHostApplication Host => ERUI.UIHostApplication.Current;

    internal static ARUI.Result OnStartup(ARUI.UIControlledApplication uiCtrlApp)
    {
      if (uiCtrlApp.IsLateAddinLoading) return ARUI.Result.Failed;

      using (var scope = Logger.LogScope())
      {
        // Check if the AddIn can startup
        {
          scope.LogTrace("Starting…", DisplayVersion);
          var result = Startup(uiCtrlApp);
          if (result != ARUI.Result.Succeeded) return result;
        }

        // Ensure we have a SwapFolder
        scope.LogTrace("Creating SwapFolder…", SwapFolder);
        Directory.CreateDirectory(SwapFolder);

        ErrorReport.OnLoadStackTraceFilePath =
          Path.ChangeExtension(uiCtrlApp.ControlledApplication.RecordingJournalFilename, "log.md");

        AssemblyResolver.Enabled = true;

        // Initialize DB
        {
          EventHandler<ARDB.Events.ApplicationInitializedEventArgs> applicationInitialized = null;
          Host.Services.ApplicationInitialized += applicationInitialized = (sender, args) =>
          {
            Host.Services.ApplicationInitialized -= applicationInitialized;
            if (CurrentStatus < Status.Available) return;

            var app = sender as Autodesk.Revit.ApplicationServices.Application;
            Convert.Geometry.GeometryTolerance.Internal = new Convert.Geometry.GeometryTolerance
            (
              Numerical.Constant.DefaultTolerance,
              app.AngleTolerance,
              app.VertexTolerance,
              app.ShortCurveTolerance
            );
          };
        }

        return ARUI.Result.Succeeded;
      }
    }

    internal static ARUI.Result OnShutdown(ARUI.UIControlledApplication uiCtrlApp)
    {
      using (Logger.LogScope())
      {
        try
        {
          return Revit.Shutdown();
        }
        catch
        {
          return ARUI.Result.Failed;
        }
        finally
        {
          AssemblyResolver.Enabled = false;
        }
      }
    }
    #endregion

    #region Version
#if REVIT_2025
    static readonly Version MinimumRevitVersion = new Version(2025, 0);
#elif REVIT_2024
    static readonly Version MinimumRevitVersion = new Version(2024, 0);
#elif REVIT_2023
    static readonly Version MinimumRevitVersion = new Version(2023, 0);
#elif REVIT_2022
    static readonly Version MinimumRevitVersion = new Version(2022, 1);
#elif REVIT_2021
    static readonly Version MinimumRevitVersion = new Version(2021, 1);
#elif REVIT_2020
    static readonly Version MinimumRevitVersion = new Version(2020, 0);
#elif REVIT_2019
    static readonly Version MinimumRevitVersion = new Version(2019, 1);
#elif REVIT_2018
    static readonly Version MinimumRevitVersion = new Version(2018, 2);
#elif REVIT_2017
    static readonly Version MinimumRevitVersion = new Version(2017, 0);
#endif

    static ARUI.Result Startup(ERUI.UIHostApplication app)
    {
      if (StartupMode == CoreStartupMode.Cancelled)
        return ARUI.Result.Cancelled;

      // Check if Revit.exe is a supported version
      var RevitVersion = new Version(app.Services.SubVersionNumber);
      if (RevitVersion.Major != MinimumRevitVersion.Major)
      {
        app.Services.WriteJournalComment($"Expected Revit version is ({MinimumRevitVersion}) or above!!", timeStamp: false);
        return ARUI.Result.Cancelled;
      }

      if (RevitVersion < MinimumRevitVersion)
      {
        using
        (
          var taskDialog = new ARUI.TaskDialog("Update Revit")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.UpdateRevit",
            MainIcon = ERUI.TaskDialogIcons.IconWarning,
            AllowCancellation = true,
            MainInstruction = "Unsupported Revit version",
            MainContent = $"Please update Revit to version {MinimumRevitVersion} or higher.",
            ExpandedContent =
            (Distribution.VersionInfo is null ? "Rhino\n" :
            $"{Distribution.VersionInfo.ProductName} {Distribution.VersionInfo.ProductMajorPart}\n") +
            $"• Version: {RhinoVersion}\n" +
            $"• Path: '{Distribution.Path}'" + (!File.Exists(Distribution.ExePath) ? " (not found)" : string.Empty) + "\n" +
            $"\n{app.Services.VersionName}\n" +
            $"• Version: {RevitVersion} ({app.Services.VersionBuild})\n" +
            $"• Path: {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\n" +
            $"• Language: {app.Services.Language}",
            FooterText = $"Current Revit version: {RevitVersion}"
          }
        )
        {
          taskDialog.AddCommandLink(ARUI.TaskDialogCommandLinkId.CommandLink1, $"Revit {RevitVersion.Major} Product Updates…");
          taskDialog.AddCommandLink(ARUI.TaskDialogCommandLinkId.CommandLink2, $"Continue without updating Revit…", $"Running {Core.Product} in Revit version {RevitVersion} is not supported.");

          switch(taskDialog.Show())
          {
            case ARUI.TaskDialogResult.CommandLink1:
              using (Process.Start($@"https://knowledge.autodesk.com/support/revit?s=Download%20Updates&v={RevitVersion.Major}&sort=score")) { }
              return ARUI.Result.Cancelled;

            case ARUI.TaskDialogResult.CommandLink2:
              app.Services.WriteJournalComment($"Minimum Revit version is ({MinimumRevitVersion}) or above!!", timeStamp: false);
              break;

            default:
              return ARUI.Result.Cancelled;
          }
        }
      }

      // Check if we have 'opennurbs_private.manifest' file on Revit folder.
      if (status == Status.Available)
      {
        if (Status.Available != (status = NativeLoader.IsolateOpenNurbs(app.MainWindowHandle) ? Status.Available : Status.Unavailable))
          return ARUI.Result.Failed;
      }

      return ARUI.Result.Succeeded;
    }

    internal static ARUI.Result CheckSetup()
    {
      var services = Host.Services;

      // Check if Rhino.Inside is expired
      if (CheckIsExpired(minDaysUntilExpiration: 10))
        return ARUI.Result.Cancelled;

      // Check if Rhino.exe is a supported version
      if (RhinoVersion < MinimumRhinoVersion)
      {
        using
        (
          var taskDialog = new ARUI.TaskDialog("Update Rhino")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.UpdateRhino",
            MainIcon = ERUI.TaskDialogIcons.IconError,
            AllowCancellation = true,
            MainInstruction = "Unsupported Rhino version",
            MainContent = $"Expected Rhino version is ({MinimumRhinoVersion.Major}.{MinimumRhinoVersion.Minor}) or above.",
            ExpandedContent =
            (Distribution.VersionInfo is null ? "Rhino\n" :
            $"{Distribution.VersionInfo.ProductName} {Distribution.VersionInfo.ProductMajorPart}\n") +
            $"• Version: {RhinoVersion}\n" +
            $"• Path: '{Distribution.Path}'" + (!File.Exists(Distribution.ExePath) ? " (not found)" : string.Empty) + "\n" +
            $"\n{services.VersionName}\n" +
            $"• Version: {services.SubVersionNumber} ({services.VersionBuild})\n" +
            $"• Path: {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\n" +
            $"• Language: {services.Language}",
            FooterText = $"Current Rhino version: {RhinoVersion}"
          }
        )
        {
          taskDialog.AddCommandLink(ARUI.TaskDialogCommandLinkId.CommandLink1, "Download latest Rhino…");
          if (taskDialog.Show() == ARUI.TaskDialogResult.CommandLink1)
          {
            using (Process.Start($@"https://www.rhino3d.com/download/rhino/{MinimumRhinoVersion.Major}.0/")) { }
          }
        }

        return ARUI.Result.Cancelled;
      }

      return ARUI.Result.Succeeded;
    }

    static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
    internal static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());
    static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);

    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public static string DisplayVersion => $"{Version} ({BuildDate:s})";
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
        var taskDialog = new ARUI.TaskDialog("Days left")
        {
          Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}",
          MainIcon = ERUI.TaskDialogIcons.IconInformation,
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
        taskDialog.AddCommandLink(ARUI.TaskDialogCommandLinkId.CommandLink1, "Check for updates…", $"Open {Product} download page");
        if (taskDialog.Show() == ARUI.TaskDialogResult.CommandLink1)
        {
          using (Process.Start(@"https://www.rhino3d.com/download/rhino.inside-revit/1/latest")) { }
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
