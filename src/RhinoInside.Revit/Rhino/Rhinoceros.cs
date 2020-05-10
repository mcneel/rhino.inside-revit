using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Win32.SafeHandles;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.PlugIns;
using Rhino.Runtime.InProcess;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.External.ApplicationServices.Extensions;
using static System.Math;
using static Rhino.RhinoMath;
using DB = Autodesk.Revit.DB;

namespace RhinoInside
{
  #region Guest
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
  public class GuestPlugInIdAttribute : Attribute
  {
    public readonly Guid PlugInId;
    public GuestPlugInIdAttribute(string plugInId) => PlugInId = Guid.Parse(plugInId);
  }

  public interface IGuest
  {
    string Name { get; }

    LoadReturnCode OnCheckIn(ref string complainMessage);
    void OnCheckOut();
  }
  #endregion
}

namespace RhinoInside.Revit
{
  using Result = Autodesk.Revit.UI.Result;
  using TaskDialog = Autodesk.Revit.UI.TaskDialog;

  public static class Rhinoceros
  {
    #region Revit Interface
    static RhinoCore core;
    public static readonly string SchemeName = $"Inside-Revit-{Revit.ApplicationUI.ControlledApplication.VersionNumber}";
    internal static string[] StartupLog;

    internal static Result Startup()
    {
      if (core is null)
      {
        // Load RhinoCore
        try
        {
          core = new RhinoCore
          (
            new string[]
            {
              "/nosplash",
              "/notemplate",
              "/captureprintcalls",
              "/stopwatch",
              $"/scheme={SchemeName}",
              $"/language={Revit.ApplicationUI.ControlledApplication.Language.ToLCID()}"
            },
            WindowStyle.Hidden
          );
        }
        catch
        {
          Addin.CurrentStatus = Addin.Status.Failed;
          return Result.Failed;
        }
        finally
        {
          StartupLog = RhinoApp.CapturedCommandWindowStrings(true);
          RhinoApp.CommandWindowCaptureEnabled = false;
        }

        MainWindow = new WindowHandle(RhinoApp.MainWindowHandle());
        External.ActivationGate.AddGateWindow(MainWindow.Handle);

        RhinoApp.MainLoop                         += MainLoop;

        RhinoDoc.NewDocument                      += OnNewDocument;
        RhinoDoc.EndOpenDocumentInitialViewUpdate += EndOpenDocumentInitialViewUpdate;

        Command.BeginCommand                      += BeginCommand;
        Command.EndCommand                        += EndCommand;

        // Alternative to /runscript= Rhino command line option
        RunScriptAsync
        (
          script:   Environment.GetEnvironmentVariable("RhinoInside_RunScript"),
          activate: Addin.StartupMode == AddinStartupMode.AtStartup
        );

        // Reset document units
        UpdateDocumentUnits(RhinoDoc.ActiveDoc);
        UpdateDocumentUnits(RhinoDoc.ActiveDoc, Revit.ActiveDBDocument);

        Type[] types = default;
        try { types = Assembly.GetCallingAssembly().GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types?.Where(x => x is object).ToArray(); }

        // Look for Guests
        guests = types.
          Where(x => typeof(IGuest).IsAssignableFrom(x)).
          Where(x => !x.IsInterface).
          Select(x => new GuestInfo(x)).
          ToList();

        CheckInGuests();

        //Enable Revit window back
        WindowHandle.ActiveWindow = Revit.MainWindow;
      }

      return Result.Succeeded;
    }

    internal static Result Shutdown()
    {
      if (core is object)
      {
        // Unload Guests
        CheckOutGuests();

        Command.EndCommand                        -= EndCommand;
        Command.BeginCommand                      -= BeginCommand;

        RhinoDoc.EndOpenDocumentInitialViewUpdate -= EndOpenDocumentInitialViewUpdate;
        RhinoDoc.NewDocument                      -= OnNewDocument;

        RhinoApp.MainLoop                         -= MainLoop;

        // Unload RhinoCore
        try
        {
          core.Dispose();
          core = null;
        }
        catch (Exception /*e*/)
        {
          //Debug.Fail(e.Source, e.Message);
          return Result.Failed;
        }
      }

      return Result.Succeeded;
    }

    internal static WindowHandle MainWindow = WindowHandle.Zero;
    public static IntPtr MainWindowHandle => MainWindow.Handle;

