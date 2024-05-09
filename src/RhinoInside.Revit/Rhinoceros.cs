using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.PlugIns;
using Rhino.Runtime.InProcess;
using static Rhino.RhinoMath;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit
{
  using Numerical;
  using Convert.Geometry;
  using Convert.Units;
  using External.ApplicationServices.Extensions;
  using RhinoWindows.Forms;
  using static Diagnostics;

  /// <summary>
  /// Provides a set of static (Shared in Visual Basic) methods for accessing Rhinoceros API from Rhino.Inside.
  /// </summary>
  public static partial class Rhinoceros
  {
    #region Revit Interface
    static RhinoCore core;
    internal static readonly string SchemeName = $"Inside-Revit-{Core.Host.Services.VersionNumber}";
    internal static string[] StartupLog;

    internal static bool InitEto(Assembly assembly)
    {
      if (Eto.Forms.Application.Instance is null)
        new Eto.Forms.Application(Eto.Platforms.Wpf).Attach();

      return true;
    }

    internal static bool InitRhinoCommon(Assembly assembly)
    {
      // We should Init Eto before Rhino does it.
      // This should force `AssemblyResolver` to call `InitEto`.
      if (Eto.Forms.Application.Instance is null)
      {
        Logger.LogCritical("Eto failed to load", $"Assembly = {typeof(Eto.Forms.Application).Assembly.FullName}");
        //return false;
      }

      var hostMainWindow = (WindowHandle) Core.Host.MainWindowHandle;

      // Save Revit window status
      bool wasEnabled = hostMainWindow.Enabled;
      var activeWindow = WindowHandle.ActiveWindow ?? hostMainWindow;

      // Disable Revit window
      hostMainWindow.Enabled = false;

      // Load RhinoCore
      try
      {
        EventHandler<Rhino.Runtime.LicenseStateChangedEventArgs> LicenseStateChanged = default;
        RhinoApp.LicenseStateChanged += LicenseStateChanged = (sender, arg) =>
        {
          if(!arg.CallingRhinoCommonAllowed)
            Core.CurrentStatus = Core.Status.Expired;
        };

        var args = new List<string>();

        if (DebugLogging.Current.Enabled)
        {
          args.Add("/captureprintcalls");
          args.Add("/stopwatch");
        }

        if (Core.IsolateSettings)
        {
          args.Add($"/scheme={SchemeName}");
        }

        if (Core.UseHostLanguage)
        {
          args.Add($"/language={Core.Host.Services.Language.ToLCID()}");
        }

#if NET
        args.Add("/netcore");
#else
        args.Add("/netfx");
#endif
        args.Add("/nosplash");
        //args.Add("/safemode");
        //args.Add("/notemplate");

        var hostWnd = Core.KeepUIOnTop ? hostMainWindow.Handle : IntPtr.Zero;
        core = new RhinoCore(args.ToArray(), WindowStyle.Hidden, hostWnd);
      }
      catch (Exception e)
      {
        ErrorReport.TraceException(e, Core.Host);

        if (Core.CurrentStatus > Core.Status.Unavailable)
        {
          ErrorReport.ReportException(e, Core.Host);
          Core.CurrentStatus = Core.Status.Failed;
        }

        return false;
      }
      finally
      {
        // Enable Revit window back
        hostMainWindow.Enabled = wasEnabled;
        WindowHandle.ActiveWindow = activeWindow;

        StartupLog = RhinoApp.CapturedCommandWindowStrings(true);
        RhinoApp.CommandWindowCaptureEnabled = false;
      }

      FormUtilities.ApplicationName = FormUtilities.ApplicationName.Replace("Rhino ", "Rhino.Inside ");
      Rhino.Runtime.PythonScript.AddRuntimeAssembly(Assembly.GetExecutingAssembly());

      MainWindow = (WindowHandle) RhinoApp.MainWindowHandle();
      MainWindow.ExtendedWindowStyles |= ExtendedWindowStyles.AppWindow;

      return External.ActivationGate.AddGateWindow(MainWindow.Handle, Core.ActivationEvent);
    }

    internal static bool InitGrasshopper(Assembly assembly)
    {
      var PluginId = new Guid(0xB45A29B1, 0x4343, 0x4035, 0x98, 0x9E, 0x04, 0x4E, 0x85, 0x80, 0xD9, 0xCF);
      return PlugIn.PlugInExists(PluginId, out bool loaded, out bool loadProtected) & (loaded | !loadProtected);
    }

    internal static ARUI.Result Startup()
    {
      if (!RhinoApp.CanSave)
        return ARUI.Result.Cancelled;

      RhinoApp.MainLoop                         += MainLoop;

      RhinoDoc.NewDocument                      += OnNewDocument;
      RhinoDoc.EndOpenDocumentInitialViewUpdate += EndOpenDocumentInitialViewUpdate;

      Command.BeginCommand                      += BeginCommand;
      Command.EndCommand                        += EndCommand;

      // Alternative to /runscript= Rhino command line option
      RunScriptAsync
      (
        script:   Environment.GetEnvironmentVariable("RhinoInside_RunScript"),
        activate: Core.StartupMode == CoreStartupMode.OnStartup
      );

      // Add DefaultRenderAppearancePath to Rhino settings if missing
      {
        var DefaultRenderAppearancePath = System.IO.Path.Combine
        (
          Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
          "Autodesk Shared",
          "Materials",
          "Textures"
        );

        if (!Rhino.ApplicationSettings.FileSettings.GetSearchPaths().Any(x => x.Equals(DefaultRenderAppearancePath, StringComparison.OrdinalIgnoreCase)))
          Rhino.ApplicationSettings.FileSettings.AddSearchPath(DefaultRenderAppearancePath, -1);

        // TODO: Add also AdditionalRenderAppearancePaths content from Revit.ini if missing ??
      }

      // Reset document units
      if (string.IsNullOrEmpty(Rhino.ApplicationSettings.FileSettings.TemplateFile))
      {
        UpdateDocumentUnits(RhinoDoc.ActiveDoc);
        UpdateDocumentUnits(RhinoDoc.ActiveDoc, Revit.ActiveUIDocument?.Document);
      }

      // Load Guests
      CheckInGuests();

      return ARUI.Result.Succeeded;
    }

    internal static ARUI.Result Shutdown()
    {
      if (core is null)
        return ARUI.Result.Failed;

      External.ActivationGate.RemoveGateWindow(MainWindow.Handle);
      MainWindow.BringToFront();

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
        return ARUI.Result.Failed;
      }

      return ARUI.Result.Succeeded;
    }

    internal static WindowHandle MainWindow = WindowHandle.Zero;

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
      public GuestResult CheckInResult;

      public GuestInfo(Type type) => ClassType = type;
    }
    static List<GuestInfo> guests;

    static void AppendExceptionExpandedContent(StringBuilder builder, Exception e)
    {
      switch (e)
      {
        case System.BadImageFormatException badImageFormatException:
          if (badImageFormatException.FileName != null) builder.AppendLine($"FileName: {badImageFormatException.FileName}");
          if (badImageFormatException.FusionLog != null) builder.AppendLine($"FusionLog: {badImageFormatException.FusionLog}");
          break;
        case System.IO.FileNotFoundException fileNotFoundException:
          if (fileNotFoundException.FileName != null) builder.AppendLine($"FileName: {fileNotFoundException.FileName}");
          if (fileNotFoundException.FusionLog != null) builder.AppendLine($"FusionLog: {fileNotFoundException.FusionLog}");
          break;
        case System.IO.FileLoadException fileLoadException:
          if (fileLoadException.FileName != null) builder.AppendLine($"FileName: {fileLoadException.FileName}");
          if (fileLoadException.FusionLog != null) builder.AppendLine($"FusionLog: {fileLoadException.FusionLog}");
          break;
      }
    }

    internal static void CheckInGuests()
    {
      if (guests is object)
        return;

      var types = Type.EmptyTypes;
      try { types = Assembly.GetExecutingAssembly().GetTypes(); }
      catch (ReflectionTypeLoadException ex)
      { types = ex.Types.Where(x => x?.TypeInitializer is object).ToArray(); }

      // Look for Guests
      guests = types.
        Where(x => typeof(IGuest).IsAssignableFrom(x)).
        Where(x => !x.IsInterface && !x.IsAbstract && !x.ContainsGenericParameters).
        Select(x => new GuestInfo(x)).
        ToList();

      foreach (var guestInfo in guests)
      {
        if (guestInfo.Guest is object)
          continue;

        bool load = true;
        foreach (var guestPlugIn in guestInfo.ClassType.GetCustomAttributes(typeof(GuestPlugInIdAttribute), false).Cast<GuestPlugInIdAttribute>())
          load |= PlugIn.GetPlugInInfo(guestPlugIn.PlugInId).IsLoaded;

        if (!load)
          continue;

        string mainContent = string.Empty;
        string expandedContent = string.Empty;
        var checkInArgs = new CheckInArgs();
        try
        {
          guestInfo.Guest = Activator.CreateInstance(guestInfo.ClassType) as IGuest;
          guestInfo.CheckInResult = guestInfo.Guest.EntryPoint(default, checkInArgs);
        }
        catch (Exception e)
        {
          guestInfo.CheckInResult = GuestResult.Failed;

          var mainContentBuilder = new StringBuilder();
          var expandedContentBuilder = new StringBuilder();
          while (e.InnerException != default)
          {
            mainContentBuilder.AppendLine($"· {e.Message}");
            AppendExceptionExpandedContent(expandedContentBuilder, e);
            e = e.InnerException;
          }
          mainContentBuilder.AppendLine($"· {e.Message}");
          AppendExceptionExpandedContent(expandedContentBuilder, e);

          mainContent = mainContentBuilder.ToString();
          expandedContent = expandedContentBuilder.ToString();
        }

        if (guestInfo.CheckInResult == GuestResult.Failed)
        {
          var guestName = guestInfo.Guest?.Name ?? guestInfo.ClassType.Namespace;

          {
            var journalContent = new StringBuilder();
            journalContent.AppendLine($"{guestName} failed to load");
            journalContent.AppendLine(mainContent);
            journalContent.AppendLine(expandedContent);
            Revit.ActiveDBApplication.WriteJournalComment(journalContent.ToString(), false);
          }

          using
          (
            var taskDialog = new ARUI.TaskDialog(Core.DisplayVersion)
            {
              Id = $"{MethodBase.GetCurrentMethod().DeclaringType.FullName}.{MethodBase.GetCurrentMethod().Name}",
              MainIcon = External.UI.TaskDialogIcons.IconError,
              AllowCancellation = false,
              MainInstruction = $"{guestName} failed to load",
              MainContent = mainContent +
                            Environment.NewLine + "Do you want to report this problem by email to tech@mcneel.com?",
              ExpandedContent = expandedContent,
              FooterText = "Press CTRL+C to copy this information to Clipboard",
              CommonButtons = ARUI.TaskDialogCommonButtons.Yes | ARUI.TaskDialogCommonButtons.Cancel,
              DefaultButton = ARUI.TaskDialogResult.Yes
            }
          )
          {
            if (taskDialog.Show() == ARUI.TaskDialogResult.Yes)
            {
              ErrorReport.SendEmail
              (
                Core.Host,
                subject: taskDialog.MainInstruction,
                body: null,
                includeAddinsList: false,
                attachments: new string[]
                {
                  Core.Host.Services.RecordingJournalFilename
                }
              );
            }
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

        if (guestInfo.CheckInResult == GuestResult.Succeeded)
          continue;

        try { guestInfo.Guest.EntryPoint(default, new CheckOutArgs()); guestInfo.CheckInResult = GuestResult.Cancelled; }
        catch (Exception) { }
      }

      guests = null;
    }
    #endregion

    #region Document
    static void OnNewDocument(object sender, DocumentEventArgs e)
    {
      // If a new document is created without template it is updated from Revit.ActiveDBDocument
      Debug.Assert(string.IsNullOrEmpty(e.Document.TemplateFileUsed));

      if (e.Document is RhinoDoc)
      {
        UpdateDocumentUnits(e.Document);
        UpdateDocumentUnits(e.Document, Revit.ActiveUIDocument?.Document);
      }
    }

    static void EndOpenDocumentInitialViewUpdate(object sender, DocumentEventArgs e)
    {
      if (e.Document.IsOpening)
      {
        EventHandler idle = default;
        RhinoApp.Idle += idle = (s, a) =>
        {
          RhinoApp.Idle -= idle;
          InvokeInHostContext(() => AuditUnits(e.Document, allowNoScale: true));
        };
      }
    }

    static void AuditTolerances(RhinoDoc doc)
    {
      if (doc is object)
      {
        var revitTol = GeometryTolerance.Internal;
        var maxDistanceTolerance = UnitScale.Convert(revitTol.VertexTolerance, UnitScale.Internal, UnitScale.GetModelScale(doc));
        if (doc.ModelAbsoluteTolerance > maxDistanceTolerance)
          doc.ModelAbsoluteTolerance = maxDistanceTolerance;

        var maxAngleTolerance = revitTol.AngleTolerance;
        if (doc.ModelAngleToleranceRadians > maxAngleTolerance)
          doc.ModelAngleToleranceRadians = maxAngleTolerance;
      }
    }

    internal static void AuditUnits(RhinoDoc doc, bool allowNoScale = false)
    {
      if (Command.InScriptRunnerCommand())
        return;

      if (Revit.ActiveUIDocument?.Document is ARDB.Document revitDoc)
      {
        var revitTol = GeometryTolerance.Internal;
        var units = revitDoc.GetUnits();
        var RevitModelUnitScale = units.ToUnitScale(out var distanceDisplayPrecision);
        var RhinoModelUnitScale = UnitScale.GetModelScale(doc);
        var GrasshopperModelUnitScale = GH.Guest.ModelUnitScale != UnitScale.Unset ? GH.Guest.ModelUnitScale : RhinoModelUnitScale;
        if (RhinoModelUnitScale != RevitModelUnitScale || RhinoModelUnitScale != GrasshopperModelUnitScale)
        {
          var hasUnits = doc.ModelUnitSystem != UnitSystem.Unset && doc.ModelUnitSystem != UnitSystem.None;
          var expandedContent = allowNoScale ?
            $"The Rhino model you are opening is in {RhinoModelUnitScale}{Environment.NewLine}Revit document '{revitDoc.Title}' length units are {RevitModelUnitScale}" :
            string.Empty;

          var dialogId = !allowNoScale && hasUnits ?
            "Rhino.Inside.Revit.DocumentUnitsWarning" : // Shown when Grasshopper window is activated and Revit and Rhino units do not coincide.
            "Rhino.Inside.Revit.DocumentUnitsMismatch"; // Shown when a new Rhino document is opened and units do not coincide with Revit active document units.

          using
          (
            var taskDialog = new ARUI.TaskDialog("Units")
            {
              Id = dialogId,
              MainIcon = External.UI.TaskDialogIcons.IconInformation,
              TitleAutoPrefix = true,
              AllowCancellation = hasUnits,
              MainInstruction = hasUnits ? (allowNoScale ? "Model units mismatch." : "Model units mismatch warning.") : "Rhino model has no units.",
              MainContent = allowNoScale ? "What units do you want to use?" : $"Revit document '{revitDoc.Title}' length units are {RevitModelUnitScale}." + (hasUnits ? $"{Environment.NewLine}Rhino is working in {RhinoModelUnitScale}." : string.Empty),
              ExpandedContent = expandedContent,
              FooterText = "Current version: " + Core.DisplayVersion
            }
          )
          {
            if (!allowNoScale && hasUnits)
            {
#if REVIT_2020
              taskDialog.EnableDoNotShowAgain(taskDialog.Id, true, "Do not show again");
#else
              // Without the ability of checking DoNotShowAgain this may be too anoying.
              return;
#endif
            }
            else
            {
              taskDialog.AddCommandLink
              (
                ARUI.TaskDialogCommandLinkId.CommandLink2,
                $"Use {RevitModelUnitScale} like Revit",
                hasUnits ? $"Scale Rhino model by {UnitScale.Convert(1.0, RhinoModelUnitScale, RevitModelUnitScale)}" : string.Empty
              );
              taskDialog.DefaultButton = ARUI.TaskDialogResult.CommandLink2;
            }

            if (hasUnits)
            {
              if (allowNoScale)
              {
                taskDialog.AddCommandLink
                (
                  ARUI.TaskDialogCommandLinkId.CommandLink1,
                  $"Continue in {RhinoModelUnitScale}",
                  $"Rhino and Grasshopper will work in {RhinoModelUnitScale}"
                );
                taskDialog.DefaultButton = ARUI.TaskDialogResult.CommandLink1;
              }
              else
              {
                taskDialog.CommonButtons = ARUI.TaskDialogCommonButtons.Ok;
                taskDialog.DefaultButton = ARUI.TaskDialogResult.Ok;
              }
            }

            if (GH.Guest.ModelUnitScale != UnitScale.Unset && GH.Guest.ModelUnitScale != UnitScale.None)
            {
              taskDialog.ExpandedContent += $"{Environment.NewLine}Documents opened in Grasshopper were working in {GrasshopperModelUnitScale}";
              if (GrasshopperModelUnitScale != RhinoModelUnitScale && GrasshopperModelUnitScale != RevitModelUnitScale)
              {
                taskDialog.AddCommandLink
                (
                  ARUI.TaskDialogCommandLinkId.CommandLink3,
                  $"Adjust Rhino model to {GrasshopperModelUnitScale} like Grasshopper",
                  hasUnits ? $"Scale Rhino model by {UnitScale.Convert(1.0, RhinoModelUnitScale, GrasshopperModelUnitScale)}" : string.Empty
                );
                taskDialog.DefaultButton = ARUI.TaskDialogResult.CommandLink3;
              }
            }

            var active = WindowHandle.ActiveWindow;
            var result = taskDialog.Show();
            var undoRecord = doc.BeginUndoRecord("Revit Units");
            try
            {
              switch (result)
              {
                case ARUI.TaskDialogResult.CommandLink2:
                  doc.ModelAngleToleranceRadians = revitTol.AngleTolerance;
                  doc.ModelDistanceDisplayPrecision = distanceDisplayPrecision;
                  doc.ModelAbsoluteTolerance = UnitScale.Convert(revitTol.VertexTolerance, UnitScale.Internal, RevitModelUnitScale);
                  UnitScale.SetModelUnitScale(doc, RevitModelUnitScale, scale: true);
                  AdjustViewConstructionPlanes(doc);
                  break;

                case ARUI.TaskDialogResult.CommandLink3:
                  doc.ModelAngleToleranceRadians = revitTol.AngleTolerance;
                  doc.ModelDistanceDisplayPrecision = (int) Arithmetic.Clamp(Grasshopper.CentralSettings.FormatDecimalDigits, 0, 7);
                  doc.ModelAbsoluteTolerance = UnitScale.Convert(revitTol.VertexTolerance, UnitScale.Internal, GH.Guest.ModelUnitScale);
                  UnitScale.SetModelUnitScale(doc, GH.Guest.ModelUnitScale, scale: true);
                  AdjustViewConstructionPlanes(doc);
                  break;

                default:
                  AuditTolerances(doc);
                  break;
              }
            }
            finally
            {
              WindowHandle.ActiveWindow = active;
              doc.EndUndoRecord(undoRecord);
            }

            doc.ClearUndoRecords(undoRecord, purgeDeletedObjects: true);
          }
        }
      }
    }

    static void UpdateDocumentUnits(RhinoDoc rhinoDoc, ARDB.Document revitDoc = null)
    {
      bool docModified = rhinoDoc.Modified;
      try
      {
        var revitTol = GeometryTolerance.Internal;

        if (revitDoc is null)
        {
          rhinoDoc.ModelUnitSystem = UnitSystem.None;
          rhinoDoc.ModelAbsoluteTolerance = revitTol.VertexTolerance;
          rhinoDoc.ModelAngleToleranceRadians = revitTol.AngleTolerance;
        }
        else if (rhinoDoc.ModelUnitSystem == UnitSystem.None)
        {
          var units = revitDoc.GetUnits();
          var modelUnitScale = units.ToUnitScale(out var distanceDisplayPrecision);
          UnitScale.SetModelUnitScale(rhinoDoc, modelUnitScale, scale: false);
          rhinoDoc.ModelAngleToleranceRadians = revitTol.AngleTolerance;
          rhinoDoc.ModelDistanceDisplayPrecision = distanceDisplayPrecision;
          rhinoDoc.ModelAbsoluteTolerance = UnitScale.Convert(revitTol.VertexTolerance, UnitScale.Internal, UnitScale.GetModelScale(rhinoDoc));

          // Like a Revit View at 1:100
          rhinoDoc.ModelSpaceHatchScalingEnabled = true;
          rhinoDoc.ModelSpaceHatchScale = 100.0 * (UnitScale.GetPageScale(rhinoDoc) / UnitScale.GetModelScale(rhinoDoc));
          rhinoDoc.Linetypes.LinetypeScale = 100.0;

          AdjustViewConstructionPlanes(rhinoDoc);
        }
      }
      finally
      {
        rhinoDoc.Modified = docModified;
      }
    }

    static void AdjustViewConstructionPlanes(RhinoDoc rhinoDoc)
    {
      if (!string.IsNullOrEmpty(rhinoDoc.TemplateFileUsed))
        return;

      if (rhinoDoc.IsCreating)
      {
        Revit.EnqueueIdlingAction
        (
          () =>
          {
            AdjustViewConstructionPlanes(rhinoDoc);

            foreach (var view in rhinoDoc.Views)
            {
              var viewport = view.MainViewport;

              // Zoom to grid
              {
                var cplane = viewport.GetConstructionPlane();
                var min = cplane.Plane.PointAt(-cplane.GridSpacing * cplane.GridLineCount, -cplane.GridSpacing * cplane.GridLineCount, 0.0);
                var max = cplane.Plane.PointAt(+cplane.GridSpacing * cplane.GridLineCount, +cplane.GridSpacing * cplane.GridLineCount, 0.0);
                var bbox = new BoundingBox(min, max);

                viewport.ZoomBoundingBox(bbox);
              }

              // Adjust to extens in case there is anything in the viewports like Grasshopper previews.
              viewport.ZoomExtents();
            }
          }
        );
        return;
      }

      foreach (var view in rhinoDoc.Views)
        AdjustViewCPlane(view.MainViewport);
    }

    static void AdjustViewCPlane(RhinoViewport viewport)
    {
      if (viewport.ParentView?.Document is RhinoDoc rhinoDoc)
      {
        var viewportUnitScale = viewport.ParentView is RhinoPageView ?
          UnitScale.GetPageScale(rhinoDoc) :
          UnitScale.GetModelScale(rhinoDoc);

        bool imperial = ((UnitSystem) viewportUnitScale).IsImperial();

        var modelGridSpacing = imperial ?
        UnitScale.Convert(1.0, UnitScale.Yards, viewportUnitScale) :
        UnitScale.Convert(1.0, UnitScale.Meters, viewportUnitScale);

        var modelSnapSpacing = imperial ?
        UnitScale.Convert(1.0, UnitScale.Yards, viewportUnitScale) :
        UnitScale.Convert(1.0, UnitScale.Meters, viewportUnitScale);

        var modelThickLineFrequency = imperial ? 6 : 5;

        var cplane = viewport.GetConstructionPlane();

        cplane.GridSpacing = modelGridSpacing;
        cplane.SnapSpacing = modelSnapSpacing;
        cplane.ThickLineFrequency = modelThickLineFrequency;

        viewport.SetConstructionPlane(cplane);
      }
    }
    #endregion

    #region Rhino UI
    [ThreadStatic]
    static bool InHostContext = false;

    /// <summary>
    /// Executes the specified delegate on Revit UI context.
    /// </summary>
    /// <param name="action">A delegate that contains a method to be called in Revit API context.</param>
    /// <since>1.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)/*, Obsolete("Please use Revit.Invoke method. This method will be removed on v1.5")*/]
    public static void InvokeInHostContext(Action action)
    {
      if (InHostContext) action();
      else try
      {
        InHostContext = true;
        core.InvokeInHostContext(action);
      }
      finally { InHostContext = false; }
    }

    /// <summary>
    /// Executes the specified delegate on Revit UI context.
    /// </summary>
    /// <typeparam name="T">The return type of the <paramref name="func"/>.</typeparam>
    /// <param name="func">A delegate that contains a method to be called in Revit API context.</param>
    /// <returns>The return value from the function being invoked.</returns>
    /// <since>1.0</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)/*, Obsolete("Please use Revit.Invoke method. This method will be removed on v1.5")*/]
    public static T InvokeInHostContext<T>(Func<T> func)
    {
      if (InHostContext) return func();
      else try
      {
        InHostContext = true;
        return core.InvokeInHostContext(func);
      }
      finally { InHostContext = false; }
    }

    internal static bool Exposed
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

    static ARDB.TransactionGroup CommandGroup;

    static void BeginCommand(object sender, CommandEventArgs e)
    {
      if (!Command.InScriptRunnerCommand())
      {
        // Capture Rhino Main Window exposure to restore it when user ends picking
        QuiescentExposure = new ExposureSnapshot();

        // Disable Revit Main Window while in Command
        Revit.MainWindow.Enabled = false;

        if (Revit.ActiveUIDocument is ARUI.UIDocument uiDocument)
        {
          var groupName = e.CommandLocalName;
          var pluginId = PlugIn.IdFromName(e.CommandPluginName);
          var plugin = PlugIn.Find(pluginId);
          var command = plugin?.GetCommands().FirstOrDefault(x => x.Id == e.CommandId);
          if
          (
            command?.GetType().GetRuntimeProperty("DisplayName") is PropertyInfo info &&
            info.CanRead &&
            info.PropertyType == typeof(string) &&
            info.GetValue(command) is string displayName &&
            !string.IsNullOrWhiteSpace(displayName)
          )
          {
            groupName = displayName;
          }

          CommandGroup = new ARDB.TransactionGroup
          (uiDocument.Document, groupName)
          { IsFailureHandlingForcedModal = true };

          CommandGroup.Start();
        }
      }
    }

    static void EndCommand(object sender, CommandEventArgs e)
    {
      if (!Command.InScriptRunnerCommand())
      {
        var result = e.CommandResult;

        try
        {
          using (CommandGroup) if (CommandGroup?.HasStarted() == true)
          {
            if (Revit.ActiveUIDocument.Document.IsModifiable)
            {
              var groupName = CommandGroup.GetName();
              if (string.IsNullOrWhiteSpace(groupName))
                groupName = e.CommandEnglishName;

              var message = "A Revit transaction or sub-transaction was opened but not closed." + Environment.NewLine +
                            $"All changes to the active document made by command '{groupName}' will be discarded.";

              Rhino.UI.Dialogs.ShowMessage
              (
                message, groupName,
                buttons: Rhino.UI.ShowMessageButton.OK,
                icon: Rhino.UI.ShowMessageIcon.Warning
              );

              result = Rhino.Commands.Result.Failure;
            }

            if (result == Rhino.Commands.Result.Failure || result == Rhino.Commands.Result.ExitRhino)
              CommandGroup.RollBack();
            else
              CommandGroup.Assimilate();
          }
        }
        catch { }
        CommandGroup = null;

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
          if (RhinoGet.InGet(rhinoDoc) && !Exposed)
          {
            if (RhinoGet.InGetObject(rhinoDoc) || RhinoGet.InGetPoint(rhinoDoc))
            {
              // If there is no floating viewport visible...
              if (!rhinoDoc.Views.Any(x => x.Floating))
              {
                var bounds = Revit.MainWindow.Bounds;
                var x = bounds.X + bounds.Width / 2;
                var y = bounds.Y + bounds.Height / 2;
                if (OpenRevitViewport(x - 400, y - 300) is null)
                  Exposed = true;
              }
            }
            // If we are in a GetString or GetInt we need the command prompt.
            else Exposed = true;
          }
        }
      }
    }

    static void Show()
    {
      Exposed = true;
      MainWindow.BringToFront();
    }

    internal static async void ShowAsync()
    {
      await External.ActivationGate.Yield();

      Show();
    }

    internal static async void RunScriptAsync(string script, bool activate)
    {
      if (string.IsNullOrWhiteSpace(script))
        return;

      await External.ActivationGate.Yield();

      if (activate)
        RhinoApp.SetFocusToMainWindow();

      RhinoApp.RunScript(script, false);
    }

    internal static ARUI.Result RunCommandAbout()
    {
      var docSerial = RhinoDoc.ActiveDoc.RuntimeSerialNumber;
      var result = RhinoApp.RunScript("!_About", false) ? ARUI.Result.Succeeded : ARUI.Result.Failed;

      if (result == ARUI.Result.Succeeded && docSerial != RhinoDoc.ActiveDoc.RuntimeSerialNumber)
      {
        Exposed = true;
        return ARUI.Result.Succeeded;
      }

      return ARUI.Result.Cancelled;
    }

    internal static ARUI.Result RunCommandOptions()
    {
      return RhinoApp.RunScript("!_Options", false) ? ARUI.Result.Succeeded : ARUI.Result.Failed;
    }

    internal static ARUI.Result RunCommandPackageManager()
    {
      return RhinoApp.RunScript("!_PackageManager", false) ? ARUI.Result.Succeeded : ARUI.Result.Failed;
    }

    #region Open Viewport
    const string RevitViewName = "Revit";
    internal static RhinoView OpenRevitViewport(int x, int y)
    {
      if (RhinoDoc.ActiveDoc is RhinoDoc rhinoDoc)
      {
        var openView = rhinoDoc.Views.FirstOrDefault(v => v.MainViewport.Name == RevitViewName);
        if (openView is null)
        {
          if
          (
            rhinoDoc.Views.Add
            (
              RevitViewName, DefinedViewportProjection.Perspective,
              new System.Drawing.Rectangle(x, y, 800, 600),
              true
            ) is RhinoView newView
          )
          {
            rhinoDoc.Views.ActiveView = newView;

            AdjustViewCPlane(newView.MainViewport);
            return newView;
          }
        }
        else
        {
          rhinoDoc.Views.ActiveView = openView;
          openView.BringToFront();
          return openView;
        }
      }

      return default;
    }

    internal static async void RunCommandOpenViewportAsync
    (
      Rhino.DocObjects.ViewportInfo vport,
      Rhino.DocObjects.ConstructionPlane cplane,
      bool setScreenPort
    )
    {
      var cursorPosition = System.Windows.Forms.Cursor.Position;
      await External.ActivationGate.Yield();

      if (OpenRevitViewport(cursorPosition.X + 50, cursorPosition.Y + 50) is RhinoView view)
      {
        if (setScreenPort && view.Floating && !view.Maximized)
          view.SetClientSize(vport.ScreenPort.Size);

        if (vport is object)
          view.MainViewport.SetViewportInfo(vport);

        if (cplane is object && view.MainViewport is RhinoViewport viewport)
        {
          if (cplane.Plane.IsValid)
            viewport.SetConstructionPlane(cplane);
          else if (viewport.GetFrustumNearPlane(out var nearPlane))
            viewport.SetConstructionPlane(nearPlane);
        }

        if (cplane is object || vport is object)
          view.Redraw();
      }
    }
    #endregion

    #endregion
  }
}
