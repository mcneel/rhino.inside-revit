using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms.Interop;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Microsoft.Win32.SafeHandles;

namespace RhinoInside.Revit.AddIn.Commands
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandGrasshopperPlayer : GrasshopperCommand
  {
    public static string CommandName => "Player";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      // Create a push button to trigger a command add it to the ribbon panel.
      var buttonData = NewPushButtonData<CommandGrasshopperPlayer, NeedsActiveDocument<Availability>>
      (
        name: CommandName,
        iconName: "GrasshopperPlayer.png",
        tooltip: "Loads and evals a Grasshopper definition",
        url: "reference/rir-interface#grasshopper-panel"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        StoreButton(CommandName, pushButton);
      }
    }

    public static Result BrowseForFile(out string filePath)
    {
      using
      (
        var openFileDialog = new System.Windows.Forms.OpenFileDialog()
        {
          Title = "Open Grasshopper file",
          Filter = "All Grasshopper files (*.gh, *.ghx)|*.gh;*.ghx|Grasshopper Binary (*.gh)|*.gh|Grasshopper Xml (*.ghx)|*.ghx",
          RestoreDirectory = true
        }
      )
      {
        switch (openFileDialog.ShowDialog(Revit.MainWindow.Handle))
        {
          case System.Windows.Forms.DialogResult.OK: filePath = openFileDialog.FileName; return Result.Succeeded;
          case System.Windows.Forms.DialogResult.Cancel: filePath = null; return Result.Cancelled;
          default: filePath = null; return Result.Failed;
        }
      }
    }

    public static Result ReadFromFile(string filePath, out GH_Document definition)
    {
      definition = null;

      var CurrentCulture = Thread.CurrentThread.CurrentCulture;
      try
      {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        var archive = new GH_Archive();
        if (!archive.ReadFromFile(filePath))
          return Result.Failed;

        definition = new GH_Document();
        if (archive.ExtractObject(definition, "Definition"))
          return Result.Succeeded;

        definition?.Dispose();
        definition = null;
        return Result.Failed;
      }
      catch (Exception)
      {
        return Result.Failed;
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = CurrentCulture;
      }
    }

    internal static IList<IGH_Param> GetInputParams(GH_Document definition)
    {
      var inputs = new List<IGH_Param>();

      // Collect input params
      foreach (var obj in definition.Objects)
      {
        if (!(obj is IGH_Param param))
          continue;

        if (param.Sources.Count != 0 || param.Recipients.Count == 0)
          continue;

        if (param.VolatileDataCount > 0)
          continue;

        if (param.Locked)
          continue;

        inputs.Add(param);
      }

      return inputs;
    }

    internal static Result PromptForInputs(UIDocument doc, IList<IGH_Param> inputs, out Dictionary<IGH_Param, IEnumerable<IGH_Goo>> values)
    {
      values = new Dictionary<IGH_Param, IEnumerable<IGH_Goo>>();
      foreach (var input in inputs.OrderBy((x) => x.Attributes.Pivot.Y))
      {
        switch (input)
        {
          case Param_Box box:
            var boxes = PromptBox(doc, input.NickName);
            if (boxes == null)
              return Result.Cancelled;
            values.Add(input, boxes);
            break;
          case Param_Point point:
            var points = PromptPoint(doc, input.NickName);
            if (points == null)
              return Result.Cancelled;
            values.Add(input, points);
            break;
          case Param_Line line:
            var lines = PromptLine(doc, input.NickName);
            if (lines == null)
              return Result.Cancelled;
            values.Add(input, lines);
            break;
          case Param_Curve curve:
            var curves = PromptEdge(doc, input.NickName);
            if (curves == null)
              return Result.Cancelled;
            values.Add(input, curves);
            break;
          case Param_Surface surface:
            var surfaces = PromptSurface(doc, input.NickName);
            if (surfaces == null)
              return Result.Cancelled;
            values.Add(input, surfaces);
            break;
          case Param_Brep brep:
            var breps = PromptBrep(doc, input.NickName);
            if (breps == null)
              return Result.Cancelled;
            values.Add(input, breps);
            break;
        }
      }

      return Result.Succeeded;
    }

    const ObjectSnapTypes DefaultSnapTypes =
      ObjectSnapTypes.Endpoints |
      ObjectSnapTypes.Midpoints |
      ObjectSnapTypes.Nearest |
      ObjectSnapTypes.WorkPlaneGrid |
      //ObjectSnapTypes.Intersections |
      ObjectSnapTypes.Centers |
      ObjectSnapTypes.Perpendicular |
      ObjectSnapTypes.Tangents |
      ObjectSnapTypes.Quadrants |
      ObjectSnapTypes.Points;

    internal static bool PickPointOnFace(UIDocument doc, string prompt, out XYZ point, ObjectSnapTypes snapSettings = DefaultSnapTypes)
    {
      point = null;

      if (doc.ActiveView.ViewType != ViewType.ThreeD)
      {
        try { point = doc.Selection.PickPoint(snapSettings, prompt + "Please pick a point on the current work plane"); }
        catch (OperationCanceledException) { }
      }
      else
      {
        var reference = doc.Selection.PickObject(ObjectType.Face, prompt + "Please select a face to define a work plane");
        if (doc.Document.GetElement(reference.ElementId) is Element element)
        {
          if (element.GetGeometryObjectFromReference(reference) is Face face)
          {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
              point = face.Evaluate(reference.UVPoint);
            }
            else
            {
              var plane = Plane.CreateByNormalAndOrigin(face.ComputeNormal(reference.UVPoint), face.Evaluate(reference.UVPoint));

              using (var transaction = new Transaction(doc.Document))
              {
                transaction.Start("PickPointOnFace");

                doc.ActiveView.SketchPlane = SketchPlane.Create(doc.Document, plane);
                doc.ActiveView.ShowActiveWorkPlane();

                try { point = doc.Selection.PickPoint(snapSettings, prompt + "Please pick a point on the defined work plane"); }
                catch (OperationCanceledException) { }

                transaction.RollBack();
              }
            }
          }
        }
      }

      return null != point;
    }

    internal static bool PickPoint(UIDocument doc, string prompt, out XYZ point, ObjectSnapTypes snapSettings = DefaultSnapTypes)
    {
      point = null;

      View view;
      do
      {
        view = doc.ActiveView;
        try { point = doc.Selection.PickPoint(snapSettings, prompt + "Please pick a point on the current work plane"); }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
      }
      while (doc.ActiveView.Id != view.Id);

      return null != point;
    }

    internal static IEnumerable<IGH_Goo> PromptPoint(UIDocument doc, string prompt)
    {
      IGH_Goo goo = null;

      if (PickPoint(doc, prompt + " : ", out var point))
        goo = new GH_Point(point.ToPoint3d());

      yield return goo;
    }

    internal static IEnumerable<IGH_Goo> PromptLine(UIDocument doc, string prompt)
    {
      IGH_Goo goo = null;

      if
      (
        PickPoint(doc, prompt + " : Start point - ", out var from) &&
        PickPoint(doc, prompt + " : End pont - ", out var to)
      )
      {
        goo = new GH_Line(new Rhino.Geometry.Line(from.ToPoint3d(), to.ToPoint3d()));
      }

      yield return goo;
    }

    internal static IEnumerable<IGH_Goo> PromptBox(UIDocument doc, string prompt)
    {
      IGH_Goo goo = null;

      if
      (
        PickPointOnFace(doc, prompt + " : First box corner - ", out var from) &&
        PickPointOnFace(doc, prompt + " : Second box corner - ", out var to)
      )
      {
        var min = new XYZ(Math.Min(from.X, to.X), Math.Min(from.Y, to.Y), Math.Min(from.Z, to.Z));
        var max = new XYZ(Math.Max(from.X, to.X), Math.Max(from.Y, to.Y), Math.Max(from.Z, to.Z));

        goo = new GH_Box(new Rhino.Geometry.BoundingBox(min.ToPoint3d(), max.ToPoint3d()));
      }

      yield return goo;
    }

    internal static IEnumerable<IGH_Goo> PromptEdge(UIDocument doc, string prompt)
    {
      IGH_Goo goo = null;

      try
      {
        var reference = doc.Selection.PickObject(ObjectType.Edge, prompt);
        if (reference != null)
        {
          var element = doc.Document.GetElement(reference);
          var edge = element.GetGeometryObjectFromReference(reference) as Edge;
          var curve = edge.AsCurve().ToCurve();
          goo = new GH_Curve(curve);
        }
      }
      catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }

      yield return goo;
    }

    internal static IEnumerable<IGH_Goo> PromptSurface(UIDocument doc, string prompt)
    {
      try
      {
        var reference = doc.Selection.PickObject(ObjectType.Face, prompt);
        if (reference != null)
        {
          var element = doc.Document.GetElement(reference);
          var face = element.GetGeometryObjectFromReference(reference) as Face;
          var surface = face.ToBrep();
          return new GH_Surface[] { new GH_Surface(surface) };
        }
      }
      catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }

      return null;
    }

    internal static IEnumerable<IGH_Goo> PromptBrep(UIDocument doc, string prompt)
    {
      try
      {
        var reference = doc.Selection.PickObject(ObjectType.Element, prompt);
        if (reference != null)
        {
          var element = doc.Document.GetElement(reference);

          using (var options = new Options() { DetailLevel = ViewDetailLevel.Fine })
          using (var geometry = element.GetGeometry(options))
            return geometry.ToGeometryBaseMany().OfType<Rhino.Geometry.Brep>().Select((x) => new GH_Brep(x));
        }
      }
      catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }

      return null;
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      Result result;
      if ((result = BrowseForFile(out var filePath)) == Result.Succeeded)
      {
        result = Execute(data.Application, data.View, data.JournalData, filePath, ref message);
      }

      return result;
    }

    public static Result Execute
    (
      UIApplication app,
      View view,
      IDictionary<string, string> journalData,
      string filePath,
      ref string message
    )
    {
      var (res, msg) = External.ActivationGate.Open(() =>
      {
        var result = ReadFromFile(filePath, out var definition);
        if (result == Result.Succeeded)
        {
          using (definition)
          {
            bool enableSolutions = GH_Document.EnableSolutions;
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
              using (var transGroup = new TransactionGroup(app.ActiveUIDocument.Document))
              {
                transGroup.Start(Path.GetFileNameWithoutExtension(definition.Properties.ProjectFileName));

                GH_Document.EnableSolutions = true;
                definition.Enabled = true;
                definition.ExpireSolution();

                var inputs = GetInputParams(definition);
                result = PromptForInputs(app.ActiveUIDocument, inputs, out var values);
                if (result != Result.Succeeded)
                  return (result, default);

                // Update input volatile data values
                foreach (var value in values)
                  value.Key.AddVolatileDataList(new Grasshopper.Kernel.Data.GH_Path(0), value.Value);

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                using (var modal = new ModalScope())
                {
                  definition.NewSolution(false, GH_SolutionMode.Silent);

                  do
                  {
                    if (modal.Run(false, false) == Result.Failed)
                      return (Result.Failed, default);

                  } while (definition.ScheduleDelay >= GH_Document.ScheduleRecursive);
                }
                Thread.CurrentThread.CurrentCulture = currentCulture;

                if (definition.SolutionState == GH_ProcessStep.Aborted)
                {
                  return (Result.Cancelled, $"Solution aborted by user after ~{ definition.SolutionSpan.TotalSeconds} seconds");
                }

                transGroup.Assimilate();
              }
            }
            catch (Exception e)
            {
              return (Result.Failed, e.Message);
            }
            finally
            {
              Thread.CurrentThread.CurrentCulture = currentCulture;
              GH_Document.EnableSolutions = enableSolutions;
            }
          }
        }

        return (result, default);
      }, default);

      message = msg;
      return res;
    }
  }

  /// <summary>
  /// Represents a Pseudo-modal loop.
  /// </summary>
  /// <remarks>
  /// This class implements <see cref="IDisposable"/> interface, it's been designed to be used in a using statement.
  /// </remarks>
  sealed class ModalScope : IDisposable
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
        if (exposeMainWindow) Rhinoceros.Exposed = true;
        else if (restorePopups) Rhinoceros.Exposed = wasExposed || Rhinoceros.MainWindow.WindowStyle == ProcessWindowStyle.Minimized;

        if (restorePopups)
          Rhinoceros.MainWindow.ShowOwnedPopups();

        // Activate a Rhino window to keep the loop running
        var activePopup = Rhinoceros.MainWindow.ActivePopup;
        if (activePopup.IsInvalid || exposeMainWindow)
        {
          if (!Rhinoceros.Exposed)
            return Result.Cancelled;

          Rhino.RhinoApp.SetFocusToMainWindow();
        }
        else
        {
          activePopup.BringToFront();
        }

        while (Rhinoceros.Run())
        {
          if (!Rhinoceros.Exposed && Rhinoceros.MainWindow.ActivePopup.IsInvalid)
            break;
        }

        return Result.Succeeded;
      }
      finally
      {
        wasExposed = Rhinoceros.Exposed;

        Revit.MainWindow.Enabled = true;
        WindowHandle.ActiveWindow = Revit.MainWindow;
        Rhinoceros.MainWindow.HideOwnedPopups();
        Rhinoceros.Exposed = false;
      }
    }
  }
}