    static bool idlePending = true;
    internal static void RaiseIdle() => core.RaiseIdle();

    internal static bool Run()
    {
      if (idlePending)
      {
        Revit.ActiveDBApplication?.PurgeReleasedAPIObjects();
        idlePending = core.DoIdle();
      }

      var active = core.DoEvents();
      if (active)
        idlePending = true;

      if (Revit.ProcessIdleActions())
        core.RaiseIdle();

      return active;
    }
    #endregion

    #region Guests
    class GuestInfo
    {
      public readonly Type ClassType;
      public IGuest Guest;
      public LoadReturnCode LoadReturnCode;

      public GuestInfo(Type type) => ClassType = type;
    }
    static List<GuestInfo> guests;

    static void CheckInGuests()
    {
      if (guests is null)
        return;

      foreach (var guestInfo in guests)
      {
        if (guestInfo.Guest is object)
          continue;

        bool load = true;
        foreach (var guestPlugIn in guestInfo.ClassType.GetCustomAttributes(typeof(GuestPlugInIdAttribute), false).Cast<GuestPlugInIdAttribute>())
          load |= PlugIn.GetPlugInInfo(guestPlugIn.PlugInId).IsLoaded;

        if (!load)
          continue;

        guestInfo.Guest = Activator.CreateInstance(guestInfo.ClassType) as IGuest;

        string complainMessage = string.Empty;
        try { guestInfo.LoadReturnCode = guestInfo.Guest.OnCheckIn(ref complainMessage); }
        catch (Exception e)
        {
          guestInfo.LoadReturnCode = LoadReturnCode.ErrorShowDialog;
          complainMessage = e.Message;
        }

        if (guestInfo.LoadReturnCode == LoadReturnCode.ErrorShowDialog)
        {
          using
          (
            var taskDialog = new TaskDialog(MethodBase.GetCurrentMethod().DeclaringType.FullName)
            {
              Title = guestInfo.Guest.Name,
              MainIcon = External.UI.TaskDialogIcons.IconError,
              AllowCancellation = false,
              MainInstruction = $"{guestInfo.Guest.Name} failed to load",
              MainContent = complainMessage
            }
          )
          {
            taskDialog.Show();
          }
        }
      }
    }

    static void CheckOutGuests()
    {
      if (guests is null)
        return;

      foreach (var guestInfo in Enumerable.Reverse(guests))
      {
        if (guestInfo.Guest is null)
          continue;

        if (guestInfo.LoadReturnCode == LoadReturnCode.Success)
          continue;

        try { guestInfo.Guest.OnCheckOut(); guestInfo.LoadReturnCode = LoadReturnCode.ErrorNoDialog; }
        catch (Exception) { }
      }
    }
    #endregion

    #region Document
    static void OnNewDocument(object sender, DocumentEventArgs e)
    {
      // If a new document is created without template it is updated from Revit.ActiveDBDocument
      Debug.Assert(string.IsNullOrEmpty(e.Document.TemplateFileUsed));

      UpdateDocumentUnits(e.Document);
      UpdateDocumentUnits(e.Document, Revit.ActiveDBDocument);
    }

    static void EndOpenDocumentInitialViewUpdate(object sender, DocumentEventArgs e)
    {
      if (e.Document.IsOpening)
        AuditUnits(e.Document);
    }

    static void AuditTolerances(RhinoDoc doc)
    {
      if (doc is object)
      {
        var maxDistanceTolerance = Revit.VertexTolerance * UnitScale(UnitSystem.Feet, doc.ModelUnitSystem);
        if (doc.ModelAbsoluteTolerance > maxDistanceTolerance)
          doc.ModelAbsoluteTolerance = maxDistanceTolerance;

        var maxAngleTolerance = Revit.AngleTolerance;
        if (doc.ModelAngleToleranceRadians > maxAngleTolerance)
          doc.ModelAngleToleranceRadians = maxAngleTolerance;
      }
    }

