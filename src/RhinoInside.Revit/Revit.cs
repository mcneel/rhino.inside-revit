using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Grasshopper;
using Grasshopper.Kernel;
using Microsoft.Win32.SafeHandles;
using Rhino;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;

namespace RhinoInside.Revit
{
  public static partial class Revit
  {
    internal static Result OnStartup(UIControlledApplication applicationUI)
    {
      if (MainWindow.IsZero)
      {
        var result = Addin.CheckSetup(applicationUI);
        if (result != Result.Succeeded)
          return result;

#if REVIT_2019
        MainWindow = new WindowHandle(applicationUI.MainWindowHandle);
#else
        MainWindow = new WindowHandle(Process.GetCurrentProcess().MainWindowHandle);
#endif

        // Save Revit window status
        bool wasEnabled = MainWindow.Enabled;
        var activeWindow = WindowHandle.ActiveWindow ?? MainWindow;

        try
        {
          // Disable Revit window
          MainWindow.Enabled = false;

          result = Rhinoceros.Startup();
        }
        finally
        {
          //Enable Revit window back
          MainWindow.Enabled = wasEnabled;
          WindowHandle.ActiveWindow = activeWindow;
        }

        if (result != Result.Succeeded)
        {
          MainWindow = WindowHandle.Zero;
          return result;
        }

        // Register some events
        applicationUI.Idling += OnIdle;
        applicationUI.ControlledApplication.DocumentChanged += OnDocumentChanged;

        Addin.CurrentStatus = Addin.Status.Ready;
      }

      return Result.Succeeded;
    }

    internal static Result OnShutdown(UIControlledApplication applicationUI)
    {
      if (!MainWindow.IsZero)
      {
        // Unregister some events
        applicationUI.ControlledApplication.DocumentChanged -= OnDocumentChanged;
        applicationUI.Idling -= OnIdle;

        Rhinoceros.Shutdown();

        MainWindow.SetHandleAsInvalid();
      }

      return Result.Succeeded;
    }

    static bool isRefreshActiveViewPending = false;
    public static void RefreshActiveView() => isRefreshActiveViewPending = true;

    static void OnIdle(object sender, IdlingEventArgs args)
    {
      if(ActiveUIApplication?.IsValidObject != true)
        ActiveUIApplication = (sender as UIApplication);

      if (Addin.CurrentStatus > Addin.Status.Available)
      {
        if (ProcessIdleActions())
          args.SetRaiseWithoutDelay();
      }
    }

    public static event EventHandler<DocumentChangedEventArgs> DocumentChanged;
    private static void OnDocumentChanged(object sender, DocumentChangedEventArgs args)
    {
      if (isCommitting)
        return;

      var document = args.GetDocument();
      if (!document.Equals(ActiveDBDocument))
        return;

      CancelReadActions();

      DocumentChanged?.Invoke(sender, args);
    }

    #region Bake Recipe
    public static void BakeGeometry(IEnumerable<Rhino.Geometry.GeometryBase> geometries, BuiltInCategory categoryToBakeInto = BuiltInCategory.OST_GenericModel)
    {
      var doc = ActiveDBDocument;
      if (doc is null)
        throw new ArgumentNullException(nameof(ActiveDBDocument));

      if (categoryToBakeInto == BuiltInCategory.INVALID)
        return;

      foreach (var shapes in geometries.Convert(ShapeEncoder.ToShape))
        BakeGeometry(doc, shapes, categoryToBakeInto);
    }

    [Conditional("DEBUG")]
    static void TraceGeometry(IEnumerable<Rhino.Geometry.GeometryBase> geometries)
    {
      var doc = ActiveDBDocument;
      if (doc is null)
        return;

      using (var ctx = GeometryEncoder.Context.Push(doc))
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          var materials = collector.OfClass(typeof(Material)).Cast<Material>();
          ctx.MaterialId = (materials.Where((x) => x.Name == "Debug").FirstOrDefault()?.Id) ?? ElementId.InvalidElementId;
        }

