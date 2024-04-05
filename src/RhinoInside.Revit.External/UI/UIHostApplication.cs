using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace RhinoInside.Revit.External.UI
{
  using Autodesk.Revit.ApplicationServices;
  using External.DB.Extensions;

  public abstract class UIHostApplication : IDisposable
  {
    protected internal UIHostApplication(bool disposable) => Disposable = disposable;

    #region IDisposable
#pragma warning disable CA1063 // Implement IDisposable Correctly
    readonly bool Disposable;
    protected abstract void Dispose(bool disposing);
    void IDisposable.Dispose()
    {
      if (!Disposable) return;

      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
#pragma warning restore CA1063 // Implement IDisposable Correctly
    #endregion

    public static implicit operator UIHostApplication(UIApplication value) => new UIHostApplicationUnconstrained(value, disposable: true);
    public static implicit operator UIHostApplication(UIControlledApplication value) => new UIHostApplicationConstrained(value, disposable: true);

    public abstract object Value { get; }
    public abstract bool IsValid { get; }

    public abstract ApplicationServices.HostServices Services { get; }
    public abstract UIDocument ActiveUIDocument { get; set; }

    #region Runtime
    internal static UIHostApplication Current;

    internal static bool StartUp(UIControlledApplication app)
    {
      if (!ApplicationServices.HostServices.StartUp(app.ControlledApplication))
        return false;

      Current = new UIHostApplicationConstrained(app, disposable: false);
      ApplicationServices.HostServices.Current.ApplicationInitialized += Initialized;

      ActivationGate.SetHostWindow(Current.MainWindowHandle);
      return true;
    }

    private static void Initialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
    {
      ApplicationServices.HostServices.Current.ApplicationInitialized -= Initialized;
      Current = new UIHostApplicationUnconstrained(new UIApplication(sender as Application), disposable: false);

      // From now on DB is available
      //
    }

    internal static bool Shutdown(UIControlledApplication app)
    {
      ActivationGate.SetHostWindow(IntPtr.Zero);

      Current?.Dispose(true);
      Current = null;

      return ApplicationServices.HostServices.Shutdown(app.ControlledApplication);
    }
    #endregion

    #region UI
    public abstract IntPtr MainWindowHandle { get; }

    internal abstract bool IsViewOpen(View view);
    #endregion

    #region Ribbon
    public abstract void CreateRibbonTab(string tabName);
    internal bool ActivateRibbonTab(string tabName)
    {
      foreach (var tab in Autodesk.Windows.ComponentManager.Ribbon.Tabs)
      {
        if (tab.Name == tabName)
        {
          tab.IsActive = true;
          return true;
        }
      }

      return false;
    }

    public abstract RibbonPanel CreateRibbonPanel(Tab tab, string panelName);
    public abstract RibbonPanel CreateRibbonPanel(string tabName, string panelName);
    public abstract IReadOnlyList<RibbonPanel> GetRibbonPanels(Tab tab);
    public abstract IReadOnlyList<RibbonPanel> GetRibbonPanels(string tabName);
    #endregion

    #region AddIns
    public abstract AddInId ActiveAddInId { get; }
    public abstract void LoadAddIn(string fileName);
    public abstract ExternalApplicationArray LoadedApplications { get; }
    #endregion

    #region Commands
    public abstract bool CanPostCommand(RevitCommandId commandId);
    public abstract void PostCommand(RevitCommandId commandId);
    #endregion

    #region Events
    public abstract event EventHandler<IdlingEventArgs> Idling;
    public abstract event EventHandler<ViewActivatingEventArgs> ViewActivating;
    public abstract event EventHandler<ViewActivatedEventArgs> ViewActivated;
    #endregion

    #region SelectionChanged
#if REVIT_2023
    protected void SelectionChangedHandler(object sender, SelectionChangedEventArgs e) => SelectionChanged?.Invoke(sender, e);
    public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
#else
    static readonly object selectionChangedLock = new object();
    static event EventHandler<SelectionChangedEventArgs> SelectionChangedHandler;
    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
      add
      {
        lock (selectionChangedLock)
        {
          if (SelectionChangedHandler is null)
          {
            Idling += CompareSelection;
            Services.DocumentClosing += Services_DocumentClosing;
          }

          SelectionChangedHandler += value;
        }
      }

      remove
      {
        lock (selectionChangedLock)
        {
          SelectionChangedHandler -= value;

          if (SelectionChangedHandler is null)
          {
            Services.DocumentClosing -= Services_DocumentClosing;
            Idling -= CompareSelection;
          }
        }
      }
    }

    static readonly Dictionary<Document, ISet<ElementId>> previousSelections = new Dictionary<Document, ISet<ElementId>>();
    private void Services_DocumentClosing(object sender, Autodesk.Revit.DB.Events.DocumentClosingEventArgs e)
    {
      previousSelections.Remove(e.Document);
    }

    private void CompareSelection(object sender, IdlingEventArgs e)
    {
      if (SelectionChangedHandler is null)
        return;

      if (sender is UIApplication uiApplication)
      {
        if (uiApplication.ActiveUIDocument is UIDocument uiDocument)
        {
          if (!previousSelections.TryGetValue(uiDocument.Document, out var previousSelection))
            previousSelection = ElementIdExtension.EmptySet;

          var currentSelection = uiDocument.Selection.GetElementIds().AsReadOnlyElementIdSet();
          if(!previousSelection.SetEquals(currentSelection))
          {
            if (currentSelection.Count > 0)
              previousSelections[uiDocument.Document] = currentSelection;
            else
              previousSelections.Remove(uiDocument.Document);

            using (var args = new SelectionChangedEventArgs(uiDocument.Document, currentSelection))
              SelectionChangedHandler(sender, args);
          }
        }
      }
    }
#endif
    #endregion
  }
}

namespace System.Windows.Interop
{
  static class UIHostApplicationInterop
  {
    static HwndTarget MainWindowTarget;
    public static Window GetMainWindow(this RhinoInside.Revit.External.UI.UIHostApplication app)
    {
      if (app.MainWindowHandle != IntPtr.Zero)
      {
        if (HwndSource.FromHwnd(app.MainWindowHandle)?.RootVisual is Window window)
          return window;

        var target = MainWindowTarget ?? (MainWindowTarget = new HwndTarget(app.MainWindowHandle));
        try { return target.RootVisual as Window; }
        catch { }
      }

      return default;
    }
  }
}

namespace System.Windows.Forms.Interop
{
  static class UIHostApplicationInterop
  {
    public static IWin32Window GetMainWindow(this RhinoInside.Revit.External.UI.UIHostApplication app)
    {
      return app.MainWindowHandle != IntPtr.Zero ?
        NativeWindow.FromHandle(app.MainWindowHandle) :
        default;
    }
  }
}

//namespace Eto.Forms.Interop
//{
//  static class UIHostApplicationInterop
//  {
//    public static Window GetMainWindow(this RhinoInside.Revit.External.UI.UIHostApplication app)
//    {
//      return app.MainWindowHandle != IntPtr.Zero ?
//        new Form(new Wpf.Forms.HwndFormHandler(app.MainWindowHandle)) :
//        default;
//    }
//  }
//}
