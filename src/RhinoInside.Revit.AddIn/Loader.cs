using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.UI;
using RhinoInside.Revit.Diagnostics;
using UIX = RhinoInside.Revit.External.UI;

namespace RhinoInside.Revit.AddIn
{
  public class Loader : UIX.ExternalApplication, UIX.IHostedApplication
  {
    public Loader() : base(new Guid("02EFF7F0-4921-4FD3-91F6-A87B6BA9BF74")) => Instance = this;

    ~Loader() => Instance = default;

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

        var result = Core.OnStartup(app);
        if (result != Result.Succeeded) return result;
      }

      // Initialize UI
      {
        Commands.CommandStart.CreateUI(app);

        StartupOnApplicationInitialized();
      }

      // Check For Updates
      {
        Properties.AddInOptions.UpdateChannelChanged += (sender, args) => CheckForUpdates();
        if (Properties.AddInOptions.Current.CheckForUpdatesOnStartup) CheckForUpdates();
      }

      return Result.Succeeded;
    }

    protected override Result OnShutdown(UIControlledApplication app) => Core.OnShutdown(app);

    public override bool CatchException(Exception e, UIApplication app, object sender)
    {
      // There is a wild pointer somewhere, is better to close Revit.
      bool fatal = e is AccessViolationException;

      if (fatal)
        Core.CurrentStatus = Core.Status.Crashed;

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

      var attachments = e.Data["Attachments"] as IEnumerable<string> ?? Enumerable.Empty<string>();
      e.Data["Attachments"] = attachments.Append(RhinoInside_dmp).ToArray();

      Core.ReportException(e, app);
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
            Core.Host.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.ExitRevit));
        }
      };
    }

    static async void CheckForUpdates()
    {
      var releaseInfo = await Distribution.Updater.GetReleaseInfoAsync();

      // if release info is received, and
      // if current version on the active update channel is newer
      if (releaseInfo is Distribution.ReleaseInfo && releaseInfo.Version > Core.Version)
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
    void UIX.IHostedApplication.InvokeInHostContext(Action action) => Rhinoceros.InvokeInHostContext(action);
    T UIX.IHostedApplication.InvokeInHostContext<T>(Func<T> func) => Rhinoceros.InvokeInHostContext(func);

    bool UIX.IHostedApplication.DoEvents() => Rhinoceros.Run();
    Microsoft.Win32.SafeHandles.WindowHandle UIX.IHostedApplication.MainWindow => Rhinoceros.MainWindow;
    #endregion
  }
}
