using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace RhinoInside.Revit.External.UI
{
  #region UIHostApplication
  public abstract class UIHostApplication : IDisposable
  {
    protected UIHostApplication() { }
    public abstract void Dispose();

    public static implicit operator UIHostApplication(UIApplication value) => new UIHostApplicationU(value);
    public static implicit operator UIHostApplication(UIControlledApplication value) => new UIHostApplicationC(value);

    public abstract object Value { get; }

    public abstract DB.HostApplication Host { get; }

    #region UI
    public abstract IntPtr MainWindowHandle { get; }
    #endregion

    #region Ribbon
    public abstract void CreateRibbonTab(string tabName);
    public abstract RibbonPanel CreateRibbonPanel(Tab tab, string panelName);
    public abstract RibbonPanel CreateRibbonPanel(string tabName, string panelName);
    public abstract IReadOnlyList<RibbonPanel> GetRibbonPanels(Tab tab);
    public abstract IReadOnlyList<RibbonPanel> GetRibbonPanels(string tabName);
    #endregion

    #region Addins
    public abstract AddInId ActiveAddInId { get; }
    public abstract void LoadAddIn(string fileName);
    public abstract ExternalApplicationArray LoadedApplications { get; }
    #endregion

    #region Events
    public abstract event EventHandler<IdlingEventArgs> Idling;
    #endregion
  }

  class UIHostApplicationC : UIHostApplication
  {
    readonly UIControlledApplication _app;
    public UIHostApplicationC(UIControlledApplication app) => _app = app;
    public override void Dispose() { }
    public override object Value => _app;

    public override DB.HostApplication Host => new DB.HostApplicationC(_app.ControlledApplication);

    #region UI
    public override IntPtr MainWindowHandle
    {
#if REVIT_2019
      get => _app.MainWindowHandle;
#else
      get => Process.GetCurrentProcess().MainWindowHandle;
#endif
    }
    #endregion

    #region Ribbon
    public override void CreateRibbonTab(string tabName) =>
      _app.CreateRibbonTab(tabName);

    public override RibbonPanel CreateRibbonPanel(Tab tab, string panelName) =>
      _app.CreateRibbonPanel(tab, panelName);

    public override RibbonPanel CreateRibbonPanel(string tabName, string panelName) =>
      _app.CreateRibbonPanel(tabName, panelName);

    public override IReadOnlyList<RibbonPanel> GetRibbonPanels(Tab tab) =>
      _app.GetRibbonPanels(tab);

    public override IReadOnlyList<RibbonPanel> GetRibbonPanels(string tabName) =>
      _app.GetRibbonPanels(tabName);
    #endregion

    #region Addins
    public override AddInId ActiveAddInId => _app.ActiveAddInId;
    public override void LoadAddIn(string fileName) => _app.LoadAddIn(fileName);
    public override ExternalApplicationArray LoadedApplications => _app.LoadedApplications;
    #endregion

    #region Events
    public override event EventHandler<IdlingEventArgs> Idling { add => _app.Idling += value; remove => _app.Idling -= value; }
    #endregion
  }

  class UIHostApplicationU : UIHostApplication
  {
    readonly UIApplication _app;
    public UIHostApplicationU(UIApplication app) => _app = app;
    public override void Dispose() => _app.Dispose();
    public override object Value => _app;

    public override DB.HostApplication Host => new DB.HostApplicationU(_app.Application);

    #region UI
    public override IntPtr MainWindowHandle
    {
#if REVIT_2019
      get => _app.MainWindowHandle;
#else
      get => Process.GetCurrentProcess().MainWindowHandle;
#endif
    }
    #endregion

    #region Ribbon
    public override void CreateRibbonTab(string tabName) =>
      _app.CreateRibbonTab(tabName);

    public override RibbonPanel CreateRibbonPanel(Tab tab, string panelName) =>
      _app.CreateRibbonPanel(tab, panelName);

    public override RibbonPanel CreateRibbonPanel(string tabName, string panelName) =>
      _app.CreateRibbonPanel(tabName, panelName);

    public override IReadOnlyList<RibbonPanel> GetRibbonPanels(Tab tab) =>
      _app.GetRibbonPanels(tab);

    public override IReadOnlyList<RibbonPanel> GetRibbonPanels(string tabName) =>
      _app.GetRibbonPanels(tabName);
    #endregion

    #region Addins
    public override AddInId ActiveAddInId => _app.ActiveAddInId;
    public override void LoadAddIn(string fileName) => _app.LoadAddIn(fileName);
    public override ExternalApplicationArray LoadedApplications => _app.LoadedApplications;
    #endregion

    #region Events
    public override event EventHandler<IdlingEventArgs> Idling { add => _app.Idling += value; remove => _app.Idling -= value; }
    #endregion
  }
  #endregion


  /// <summary>
  /// Base class for an external Revit application
  /// </summary>
  public abstract class ExternalApplication : AddInId, IExternalApplication
  {
    protected ExternalApplication(Guid addInId) : base(addInId) { }

    protected abstract Result OnStartup(UIControlledApplication app);
    Result IExternalApplication.OnStartup(UIControlledApplication app)
    {
      if (app.ActiveAddInId.GetGUID() != GetGUID())
        return Result.Failed;

      return ActivationGate.Open(() => OnStartup(app), this);
    }

    protected abstract Result OnShutdown(UIControlledApplication app);
    Result IExternalApplication.OnShutdown(UIControlledApplication app) =>
      ActivationGate.Open(() => OnShutdown(app), this);

    public virtual bool CatchException(Exception e, UIApplication app, object sender) => false;

    public virtual void ReportException(Exception e, UIApplication app, object sender) { }
  }

}
