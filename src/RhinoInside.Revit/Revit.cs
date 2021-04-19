using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Microsoft.Win32.SafeHandles;

using Rhino;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.ApplicationServices.Extensions;
using RhinoInside.Revit.External.UI;
using RhinoInside.Revit.External.UI.Extensions;

namespace RhinoInside.Revit
{
  public static partial class Revit
  {
    internal static Result OnStartup(UIHostApplication applicationUI)
    {
      if (MainWindow.IsZero)
      {
        var result = AddIn.CheckSetup();
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
        applicationUI.Services.DocumentChanged += OnDocumentChanged;

        AddIn.CurrentStatus = AddIn.Status.Ready;
      }

      return Result.Succeeded;
    }

    internal static Result OnShutdown(UIHostApplication applicationUI)
    {
      if (!MainWindow.IsZero)
      {
        // Unregister some events
        applicationUI.Services.DocumentChanged -= OnDocumentChanged;
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
      if (AddIn.CurrentStatus > AddIn.Status.Available)
      {
        if (ProcessIdleActions())
          args.SetRaiseWithoutDelay();
      }
    }

    public static event EventHandler<DocumentChangedEventArgs> DocumentChanged;
    private static void OnDocumentChanged(object sender, DocumentChangedEventArgs args)
    {
      var document = args.GetDocument();
      if (!document.Equals(ActiveDBDocument))
        return;

      CancelReadActions();

      DocumentChanged?.Invoke(sender, args);
    }

    #region Bake Recipe
    [Obsolete("Since 2021-03-19")]
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

        // 2. Refresh Active View if necesary
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

    internal static Screen MainScreen => ActiveUIApplication?.GetRevitScreen() ?? Screen.FromHandle(MainWindowHandle);

    [Obsolete("Since 2021-03-19")]
    public static string CurrentUsersDataFolderPath => ApplicationUI.ControlledApplication.GetCurrentUsersDataFolderPath();

    [Obsolete("Since 2021-03-27")]
    public static Autodesk.Revit.UI.UIControlledApplication       ApplicationUI { get; internal set; }

    public static Autodesk.Revit.UI.UIApplication                 ActiveUIApplication => AddIn.Host.Value as Autodesk.Revit.UI.UIApplication;
    public static Autodesk.Revit.ApplicationServices.Application  ActiveDBApplication => ActiveUIApplication?.Application;

    public static Autodesk.Revit.UI.UIDocument                    ActiveUIDocument => ActiveUIApplication?.ActiveUIDocument;
    public static Autodesk.Revit.DB.Document                      ActiveDBDocument => ActiveUIDocument?.Document;

    private const double AbsoluteTolerance                        = (1.0 / 12.0) / 16.0; // 1/16″ in feet
    public static double AngleTolerance                           => ActiveDBApplication?.AngleTolerance       ?? Math.PI / 1800.0; // 0.1° in rad
    public static double ShortCurveTolerance                      => ActiveDBApplication?.ShortCurveTolerance  ?? AbsoluteTolerance / 2.0;
    public static double VertexTolerance                          => ActiveDBApplication?.VertexTolerance      ?? AbsoluteTolerance / 10.0;

    [Obsolete("Since 2021-04-16")]
    public const Rhino.UnitSystem ModelUnitSystem                 = Rhino.UnitSystem.Feet; // Always feet
    public static double ModelUnits                               => UnitConverter.HostModelUnits; // 1 feet in Rhino units
    #endregion
  }
}
