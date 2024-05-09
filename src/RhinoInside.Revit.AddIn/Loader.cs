using System;
using Autodesk.Revit.UI;
using static RhinoInside.Revit.Diagnostics;
using ERUI = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.AddIn
{
  public sealed class Loader : ERUI.ExternalApplication, ERUI.IHostedApplication
  {
    public Loader() : base(new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74")) => Instance = this;

    protected override void Dispose(bool disposing)
    {
      if (!disposing) Instance = null;

      base.Dispose(disposing);
    }

    public static Loader Instance { get; private set; }

    #region ExternalApplication
    protected override Result OnStartup(UIControlledApplication app)
    {
      // Initialize Core
      {
        var options = Properties.AddInOptions.Session;
        Core.IsolateSettings = options.IsolateSettings;
        Core.UseHostLanguage = options.UseHostLanguage;
        Core.KeepUIOnTop     = options.KeepUIOnTop;
        Core.ActivationEvent = External.ActivationGate.CreateActivationEvent();

        var result = Core.OnStartup(app);
        if (result != Result.Succeeded) return result;
      }

      // Initialize UI
      {
        StartupOnApplicationInitialized();

        Commands.CommandStart.CreateUI(app);
      }

      // Check For Updates
      {
        Properties.AddInOptions.UpdateChannelChanged += (sender, args) => CheckForUpdates();
        if (Properties.AddInOptions.Current.CheckForUpdatesOnStartup) CheckForUpdates();
      }

      return Result.Succeeded;
    }

    protected override Result OnShutdown(UIControlledApplication app)
    {
      Properties.AddInOptions.Save();

      return Core.OnShutdown(app);
    }

    public override bool CatchException(Exception e, UIApplication app, object sender)
    {
      // There is a wild pointer somewhere, is better to close Revit.
      bool fatal = e is AccessViolationException;

      if (fatal)
        Core.CurrentStatus = Core.Status.Crashed;

      ErrorReport.DumpException(e, app);
      return true;
    }

    public override void ReportException(Exception e, UIApplication app, object sender)
    {
      // A serious error has occurred. The current action has ben cancelled.
      // It is stringly recommended that you save your work in a new file before continuing.
      //
      // Would you like to save a recovery file? "{TileName}(Recovery)".rvt

      ErrorReport.ReportException(e, app);
    }

    static void StartupOnApplicationInitialized()
    {
      if (Core.StartupMode < CoreStartupMode.OnStartup && !Properties.AddInOptions.Session.LoadOnStartup)
        return;

      EventHandler<Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs> applicationInitialized = null;
      Core.Host.Services.ApplicationInitialized += applicationInitialized = (sender, args) =>
      {
        Core.Host.Services.ApplicationInitialized -= applicationInitialized;

        if (Core.CurrentStatus < Core.Status.Available) return;
        if (Commands.CommandStart.Start() == Result.Succeeded)
        {
          if (Core.StartupMode == CoreStartupMode.Scripting)
          {
            if (ERUI.Extensions.UIApplicationExtension.LookupPostableCommandId(null, PostableCommand.ExitRevit) is RevitCommandId cmdExitRevit)
              Core.Host.PostCommand(cmdExitRevit);
            else
              Revit.MainWindow.TryClose();
          }
        }
      };
    }

    static async void CheckForUpdates()
    {
      var releaseInfo = await Deployment.Updater.GetReleaseInfoAsync();

      // if release info is received, and
      // if current version on the active update channel is newer
      if (releaseInfo is Deployment.ReleaseInfo && releaseInfo.Version > Core.Version)
      {
        // ask UI to notify user of updates
        if (!Properties.AddInOptions.Session.CompactTab)
          Commands.CommandStart.NotifyUpdateAvailable(releaseInfo);

        Commands.CommandAddInOptions.NotifyUpdateAvailable(releaseInfo);
      }
      else
      {
        // otherwise clear updates
        Commands.CommandStart.ClearUpdateNotifiy();
        Commands.CommandAddInOptions.ClearUpdateNotifiy();
      }
    }
    #endregion

    #region IHostedApplication
    void ERUI.IHostedApplication.InvokeInHostContext(Action action)
    {
      if (Core.CurrentStatus == Core.Status.Ready) Rhinoceros.InvokeInHostContext(action);
      else action();
    }
    T ERUI.IHostedApplication.InvokeInHostContext<T>(Func<T> func)
    {
      if (Core.CurrentStatus == Core.Status.Ready) return Rhinoceros.InvokeInHostContext(func);
      else return func();
    }

    bool ERUI.IHostedApplication.DoEvents() => Rhinoceros.Run();
    Microsoft.Win32.SafeHandles.WindowHandle ERUI.IHostedApplication.MainWindow => Rhinoceros.MainWindow;
    #endregion
  }
}
