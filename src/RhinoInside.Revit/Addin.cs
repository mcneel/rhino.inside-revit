using System;
using System.Collections.Generic;
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
using Microsoft.Win32.SafeHandles;
using RhinoInside.Revit.External.UI.Extensions;
using RhinoInside.Revit.Native;
using RhinoInside.Revit.Settings;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit
{
  static class AssemblyResolver
  {
    static readonly string InstallPath =
#if DEBUG
      Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\McNeel\Rhinoceros\7.0-WIP-Developer-Debug-trunk\Install", "InstallPath", null) as string ??
#endif
      Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "InstallPath", null) as string ??
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP");

    static readonly FieldInfo _AssemblyResolve = typeof(AppDomain).GetField("_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic);

    struct AssemblyLocation
    {
      public AssemblyLocation(AssemblyName name, string location) { Name = name; Location = location; Assembly = default; }
      public readonly AssemblyName Name;
      public readonly string Location;
      public Assembly Assembly;
    }

    static readonly Dictionary<string, AssemblyLocation> AssemblyLocations = new Dictionary<string, AssemblyLocation>();
    static AssemblyResolver()
    {
      try
      {
        var installFolder = new DirectoryInfo(InstallPath);
        foreach (var dll in installFolder.EnumerateFiles("*.dll", SearchOption.AllDirectories))
        {
          try
          {
            if (dll.Extension.ToLower() != ".dll") continue;

            var assemblyName = AssemblyName.GetAssemblyName(dll.FullName);

            if (AssemblyLocations.TryGetValue(assemblyName.Name, out var location))
            {
              if (location.Name.Version > assemblyName.Version) continue;
              AssemblyLocations.Remove(assemblyName.Name);
            }

            AssemblyLocations.Add(assemblyName.Name, new AssemblyLocation(assemblyName, dll.FullName));
          }
          catch { }
        }
      }
      catch { }
    }

    static bool enabled;
    public static bool Enabled
    {
      get => enabled;
      set
      {
        if (enabled != value)
        {
          if (value)
          {
            if (_AssemblyResolve is null) AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            else
            {
              External.ActivationGate.Enter += ActivationGate_Enter;
              External.ActivationGate.Exit += ActivationGate_Exit;
            }
          }
          else
          {
            if (_AssemblyResolve is null) AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            else
            {
              External.ActivationGate.Exit -= ActivationGate_Exit;
              External.ActivationGate.Enter -= ActivationGate_Enter;
            }
          }
          enabled = value;
        }
      }
    }

    static void ActivationGate_Enter(object sender, EventArgs e)
    {
      var domain = AppDomain.CurrentDomain;
      var assemblyResolve = _AssemblyResolve.GetValue(domain) as ResolveEventHandler;
      var invocationList = assemblyResolve.GetInvocationList();

      foreach (var invocation in invocationList)
        domain.AssemblyResolve -= invocation as ResolveEventHandler;

      domain.AssemblyResolve += AssemblyResolve;

      foreach (var invocation in invocationList)
        AppDomain.CurrentDomain.AssemblyResolve += invocation as ResolveEventHandler;
    }

    static void ActivationGate_Exit(object sender, EventArgs e)
    {
      AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
    }

    static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
    {
      var assemblyName = new AssemblyName(args.Name).Name;
      if (!AssemblyLocations.TryGetValue(assemblyName, out var location))
        return null;

      if (location.Assembly is null)
      {
        // Remove it to not try again if it fails
        AssemblyLocations.Remove(assemblyName);

        // Add again loaded assembly
        location.Assembly = Assembly.LoadFrom(location.Location);
        AssemblyLocations.Add(assemblyName, location);
      }

      return location.Assembly;
    }
  }

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
#if DEBUG
      Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\McNeel\Rhinoceros\7.0-WIP-Developer-Debug-trunk\Install", "Path", null) as string ??
#endif
      Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path", null) as string ??
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP", "System");

    internal static readonly string RhinoExePath = Path.Combine(SystemDir, "Rhino.exe");
    internal static readonly FileVersionInfo RhinoVersionInfo = File.Exists(RhinoExePath) ? FileVersionInfo.GetVersionInfo(RhinoExePath) : null;
    static readonly Version MinimumRhinoVersion = new Version(7, 0, 20314);
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

      NativeLoader.IsolateOpenNurbs();

      // initialize ui framework provided by Rhino
      RhinoUIFramework.LoadFramework(SystemDir);
    }

    public Addin() : base(new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74")) => Instance = this;

    ~Addin() => Instance = default;

    internal static Addin Instance { get; set; }
    public static AddInId Id => Instance;
    #endregion

    #region IExternalApplication Members
    internal static UIControlledApplication ApplicationUI { get; private set; }

    protected override Result OnStartup(UIControlledApplication applicationUI)
    {
      if (!CanLoad(applicationUI))
        return Result.Failed;

      if (StartupMode == AddinStartupMode.Cancelled)
        return Result.Cancelled;

      // Report if opennurbs.dll is loaded
      NativeLoader.SetStackTraceFilePath
      (
        Path.ChangeExtension(applicationUI.ControlledApplication.RecordingJournalFilename, "log.md")
      );

      NativeLoader.ReportOnLoad("opennurbs.dll", enable: true);

      AssemblyResolver.Enabled = true;
      ApplicationUI = applicationUI;

      // Register Revit Failures
      External.DB.ExternalFailures.CreateFailureDefinitions();

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
      var addinRibbon = applicationUI.CreateRibbonPanel("Rhinoceros");
      UI.CommandRhinoInside.CreateUI(addinRibbon);
      UI.CommandRhinoInsideOptions.CreateUI(addinRibbon);

      // check for updates
      if (AddinOptions.CheckForUpdatesOnStartup)
        AddinUpdater.GetReleaseInfo(
          (ReleaseInfo releaseInfo) =>
          {
            // if release info is received,
            if (releaseInfo != null) {
              // if current version on the active update channel is newer
              if (releaseInfo.Version > Version)
                // ask UI to notify user of updates
                UI.CommandRhinoInsideOptions.NotifyUpdateAvailable(releaseInfo);
            }
          }
        );

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
        AssemblyResolver.Enabled = false;
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

      // Show the most inner exception
      while (e.InnerException is object)
        e = e.InnerException;

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
          $"Rhino.Inside Revit failed - {e.GetType().FullName}",
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
    internal static bool CheckIsExpired(bool quiet = true)
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
          "Rhino.Inside WIP has expired" :
          $"Rhino.Inside WIP expires in {DaysUntilExpiration} days",
          MainContent = "While in WIP phase, you do need to update Rhino.Inside addin at least every 45 days.",
          FooterText = "Current version: " + DisplayVersion
        }
      )
      {
        taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Check for updates…", "Open Rhino.Inside download page");
        if (taskDialog.Show() == TaskDialogResult.CommandLink1)
        {
          using (Process.Start(@"https://www.rhino3d.com/download/rhino.inside-revit/7/wip")) { }
        }
      }

      return DaysUntilExpiration < 1;
    }

    static bool IsValid(UIControlledApplication app)
    {
#if REVIT_2021
      return app.ControlledApplication.VersionNumber == "2021";
#elif REVIT_2020
      return app.ControlledApplication.VersionNumber == "2020";
#elif REVIT_2019
      return app.ControlledApplication.VersionNumber == "2019";
#elif REVIT_2018
      return app.ControlledApplication.VersionNumber == "2018";
#elif REVIT_2017
      return app.ControlledApplication.VersionNumber == "2017";
#else
      return false;
#endif
    }

    static bool CanLoad(UIControlledApplication app)
    {
      return IsValid(app);
    }

    internal static Result CheckSetup(UIControlledApplication app)
    {
      var revit = app.ControlledApplication;

      // Check if Rhino.Inside is expired
      if (CheckIsExpired(DaysUntilExpiration > 10))
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
            MainInstruction = "Unsupported Rhino WIP version",
            MainContent = $"Expected Rhino version is ({MinimumRhinoVersion}) or above.",
            ExpandedContent =
            RhinoVersionInfo is null ? "Rhino\n" :
            $"{RhinoVersionInfo.ProductName} {RhinoVersionInfo.ProductMajorPart}\n" +
            $"• Version: {RhinoVersion}\n" +
            $"• Path: '{SystemDir}'" + (!File.Exists(RhinoExePath) ? " (not found)" : string.Empty) + "\n" +
            $"\n{revit.VersionName}\n" +
#if REVIT_2019
            $"• Version: {revit.SubVersionNumber} ({revit.VersionBuild})\n" +
#else
            $"• Version: {revit.VersionNumber} ({revit.VersionBuild})\n" +
#endif
            $"• Path: {Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\n" +
            $"• Language: {revit.Language}",
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

      // Check if 'opennurbs.dll' is already loaded
      var openNURBS = LibraryHandle.GetLoadedModule("opennurbs.dll");
      if (openNURBS != LibraryHandle.Zero)
      {
        var openNURBSVersion = FileVersionInfo.GetVersionInfo(openNURBS.ModuleFileName);

        using
        (
          var taskDialog = new TaskDialog($"Rhino.Inside {Version} - openNURBS Conflict")
          {
            Id = $"{MethodBase.GetCurrentMethod().DeclaringType}.{MethodBase.GetCurrentMethod().Name}.OpenNURBSConflict",
            MainIcon = UIX.TaskDialogIcons.IconError,
            TitleAutoPrefix = false,
            AllowCancellation = true,
            MainInstruction = "An unsupported openNURBS version is already loaded. Rhino.Inside cannot run.",
            MainContent = "Please restart Revit and load Rhino.Inside first to work around the problem.",
            FooterText = $"Currently loaded openNURBS version: {openNURBSVersion.FileMajorPart}.{openNURBSVersion.FileMinorPart}.{openNURBSVersion.FileBuildPart}.{openNURBSVersion.FilePrivatePart}"
          }
        )
        {
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "More information…");
          taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Report Error…", "by email to tech@mcneel.com");
          taskDialog.DefaultButton = TaskDialogResult.CommandLink2;
          switch(taskDialog.Show())
          {
            case TaskDialogResult.CommandLink1:
              using (Process.Start(@"https://www.rhino3d.com/inside/revit/beta/reference/known-issues")) { }
              break;
            case TaskDialogResult.CommandLink2:

              var RhinoInside_dmp = Path.Combine
              (
                Path.GetDirectoryName(revit.RecordingJournalFilename),
                Path.GetFileNameWithoutExtension(revit.RecordingJournalFilename) + ".RhinoInside.Revit.dmp"
              );

              MiniDumper.Write(RhinoInside_dmp);

              ErrorReport.SendEmail
              (
                app,
                $"Rhino.Inside Revit failed - openNURBS Conflict",
                true,
                new string[]
                {
                  revit.RecordingJournalFilename,
                  RhinoInside_dmp
                }
              );

              CurrentStatus = Status.Failed;
              break;
          }
        }

        return Result.Cancelled;
      }

      // Disable report opennurbs.dll is loaded 
      NativeLoader.ReportOnLoad("opennurbs.dll", enable: false);

      return Result.Succeeded;
    }

    static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
    public static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());
    public static DateTime BuildDate => new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
    public static int DaysUntilExpiration => Math.Max(0, 45 - (DateTime.Now - BuildDate).Days);

    public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public static string DisplayVersion => $"{Version} ({BuildDate})";
    #endregion

    #region Rhino-friendly UI Framework
    static class RhinoUIFramework
    {
      /// <summary>
      /// Loads assemblies related to the Rhino ui framework from given Rhino system directory
      /// </summary>
      /// <param name="sysDir"></param>
      static public void LoadFramework(string sysDir)
      {
        Assembly.LoadFrom(Path.Combine(sysDir, "Eto.dll"));
        Assembly.LoadFrom(Path.Combine(sysDir, "Eto.Wpf.dll"));
        Assembly.LoadFrom(Path.Combine(sysDir, "Eto.Serialization.Xaml.dll"));
        Assembly.LoadFrom(Path.Combine(sysDir, "Xceed.Wpf.Toolkit.dll"));

        Init();
      }

      /// <summary>
      /// Initialize the ui framework
      /// </summary>
      static void Init()
      {
        if (Eto.Forms.Application.Instance is null)
          new Eto.Forms.Application(Eto.Platforms.Wpf).Attach();
      }
    }

    #endregion
  }
}

