using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using RhinoInside.Revit.External.ApplicationServices.Extensions;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.External.UI.Extensions;

namespace RhinoInside.Revit.External.UI
{
  sealed class UIHostApplicationUnconstrained : UIHostApplication
  {
    UIApplication _app;
    public UIHostApplicationUnconstrained(UIApplication app, bool disposable) : base(disposable)
    {
      _app = app;
      _app.ViewActivated += UpdateOpenViewsList;

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
          _app.ViewActivated -= UpdateOpenViewsList;
          _app.Dispose();
        }

        _app = null;
      }
    }
    
    public override object Value => _app.IsValidObject ? _app : default;
    public override bool IsValid => _app.IsValidObject;

    public override ApplicationServices.HostServices Services => ApplicationServices.HostServices.Current ??= _app.Application;
    public override UIDocument ActiveUIDocument
    {
      get => _app.ActiveUIDocument;
      set
      {
        if (value is null) throw new ArgumentNullException();
        if (value.Document.IsEquivalent(_app.ActiveUIDocument.Document)) return;

        if (value.TryGetActiveGraphicalView(out var uiView))
        {
          HostedApplication.Active.InvokeInHostContext
          (() => value.Document.SetActiveGraphicalView(value.Document.GetElement(uiView.ViewId) as View, out var _));
        }
      }
    }

    #region UI
    public override IntPtr MainWindowHandle
    {
#if REVIT_2019
      get => _app.MainWindowHandle;
#else
      get => System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
#endif
    }

    internal override bool IsViewOpen(View view) => OpenViews.Contains(view);

    private ISet<View> OpenViews = new HashSet<View>(ElementEqualityComparer.InterDocument);
    private void UpdateOpenViewsList(object sender, ViewActivatedEventArgs e)
    {
      OpenViews = new HashSet<View>(_app.Application.GetOpenViews(), ElementEqualityComparer.InterDocument);
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

    #region AddIns
    public override AddInId ActiveAddInId => _app.ActiveAddInId;
    public override void LoadAddIn(string fileName) => _app.LoadAddIn(fileName);
    public override ExternalApplicationArray LoadedApplications => _app.LoadedApplications;
    #endregion

    #region Commands
    public override bool CanPostCommand(RevitCommandId commandId) => _app.CanPostCommand(commandId);
    public override void PostCommand(RevitCommandId commandId) => _app.PostCommand(commandId);
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
