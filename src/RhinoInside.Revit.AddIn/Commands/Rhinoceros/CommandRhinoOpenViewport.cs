using System;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.AddIn.Commands
{
  using Convert.DocObjects;
  using Convert.Geometry;
  using Convert.Units;
  using External.DB.Extensions;

  [Transaction(TransactionMode.Manual), Regeneration(RegenerationOption.Manual)]
  class CommandRhinoOpenViewport : RhinoCommand
  {
    public static string CommandName => "Open\nViewport";

    public static void CreateUI(RibbonPanel ribbonPanel)
    {
      var buttonData = NewPushButtonData<CommandRhinoOpenViewport, Availability>
      (
        name: CommandName,
        iconName: "Ribbon.Rhinoceros.OpenViewport.png",
        tooltip: "Opens a floating viewport.",
        url: "reference/rir-interface#rhinoceros-panel"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.LongDescription =
          $"Use CTRL key to open the viewport synchronizing camera and workplane.{Environment.NewLine}" +
          "Use CTRL + SHIFT to also synchronize Zoom level.";
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var ctrlIsPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
      var shiftIsPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

      if (ctrlIsPressed && data.View.TryGetViewportInfo(useUIView: shiftIsPressed, out var vport))
      {
        var rhinoDoc = Rhino.RhinoDoc.ActiveDoc;
        var modelScale = UnitScale.GetModelScale(rhinoDoc);
        bool imperial = Rhino.Geometry.UnitSystemExtension.IsImperial(rhinoDoc.ModelUnitSystem);
        var spacing = imperial ?
        UnitScale.Convert(1.0, UnitScale.Yards, modelScale) :
        UnitScale.Convert(1.0, UnitScale.Meters, modelScale);

        var cplane = new Rhino.DocObjects.ConstructionPlane()
        {
          Plane = (data.View.SketchPlane?.GetPlane().ToPlane()) ?? vport.FrustumNearPlane,
          GridSpacing = spacing,
          SnapSpacing = spacing,
          GridLineCount = 70,
          ThickLineFrequency = imperial ? 6 : 5,
          DepthBuffered = true,
          Name = data.View.Name,
        };

        if
        (
          data.View.TryGetSketchGridSurface(out var name, out var surface, out var bboxUV, out spacing) &&
          surface is Plane plane
        )
        {
          cplane.Name = name;
          cplane.Plane = plane.ToPlane();
          cplane.GridSpacing = UnitScale.Convert(spacing, UnitScale.Internal, modelScale);
          cplane.SnapSpacing = UnitScale.Convert(spacing, UnitScale.Internal, modelScale);
          var min = bboxUV.Min.ToPoint2d();
          min.X = Math.Round(min.X / cplane.GridSpacing) * cplane.GridSpacing;
          min.Y = Math.Round(min.Y / cplane.GridSpacing) * cplane.GridSpacing;
          var max = bboxUV.Max.ToPoint2d();
          max.X = Math.Round(max.X / cplane.GridSpacing) * cplane.GridSpacing;
          max.Y = Math.Round(max.Y / cplane.GridSpacing) * cplane.GridSpacing;
          var gridUCount = Math.Max(1, (int) Math.Round((max.X - min.X) / cplane.GridSpacing * 0.5));
          var gridVCount = Math.Max(1, (int) Math.Round((max.Y - min.Y) / cplane.GridSpacing * 0.5));
          cplane.GridLineCount = Math.Max(gridUCount, gridVCount);
          cplane.Plane = new Rhino.Geometry.Plane
          (
            cplane.Plane.PointAt
            (
              min.X + gridUCount * cplane.GridSpacing,
              min.Y + gridVCount * cplane.GridSpacing
            ),
            cplane.Plane.XAxis, cplane.Plane.YAxis
          );
          cplane.ShowAxes = false;
          cplane.ShowZAxis = false;
        }

        // Make screen port a bit smaller than Revit one.
        {
          var port = vport.ScreenPort;
          port.Width /= 3; port.Height /= 3;
          vport.ScreenPort = port;
        }

        if (!shiftIsPressed && vport.IsParallelProjection)
        {
          vport.DollyExtents
          (
            new Rhino.Geometry.BoundingBox
            (
              new Rhino.Geometry.Point3d(vport.FrustumLeft, vport.FrustumBottom, vport.FrustumNear),
              new Rhino.Geometry.Point3d(vport.FrustumRight, vport.FrustumTop, vport.FrustumFar)
            ), 1.1
          );
        }

        Rhinoceros.RunCommandOpenViewportAsync(vport, cplane, setScreenPort: true);
      }
      else Rhinoceros.RunCommandOpenViewportAsync(default, default, setScreenPort: false);

      return Result.Succeeded;
    }
  }
}