    static void AuditUnits(RhinoDoc doc)
    {
      if (Command.InScriptRunnerCommand())
        return;

      if (Revit.ActiveUIDocument.Document is DB.Document revitDoc)
      {
        var units = revitDoc.GetUnits();
        var lengthFormatoptions = units.GetFormatOptions(DB.UnitType.UT_Length);
        var RevitModelUnitSystem = lengthFormatoptions.DisplayUnits.ToUnitSystem();
        var GrasshopperModelUnitSystem = GH.Guest.ModelUnitSystem != UnitSystem.Unset ? GH.Guest.ModelUnitSystem : doc.ModelUnitSystem;
        if (doc.ModelUnitSystem != RevitModelUnitSystem || doc.ModelUnitSystem != GrasshopperModelUnitSystem)
        {
          using
          (
            var taskDialog = new TaskDialog("Units")
            {
              MainIcon = External.UI.TaskDialogIcons.IconInformation,
              TitleAutoPrefix = true,
              AllowCancellation = true,
              MainInstruction = "Model units mismatch.",
              MainContent = "What units do you want to use?",
              ExpandedContent = $"The model you are opening is in {doc.ModelUnitSystem}{Environment.NewLine}Active Revit model '{revitDoc.Title}' units are {RevitModelUnitSystem}",
              FooterText = "Current version: " + Addin.DisplayVersion
            }
          )
          {
            taskDialog.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink1, $"Continue opening in {doc.ModelUnitSystem}", $"Rhino and Grasshopper will work in {doc.ModelUnitSystem}");
            taskDialog.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink2, $"Adjust Rhino model to {RevitModelUnitSystem} like Revit", $"Scale Rhino model by {UnitScale(doc.ModelUnitSystem, RevitModelUnitSystem)}");
            taskDialog.DefaultButton = Autodesk.Revit.UI.TaskDialogResult.CommandLink2;

            if (GH.Guest.ModelUnitSystem != UnitSystem.Unset)
            {
              taskDialog.ExpandedContent += $"{Environment.NewLine}Documents opened in Grasshopper were working in {GH.Guest.ModelUnitSystem}";
              if (GrasshopperModelUnitSystem != doc.ModelUnitSystem && GrasshopperModelUnitSystem != RevitModelUnitSystem)
              {
                taskDialog.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink3, $"Adjust Rhino model to {GH.Guest.ModelUnitSystem} like Grasshopper", $"Scale Rhino model by {UnitScale(doc.ModelUnitSystem, GH.Guest.ModelUnitSystem)}");
                taskDialog.DefaultButton = Autodesk.Revit.UI.TaskDialogResult.CommandLink3;
              }
            }

            switch (taskDialog.Show())
            {
            case Autodesk.Revit.UI.TaskDialogResult.CommandLink2:
                doc.ModelAngleToleranceRadians = Revit.AngleTolerance;
                doc.ModelDistanceDisplayPrecision = Clamp((int) -Log10(lengthFormatoptions.Accuracy), 0, 7);
                doc.ModelAbsoluteTolerance = Revit.VertexTolerance * UnitScale(UnitSystem.Feet, RevitModelUnitSystem);
                doc.AdjustModelUnitSystem(RevitModelUnitSystem, true);
                UpdateViewConstructionPlanesFrom(doc, revitDoc);
              break;
              case Autodesk.Revit.UI.TaskDialogResult.CommandLink3:
                doc.ModelAngleToleranceRadians = Revit.AngleTolerance;
                doc.ModelDistanceDisplayPrecision = Clamp(Grasshopper.CentralSettings.FormatDecimalDigits, 0, 7);
                doc.ModelAbsoluteTolerance = Revit.VertexTolerance * UnitScale(UnitSystem.Feet, GH.Guest.ModelUnitSystem);
                doc.AdjustModelUnitSystem(GH.Guest.ModelUnitSystem, true);
                UpdateViewConstructionPlanesFrom(doc, revitDoc);
                break;
              default:
                AuditTolerances(doc);
                break;
            }
          }
        }
      }
    }

    static void UpdateDocumentUnits(RhinoDoc rhinoDoc, DB.Document revitDoc = null)
    {
      bool docModified = rhinoDoc.Modified;
      try
      {
        if (revitDoc is null)
        {
          rhinoDoc.ModelUnitSystem = UnitSystem.None;
          rhinoDoc.ModelAbsoluteTolerance = Revit.VertexTolerance;
          rhinoDoc.ModelAngleToleranceRadians = Revit.AngleTolerance;
        }
        else if (rhinoDoc.ModelUnitSystem == UnitSystem.None)
        {
          var units = revitDoc.GetUnits();
          var lengthFormatoptions = units.GetFormatOptions(DB.UnitType.UT_Length);
          rhinoDoc.ModelUnitSystem = lengthFormatoptions.DisplayUnits.ToUnitSystem();
          rhinoDoc.ModelAngleToleranceRadians = Revit.AngleTolerance;
          rhinoDoc.ModelDistanceDisplayPrecision = Clamp((int) -Log10(lengthFormatoptions.Accuracy), 0, 7);
          rhinoDoc.ModelAbsoluteTolerance = Revit.VertexTolerance * UnitScale(UnitSystem.Feet, rhinoDoc.ModelUnitSystem);
          //switch (rhinoDoc.ModelUnitSystem)
          //{
          //  case UnitSystem.None: break;
          //  case UnitSystem.Feet:
          //  case UnitSystem.Inches:
          //    newDoc.ModelAbsoluteTolerance = (1.0 / 160.0) * UnitScale(UnitSystem.Inches, newDoc.ModelUnitSystem);
          //    break;
          //  default:
          //    newDoc.ModelAbsoluteTolerance = 0.1 * UnitScale(UnitSystem.Millimeters, newDoc.ModelUnitSystem);
          //    break;
          //}

          UpdateViewConstructionPlanesFrom(rhinoDoc, revitDoc);
        }
      }
      finally
      {
        rhinoDoc.Modified = docModified;
      }
    }

    static void UpdateViewConstructionPlanesFrom(RhinoDoc rhinoDoc, DB.Document revitDoc)
    {
      if (!string.IsNullOrEmpty(rhinoDoc.TemplateFileUsed))
        return;

      if (rhinoDoc.IsCreating)
      {
        Revit.EnqueueAction(doc => UpdateViewConstructionPlanesFrom(rhinoDoc, doc));
        return;
      }

      bool imperial = rhinoDoc.ModelUnitSystem == UnitSystem.Feet || rhinoDoc.ModelUnitSystem == UnitSystem.Inches;

      var modelGridSpacing = imperial ?
      1.0 * UnitScale(UnitSystem.Yards, rhinoDoc.ModelUnitSystem) :
      1.0 * UnitScale(UnitSystem.Meters, rhinoDoc.ModelUnitSystem);

      var modelSnapSpacing = imperial ?
      1 / 16.0 * UnitScale(UnitSystem.Inches, rhinoDoc.ModelUnitSystem) :
      1.0 * UnitScale(UnitSystem.Millimeters, rhinoDoc.ModelUnitSystem);

      var modelThickLineFrequency = imperial ? 6 : 5;

      // Views
      {
        foreach (var view in rhinoDoc.Views)
        {
          var cplane = view.MainViewport.GetConstructionPlane();

          cplane.GridSpacing = modelGridSpacing;
          cplane.SnapSpacing = modelSnapSpacing;
          cplane.ThickLineFrequency = modelThickLineFrequency;

          view.MainViewport.SetConstructionPlane(cplane);

          var min = cplane.Plane.PointAt(-cplane.GridSpacing * cplane.GridLineCount, -cplane.GridSpacing * cplane.GridLineCount, 0.0);
          var max = cplane.Plane.PointAt(+cplane.GridSpacing * cplane.GridLineCount, +cplane.GridSpacing * cplane.GridLineCount, 0.0);
          var bbox = new BoundingBox(min, max);

          // Zoom to grid
          view.MainViewport.ZoomBoundingBox(bbox);

          // Adjust to extens in case There is anything in the viewports like Grasshopper previews.
          view.MainViewport.ZoomExtents();
        }
      }
    }
    #endregion

    #region Status
    private static bool CoreIsLicenseExpired() => !RhinoApp.IsLicenseValidated;
    public static bool IsLicenseExpired = false;
    #endregion

    #region Rhino UI
    /*internal*/ public static void InvokeInHostContext(Action action) => core.InvokeInHostContext(action);
    /*internal*/ public static T InvokeInHostContext<T>(Func<T> func) => core.InvokeInHostContext(func);

    public static bool Exposed
    {
      get => MainWindow.Visible && MainWindow.WindowStyle != ProcessWindowStyle.Minimized;
      set
      {
        MainWindow.Visible = value;

        if (value && MainWindow.WindowStyle == ProcessWindowStyle.Minimized)
          MainWindow.WindowStyle = ProcessWindowStyle.Normal;
      }
    }

    class ExposureSnapshot
    {
      readonly bool Visible             = MainWindow.Visible;
      readonly ProcessWindowStyle Style = MainWindow.WindowStyle;

      public void Restore()
      {
        MainWindow.WindowStyle          = Style;
        MainWindow.Visible              = Visible;
      }
    }
    static ExposureSnapshot QuiescentExposure;

    static void BeginCommand(object sender, CommandEventArgs e)
    {
      if (!Command.InScriptRunnerCommand())
      {
        // Capture Rhino Main Window exposure to restore it when user ends picking
        QuiescentExposure = new ExposureSnapshot();

        // Disable Revit Main Window while in Command
        Revit.MainWindow.Enabled = false;
      }
    }

    static void EndCommand(object sender, CommandEventArgs e)
    {
      if (!Command.InScriptRunnerCommand())
      {
        // Reenable Revit main window
        Revit.MainWindow.Enabled = true;

        if (MainWindow.WindowStyle != ProcessWindowStyle.Maximized)
        {
          // Restore Rhino Main Window exposure
          QuiescentExposure?.Restore();
          QuiescentExposure = null;
          RhinoApp.SetFocusToMainWindow();
        }
      }
    }

    static void MainLoop(object sender, EventArgs e)
    {
      if (!Command.InScriptRunnerCommand()) 
      {
        if (RhinoDoc.ActiveDoc is RhinoDoc rhinoDoc)
        {
          // Keep Rhino window exposed to user while in a get operation
          if (RhinoGet.InGet(rhinoDoc))
          {
            // if there is no floating viewport visible...
            if (!rhinoDoc.Views.Where(x => x.Floating).Any())
            {
              if (!Exposed)
                Exposed = true;
            }
          }
        }
      }
    }

    public static void Show()
    {
      Exposed = true;
      MainWindow.BringToFront();
    }

    public static async void ShowAsync()
    {
      await External.ActivationGate.Yield();

      Show();
    }

    public static async void RunScriptAsync(string script, bool activate)
    {
      if (string.IsNullOrEmpty(script))
        return;

      await External.ActivationGate.Yield();

      if (activate)
        RhinoApp.SetFocusToMainWindow();

      RhinoApp.RunScript(script, false);
    }

    public static Result RunCommandAbout()
    {
      var docSerial = RhinoDoc.ActiveDoc.RuntimeSerialNumber;
      var result = RhinoApp.RunScript("!_About", false) ? Result.Succeeded : Result.Failed;

      if (result == Result.Succeeded && docSerial != RhinoDoc.ActiveDoc.RuntimeSerialNumber)
      {
        Exposed = true;
        return Result.Succeeded;
      }

      return Result.Cancelled;
    }

    /// <summary>
    /// Represents a Pseudo-modal loop
    /// This class implements IDisposable, it's been designed to be used in a using statement.
    /// </summary>
    internal sealed class ModalScope : IDisposable
    {
      static bool wasExposed = false;
      readonly bool wasEnabled = Revit.MainWindow.Enabled;

      public ModalScope() => Revit.MainWindow.Enabled = false;

      void IDisposable.Dispose() => Revit.MainWindow.Enabled = wasEnabled;

      public Result Run(bool exposeMainWindow)
      {
        return Run(exposeMainWindow, !Keyboard.IsKeyDown(Key.LeftCtrl));
      }

      public Result Run(bool exposeMainWindow, bool restorePopups)
      {
        try
        {
          if (exposeMainWindow) Exposed = true;
          else if (restorePopups) Exposed = wasExposed || MainWindow.WindowStyle == ProcessWindowStyle.Minimized;

          if (restorePopups)
            MainWindow.ShowOwnedPopups();

          // Activate a Rhino window to keep the loop running
          var activePopup = MainWindow.ActivePopup;
          if (activePopup.IsInvalid || exposeMainWindow)
          {
            if (!Exposed)
              return Result.Cancelled;

            RhinoApp.SetFocusToMainWindow();
          }
          else
          {
            activePopup.BringToFront();
          }

          while (Rhinoceros.Run())
          {
            if (!Exposed && MainWindow.ActivePopup.IsInvalid)
              break;
          }

          return Result.Succeeded;
        }
        finally
        {
          wasExposed = Exposed;

          Revit.MainWindow.Enabled = true;
          WindowHandle.ActiveWindow = Revit.MainWindow;
          MainWindow.HideOwnedPopups();
          Exposed = false;
        }
      }
    }
    #endregion
  }
}