namespace RhinoInside.Revit.UI
{
  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoInside : Command
  {
    static PushButton Button;

    new class Availability : External.UI.CommandAvailability
    {
      public override bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
        Addin.CurrentStatus >= Addin.Status.Obsolete;
    }

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
          pushButton.Image = ImageBuilder.LoadBitmapImage("Resources.Rhino-logo.png", true);
          pushButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Rhino-logo.png");
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

          var assemblies = AppDomain.CurrentDomain.GetAssemblies();

          if (assemblies.Any(x => x.GetName().Name == "RhinoCommon"))
          {
            RhinocerosPanel.AddSeparator();
            CommandRhino.CreateUI(RhinocerosPanel);
            CommandImport.CreateUI(RhinocerosPanel);
            CommandRhinoPreview.CreateUI(RhinocerosPanel);
            CommandPython.CreateUI(RhinocerosPanel);
          }

          if (assemblies.Any(x => x.GetName().Name == "Grasshopper"))
          {
            var GrasshopperPanel = data.Application.CreateRibbonPanel(rhinoTab, "Grasshopper");
            CommandGrasshopper.CreateUI(GrasshopperPanel);
            CommandGrasshopperPreview.CreateUI(GrasshopperPanel);
            CommandGrasshopperSolver.CreateUI(GrasshopperPanel);
            CommandGrasshopperRecompute.CreateUI(GrasshopperPanel);
            CommandGrasshopperBake.CreateUI(GrasshopperPanel);
            GrasshopperPanel.AddSeparator();
            CommandGrasshopperPlayer.CreateUI(GrasshopperPanel);
          }

          result = data.Application.ActivateRibbonTab(rhinoTab) ? Result.Succeeded : Result.Failed;
          break;
        case Result.Cancelled:
          Button.Enabled = false;

          if (Addin.CurrentStatus == Addin.Status.Unavailable)
            Button.ToolTip = "Rhino.Inside failed to found a valid copy of Rhino 7 WIP installed.";
          else if (Addin.CurrentStatus == Addin.Status.Obsolete)
            Button.ToolTip = "Rhino.Inside has expired.";
          else
            Button.ToolTip = "Rhino.Inside load was cancelled.";

          Button.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, @"https://www.rhino3d.com/inside/revit"));
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

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoInsideOptions : Command
  {
    static PushButton Button;
    static ReleaseInfo LatestReleaseInfo = null;

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      const string CommandName = "Options";

      var buttonData = NewPushButtonData<CommandRhinoInsideOptions, AllwaysAvailable>(CommandName);
      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        // setup button
        Button = pushButton;
        pushButton.Image = ImageBuilder.LoadBitmapImage("Resources.Options.png", true);
        pushButton.LargeImage = ImageBuilder.LoadBitmapImage("Resources.Options.png");

        // disable if startup mode is disabled
        if (Addin.StartupMode == AddinStartupMode.Disabled)
        {
          Button.Enabled = false;
          Button.ToolTip = "Addin Disabled";
        }

        // disable the button if options are readonly
        Button.Enabled = !AddinOptions.IsReadOnly;
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      // try opening options window
      if (!AddinOptions.IsReadOnly)
      {
        var optWindow = new OptionsWindow(data.Application);
        if (LatestReleaseInfo != null)
          optWindow.SetReleaseInfo(LatestReleaseInfo);
        optWindow.Show();
      }
      else
        TaskDialog.Show("Options", "Contact your system admin to change the options");

      return Result.Succeeded;
    }

    /// <summary>
    /// Mark button with highlighter dot using Autodesk.Windows api
    /// </summary>
    static public void NotifyUpdateAvailable(ReleaseInfo releaseInfo)
    {
      // button gets deactivated if options are readonly
      if (!AddinOptions.IsReadOnly)
      {
        Highlight();
        Button.ToolTip = "New Release Available for Download!\n"
                       + $"Version: {releaseInfo.Version}\n"
                       + Button.ToolTip;
        LatestReleaseInfo = releaseInfo;
      }
    }

    static public void Highlight()
    {
      // grab the underlying Autodesk.Windows object from Button
      var getRibbonItemMethodInfo = Button.GetType().GetMethod("getRibbonItem", BindingFlags.NonPublic | BindingFlags.Instance);
      var adWndObj = (Autodesk.Windows.RibbonButton) getRibbonItemMethodInfo.Invoke(Button, null);
      // set highlight state and update tooltip
      adWndObj.Highlight = Autodesk.Internal.Windows.HighlightMode.New;
    }
  }
}
