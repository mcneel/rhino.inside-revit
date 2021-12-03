using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;

namespace RhinoInside.Revit.External.UI
{
  /// <summary>
  /// Base class for an external Revit application
  /// </summary>
  public abstract class ExternalApplication : AddInId, IExternalApplication
  {
    protected ExternalApplication(Guid addInId) : base(addInId) { }

    static UIControlledApplication uiControlledApplication;
    static readonly Dictionary<Guid, ExternalApplication> Instances = new Dictionary<Guid, ExternalApplication>();
    public static ExternalApplication ActiveApplication
    {
      get
      {
        if (uiControlledApplication is null) return default;

        var activeAddInId = uiControlledApplication.ActiveAddInId?.GetGUID() ?? Guid.Empty;
        if (activeAddInId == Guid.Empty) return default;

        Instances.TryGetValue(activeAddInId, out var addIn);
        return addIn;
      }
    }
    internal static WindowHandle HostMainWindow { get; private set; } = WindowHandle.Zero;

    protected abstract Result OnStartup(UIControlledApplication app);
    Result IExternalApplication.OnStartup(UIControlledApplication app)
    {
      var addInId = GetGUID();
      if (app.ActiveAddInId.GetGUID() != addInId)
        return Result.Failed;

      if (Instances.ContainsKey(addInId))
        return Result.Failed;

      if (Instances.Count == 0)
      {
        uiControlledApplication = app;
#if REVIT_2019
        HostMainWindow = new WindowHandle(app.MainWindowHandle);
#else
        HostMainWindow = new WindowHandle(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
#endif
      }

      Instances.Add(addInId, this);

      var result = Result.Failed;
      try
      {
        return result = ActivationGate.Open(() => OnStartup(app), this);
      }
      finally
      {
        if (result != Result.Succeeded)
        {
          Instances.Remove(addInId);

          if (Instances.Count == 0)
          {
            uiControlledApplication = default;
            HostMainWindow = WindowHandle.Zero;
          }
        }
      }
    }

    protected abstract Result OnShutdown(UIControlledApplication app);
    Result IExternalApplication.OnShutdown(UIControlledApplication app)
    {
      var addInId = GetGUID();
      if (app.ActiveAddInId.GetGUID() != addInId)
        return Result.Failed;

      if (!Instances.ContainsKey(addInId))
        return Result.Failed;

      try
      {
        return ActivationGate.Open(() => OnShutdown(app), this);
      }
      finally
      {
        Instances.Remove(addInId);

        if (Instances.Count == 0)
          uiControlledApplication = default;
      }
    }

    public virtual bool CatchException(Exception e, UIApplication app, object sender) => false;

    public virtual void ReportException(Exception e, UIApplication app, object sender) { }

    internal static bool InvokeCatchException(UIApplication app, Exception e, object sender)
    {
      return ActiveApplication?.CatchException(e, app, sender) ?? false;
    }

    internal static void InvokeReportException(UIApplication app, Exception e, object sender)
    {
      var comment = $@"Managed exception caught from external API application '{e.Source}' in method '{e.TargetSite}' Exception type: '<{e.GetType().FullName}>,' Exception method: '<{e.Message}>,' Stack trace '   {e.StackTrace}";
      comment = comment.Replace(Environment.NewLine, $"{Environment.NewLine}'");
      app.Application.WriteJournalComment(comment, true);

      foreach (var hWnd in ActivationGate.GateWindows)
      {
        using (var window = new WindowHandle(hWnd))
        {
          window.HideOwnedPopups();
          window.Hide();
        }
      }

      ActiveApplication?.ReportException(e, app, sender);
    }
  }

  internal interface IHostedApplication
  {
    void InvokeInHostContext(Action action);
    T InvokeInHostContext<T>(Func<T> func);

    WindowHandle MainWindow { get; }

    bool DoEvents();
  }

  internal struct HostedApplication : IHostedApplication
  {
    public static IHostedApplication Active =>
      ExternalApplication.ActiveApplication as IHostedApplication ??
      default(HostedApplication);

    public void InvokeInHostContext(Action action) => action();

    public T InvokeInHostContext<T>(Func<T> func) => func();

    public WindowHandle MainWindow => WindowHandle.Zero;

    public bool DoEvents() => false;
  }
}
