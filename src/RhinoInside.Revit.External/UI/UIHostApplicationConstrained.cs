using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace RhinoInside.Revit.External.UI
{
  sealed class UIHostApplicationConstrained : UIHostApplication
  {
    UIControlledApplication _app;
    public UIHostApplicationConstrained(UIControlledApplication app, bool disposable) : base(disposable)
    {
      _app = app;

#if REVIT_2023
      _app.SelectionChanged += SelectionChangedHandler;
#endif
    }
    protected override void Dispose(bool disposing)
    {
      if (_app is object)
      {
        if (disposing)
        {
  #if REVIT_2023
          _app.SelectionChanged -= SelectionChangedHandler;
#endif
          //_app.Dispose();
        }

        _app = null;
      }
    }

    public override object Value => _app;
    public override bool IsValid => true;

    public override ApplicationServices.HostServices Services => ApplicationServices.HostServices.Current ??= _app.ControlledApplication;
    public override UIDocument ActiveUIDocument { get => default; set => throw new InvalidOperationException(); }

    #region UI
    public override IntPtr MainWindowHandle
    {
#if REVIT_2019
      get => _app.MainWindowHandle;
#else
      get => System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
#endif
    }

    internal override bool IsViewOpen(View view) => false;
    #endregion

    #region Ribbon
    public override void CreateRibbonTab(string tabName) =>
      _app.CreateRibbonTab(tabName);

    public override RibbonPanel CreateRibbonPanel(Tab tab, string panelName) =>
      _app.CreateRibbonPanel(tab, panelName);

    public override RibbonPanel CreateRibbonPanel(string tabName, string panelName) =>
      _app.CreateRibbonPanel(tabName, panelName);

    public override IReadOnlyDictionary<string, RibbonPanel> GetRibbonPanels(Tab tab) =>
      _app.GetRibbonPanels(tab).ToDictionary(x => x.Name);

    public override IReadOnlyDictionary<string, RibbonPanel> GetRibbonPanels(string tabName) =>
      _app.GetRibbonPanels(tabName).ToDictionary(x => x.Name);
    #endregion

    #region Addins
    public override AddInId ActiveAddInId => _app.ActiveAddInId;
    public override void LoadAddIn(string fileName) => _app.LoadAddIn(fileName);
    public override ExternalApplicationArray LoadedApplications => _app.LoadedApplications;
    #endregion

    #region Commands
    public override bool CanPostCommand(RevitCommandId commandId) => false;
    public override void PostCommand(RevitCommandId commandId) => throw new InvalidOperationException();
    #endregion

    #region Events
    public override event EventHandler<IdlingEventArgs> Idling
    {
      add    => _app.Idling += ActivationGate.AddEventHandler(value);
      remove => _app.Idling -= ActivationGate.RemoveEventHandler(value);
    }
    public override event EventHandler<ViewActivatingEventArgs> ViewActivating { add => _app.ViewActivating += value; remove => _app.ViewActivating -= value; }
    public override event EventHandler<ViewActivatedEventArgs> ViewActivated { add => _app.ViewActivated += value; remove => _app.ViewActivated -= value; }
    #endregion
  }
}
