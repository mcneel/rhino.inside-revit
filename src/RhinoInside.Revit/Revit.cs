using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Microsoft.Win32.SafeHandles;
using RhinoInside.Revit.Convert.Geometry;

namespace RhinoInside.Revit
{
  public static partial class Revit
  {
    internal static Result OnStartup()
    {
      if (MainWindow.IsZero)
      {
        var result = Core.CheckSetup();
        if (result != Result.Succeeded)
          return result;

        MainWindow = new WindowHandle(Core.Host.MainWindowHandle);

        try   { result = Rhinoceros.Startup(); }
        catch { result = Result.Failed; }

        if (result != Result.Succeeded)
        {
          MainWindow = WindowHandle.Zero;
          return result;
        }

        // Register some events
        Core.Host.Idling += OnIdle;
        Core.Host.Services.DocumentChanged += OnDocumentChanged;

        Core.CurrentStatus = Core.Status.Ready;
      }

      return Result.Succeeded;
    }

    internal static Result Shutdown()
    {
      Rhinoceros.Shutdown();

      if (!MainWindow.IsZero)
      {
        // Unregister some events
        Core.Host.Services.DocumentChanged -= OnDocumentChanged;
        Core.Host.Idling -= OnIdle;

        MainWindow.SetHandleAsInvalid();
      }

      return Result.Succeeded;
    }

    static bool isRefreshActiveViewPending = false;
    internal static void RefreshActiveView() => isRefreshActiveViewPending = true;

    static void OnIdle(object sender, IdlingEventArgs args)
    {
      if (Core.CurrentStatus > Core.Status.Available)
      {
        if (ProcessIdleActions())
          args.SetRaiseWithoutDelay();
      }
    }

    internal static event EventHandler<DocumentChangedEventArgs> DocumentChanged;
    private static void OnDocumentChanged(object sender, DocumentChangedEventArgs args)
    {
      var document = args.GetDocument();
      if (document.Equals(ActiveDBDocument))
        CancelReadActions();

      DocumentChanged?.Invoke(sender, args);
    }

    #region Idling Actions
    private static Queue<Action> idlingActions = new Queue<Action>();
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

    static Queue<Action<Document, bool>> docReadActions = new Queue<Action<Document, bool>>();
    internal static void EnqueueReadAction(Action<Document, bool> action)
    {
      lock (docReadActions)
        docReadActions.Enqueue(action);
    }

    internal static void CancelReadActions() => ProcessReadActions(true);
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

    public static Autodesk.Revit.UI.UIApplication                 ActiveUIApplication => Core.Host.Value as Autodesk.Revit.UI.UIApplication;
    public static Autodesk.Revit.ApplicationServices.Application  ActiveDBApplication => ActiveUIApplication?.Application;

    public static Autodesk.Revit.UI.UIDocument                    ActiveUIDocument => ActiveUIApplication?.ActiveUIDocument;
    public static Autodesk.Revit.DB.Document                      ActiveDBDocument => ActiveUIDocument?.Document;

    private const double AbsoluteTolerance                        = (1.0 / 12.0) / 16.0; // 1/16″ in feet
    public static double AngleTolerance                           => ActiveDBApplication?.AngleTolerance       ?? Math.PI / 1800.0; // 0.1° in rad
    public static double ShortCurveTolerance                      => ActiveDBApplication?.ShortCurveTolerance  ?? AbsoluteTolerance / 2.0;
    public static double VertexTolerance                          => ActiveDBApplication?.VertexTolerance      ?? AbsoluteTolerance / 10.0;

    public static double ModelUnits                               => UnitConverter.ToRhinoUnits; // 1 feet in Rhino units
    #endregion
  }
}