        foreach (var shape in geometries.Convert(ShapeEncoder.ToShape))
          BakeGeometry(doc, shape, BuiltInCategory.OST_GenericModel);
      }
    }

    static void BakeGeometry(Document doc, IEnumerable<GeometryObject> geometryToBake, BuiltInCategory categoryToBakeInto)
    {
      try
      {
        var geometryList = new List<GeometryObject>();

        // DirectShape only accepts those types and no nulls
        foreach (var g in geometryToBake)
        {
          switch (g)
          {
            case Point p: geometryList.Add(p); break;
            case Curve c: geometryList.Add(c); break;
            case Solid s: geometryList.Add(s); break;
            case Mesh m: geometryList.Add(m); break;
          }
        }

        if (geometryList.Count > 0)
        {
          var category = new ElementId(categoryToBakeInto);
          if (!DirectShape.IsValidCategoryId(category, doc))
            category = new ElementId(BuiltInCategory.OST_GenericModel);

          var ds = DirectShape.CreateElement(doc, category);
          ds.SetShape(geometryList);
        }
      }
      catch (Exception e)
      {
        Debug.Fail(e.Source, e.Message);
      }
    }
    #endregion

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

        // 2. Do all document write actions
        if (!ActiveDBDocument.IsReadOnly)
          ProcessWriteActions();

        // 3. Refresh Active View if necesary
        bool regenComplete = DirectContext3DServer.RegenComplete();
        if (isRefreshActiveViewPending || !regenComplete)
        {
          isRefreshActiveViewPending = false;

          var RefreshTime = new Stopwatch();
          RefreshTime.Start();
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

    static Queue<Action<Document>> docWriteActions = new Queue<Action<Document>>();
    [Obsolete("Since 2020-09-14")]
    public static void EnqueueAction(Action<Document> action)
    {
      lock (docWriteActions)
        docWriteActions.Enqueue(action);
    }

    static bool isCommitting = false;
    static void ProcessWriteActions()
    {
      lock (docWriteActions)
      {
        if (docWriteActions.Count > 0)
        {
          using (var trans = new Transaction(ActiveDBDocument))
          {
            try
            {
              isCommitting = true;

              if (trans.Start("RhinoInside") == TransactionStatus.Started)
              {
                while (docWriteActions.Count > 0)
                  docWriteActions.Dequeue().Invoke(ActiveDBDocument);

                var options = trans.GetFailureHandlingOptions();
#if !DEBUG
                options = options.SetClearAfterRollback(true);
#endif
                options = options.SetDelayedMiniWarnings(true);
                options = options.SetForcedModalHandling(true);
                options = options.SetFailuresPreprocessor(new FailuresPreprocessor());

                // Hide Rhino UI in case any warning-error dialog popups
                {
                  External.EditScope editScope = null;
                  EventHandler<DialogBoxShowingEventArgs> _ = null;
                  try
                  {
                    ApplicationUI.DialogBoxShowing += _ = (sender, args) =>
                    {
                      if (editScope == null)
                        editScope = new External.EditScope();
                    };

                    trans.Commit(options);
                  }
                  finally
                  {
                    ApplicationUI.DialogBoxShowing -= _;

                    if(editScope is IDisposable disposable)
                      disposable.Dispose();
                  }
                }

                if (trans.GetStatus() == TransactionStatus.Committed)
                {
                  foreach (GH_Document definition in Instances.DocumentServer)
                  {
                    if (definition.Enabled)
                      definition.NewSolution(false);
                  }
                }
              }
            }
            catch (Exception e)
            {
              Debug.Fail(e.Source, e.Message);
              docWriteActions.Clear();
            }
            finally
            {
              isCommitting = false;
            }
          }
        }
      }
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
    public static IntPtr MainWindowHandle => MainWindow.Handle;

    public static Screen MainScreen => Screen.FromHandle(MainWindowHandle);
    public static int MainScreenScaleFactor
      => MainScreen != null ?
            System.Convert.ToInt32(Math.Abs(
              MainScreen.WorkingArea.Width / System.Windows.SystemParameters.PrimaryScreenWidth
              )) : 1;

#if REVIT_2019
    public static string CurrentUsersDataFolderPath => ApplicationUI.ControlledApplication.CurrentUsersDataFolderPath;
#else
    public static string CurrentUsersDataFolderPath => System.IO.Path.Combine
    (
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "Autodesk",
      "Revit",
      ApplicationUI.ControlledApplication.VersionName
    );
#endif

    public static Autodesk.Revit.UI.UIControlledApplication       ApplicationUI => Addin.ApplicationUI;
    public static Autodesk.Revit.UI.UIApplication                 ActiveUIApplication { get; internal set; }
    public static Autodesk.Revit.ApplicationServices.Application  ActiveDBApplication => ActiveUIApplication?.Application;

    public static Autodesk.Revit.UI.UIDocument                    ActiveUIDocument => ActiveUIApplication?.ActiveUIDocument;
    public static Autodesk.Revit.DB.Document                      ActiveDBDocument => ActiveUIDocument?.Document;

    private const double AbsoluteTolerance                        = (1.0 / 12.0) / 16.0; // 1/16 inch in feet
    public static double AngleTolerance                           => ActiveDBApplication?.AngleTolerance       ?? Math.PI / 180.0; // in rad
    public static double ShortCurveTolerance                      => ActiveDBApplication?.ShortCurveTolerance  ?? AbsoluteTolerance / 2.0;
    public static double VertexTolerance                          => ActiveDBApplication?.VertexTolerance      ?? AbsoluteTolerance / 10.0;
    public const Rhino.UnitSystem ModelUnitSystem                 = Rhino.UnitSystem.Feet; // Always feet
    public static double ModelUnits => RhinoDoc.ActiveDoc is null ? double.NaN : RhinoMath.UnitScale(ModelUnitSystem, RhinoDoc.ActiveDoc.ModelUnitSystem); // 1 feet in Rhino units
    #endregion
  }
}
