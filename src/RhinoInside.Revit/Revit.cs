using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using ARAS = Autodesk.Revit.ApplicationServices;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit
{
  /// <summary>
  /// Provides a set of static (Shared in Visual Basic) methods for accessing Revit API from Rhino.Inside.
  /// </summary>
  public static partial class Revit
  {
    internal static ARUI.Result OnStartup()
    {
      if (MainWindow.IsZero)
      {
        var result = Core.CheckSetup();
        if (result != ARUI.Result.Succeeded)
          return result;

        MainWindow = new WindowHandle(Core.Host.MainWindowHandle);

        try   { result = Rhinoceros.Startup(); }
        catch { result = ARUI.Result.Failed; }

        if (result != ARUI.Result.Succeeded)
        {
          MainWindow = WindowHandle.Zero;
          return result;
        }

        // Register some events
        Core.Host.Idling += OnIdle;
        Core.Host.Services.DocumentChanged += OnDocumentChanged;

        Core.CurrentStatus = Core.Status.Ready;
      }

      return ARUI.Result.Succeeded;
    }

    internal static ARUI.Result Shutdown()
    {
      Rhinoceros.Shutdown();

      if (!MainWindow.IsZero)
      {
        // Unregister some events
        Core.Host.Services.DocumentChanged -= OnDocumentChanged;
        Core.Host.Idling -= OnIdle;

        MainWindow.SetHandleAsInvalid();
      }

      return ARUI.Result.Succeeded;
    }

    static bool isRefreshActiveViewPending = false;
    internal static void RefreshActiveView() => isRefreshActiveViewPending = true;

    static void OnIdle(object sender, ARUI.Events.IdlingEventArgs args)
    {
      if (Core.CurrentStatus > Core.Status.Available)
      {
        if (ProcessIdleActions())
          args.SetRaiseWithoutDelay();
      }
    }

    internal static event EventHandler<ARDB.Events.DocumentChangedEventArgs> DocumentChanged;
    private static void OnDocumentChanged(object sender, ARDB.Events.DocumentChangedEventArgs args)
    {
      var document = args.GetDocument();
      if (document.Equals(ActiveDBDocument))
        CancelReadActions();

      DocumentChanged?.Invoke(sender, args);
    }

    #region Idling Actions
    private static readonly Queue<Action> idlingActions = new Queue<Action>();
    internal static void EnqueueIdlingAction(Action action)
    {
      lock (idlingActions)
        idlingActions.Enqueue(action);
    }

    internal static bool ProcessIdleActions()
    {
      bool pendingIdleActions = false;

      // Document dependant tasks need a document
      if (ActiveDBDocument != null)
      {
        // 1. Do all document read actions
        if (ProcessReadActions())
          pendingIdleActions = true;

        // 2. Refresh Active View if necesary
        bool regenComplete = DirectContext3DServer.RegenComplete();
        if (isRefreshActiveViewPending || !regenComplete)
        {
          isRefreshActiveViewPending = false;

          var RefreshTime = new Stopwatch();
          RefreshTime.Start();

          if (DirectContext3DServer.IsAvailable(ActiveUIApplication.ActiveUIDocument.ActiveGraphicalView))
            ActiveUIApplication.ActiveUIDocument.RefreshActiveView();

          RefreshTime.Stop();
          DirectContext3DServer.RegenThreshold = Math.Max(RefreshTime.ElapsedMilliseconds / 3, 100);
        }

        if (!regenComplete)
          pendingIdleActions = true;
      }

      // Non document dependant tasks
      lock (idlingActions)
      {
        while (idlingActions.Count > 0)
        {
          try { idlingActions.Dequeue().Invoke(); }
          catch (Exception e) { Debug.Fail(e.Source, e.Message); }
        }
      }

      return pendingIdleActions;
    }

    static readonly Queue<Action<ARDB.Document, bool>> docReadActions = new Queue<Action<ARDB.Document, bool>>();
    internal static void EnqueueReadAction(Action<ARDB.Document, bool> action)
    {
      lock (docReadActions)
        docReadActions.Enqueue(action);
    }

    static void CancelReadActions() => ProcessReadActions(true);
    static bool ProcessReadActions(bool cancel = false)
    {
      lock (docReadActions)
      {
        if (docReadActions.Count > 0)
        {
          var stopWatch = new Stopwatch();

          while (docReadActions.Count > 0)
          {
            // We will do as much work as possible in 150 ms on each OnIdle event
            if (!cancel && stopWatch.ElapsedMilliseconds > 150)
              return true; // there is pending work to do

            stopWatch.Start();
            try { docReadActions.Dequeue().Invoke(ActiveDBDocument, cancel); }
            catch (Exception e) { Debug.Fail(e.Source, e.Message); }
            stopWatch.Stop();
          }
        }
      }

      // there is no more work to do
      return false;
    }
    #endregion

    #region Public Properties
    internal static WindowHandle MainWindow { get; private set; } = WindowHandle.Zero;

    /// <summary>
    /// Gets the active <see cref="ARUI.UIApplication"/> in the current UI session.
    /// </summary>
    /// <remarks>
    /// Provides access to windows, documents, events used at UI level.
    /// </remarks>
    /// <since>1.0</since>
    public static ARUI.UIApplication ActiveUIApplication => Core.Host.Value as ARUI.UIApplication;

    /// <summary>
    /// Gets the active <see cref="ARAS.Application"/> in the current DB session.
    /// </summary>
    /// <remarks>
    /// Provides access to tolerances, documents, events used at databse level.
    /// </remarks>
    /// <since>1.0</since>
    public static ARAS.Application ActiveDBApplication => ActiveUIApplication?.Application;

    /// <summary>
    /// Gets the active <see cref="ARUI.UIDocument"/> in the Revit UI.
    /// </summary>
    /// <since>1.0</since>
    public static ARUI.UIDocument ActiveUIDocument => ActiveUIApplication?.ActiveUIDocument;

    /// <summary>
    /// Gets the active <see cref="ARDB.Document"/> in the Revit UI.
    /// </summary>
    /// <since>1.0</since>
    public static ARDB.Document ActiveDBDocument => ActiveUIDocument?.Document;

    internal static double ModelUnits => Convert.Geometry.UnitConverter.ToModelLength; // 1 feet in Rhino model units
    #endregion
  }
}
