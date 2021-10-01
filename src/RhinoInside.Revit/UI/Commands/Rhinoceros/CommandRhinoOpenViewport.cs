using System;
using System.Windows.Input;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RhinoInside.Revit.Convert.DocObjects;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.UI
{
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
        tooltip: "Opens a floating viewport",
        url: "reference/rir-interface#rhino-options"
      );

      if (ribbonPanel.AddItem(buttonData) is PushButton pushButton)
      {
        pushButton.LongDescription = $"Use CTRL key to open the viewport without synchronizing camera";
        StoreButton(CommandName, pushButton);
      }
    }

    public override Result Execute(ExternalCommandData data, ref string message, DB.ElementSet elements)
    {
      var ctrlIsPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

      if (!ctrlIsPressed && data.View.TryGetViewportInfo(useUIView: true, out var vport))
      {
        var rhinoDoc = Rhino.RhinoDoc.ActiveDoc;
        bool imperial = rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Feet || rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Inches;
        var spacing = imperial ?
        UnitConverter.Convert(1.0, Rhino.UnitSystem.Yards, rhinoDoc.ModelUnitSystem) :
        UnitConverter.Convert(1.0, Rhino.UnitSystem.Meters, rhinoDoc.ModelUnitSystem);

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
          surface is DB.Plane plane
        )
        {
          cplane.Name = name;
          cplane.Plane = plane.ToPlane();
          cplane.GridSpacing = spacing * Revit.ModelUnits;
          cplane.SnapSpacing = spacing * Revit.ModelUnits;
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

        Rhinoceros.RunCommandOpenViewportAsync(vport, cplane);
      }
      else
        Rhinoceros.RunCommandOpenViewportAsync(default, default);

      return Result.Succeeded;
    }
  }
}
