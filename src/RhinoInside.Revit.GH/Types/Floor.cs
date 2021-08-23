using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Floor")]
  public class Floor : HostObject
  {
    protected override Type ScriptVariableType => typeof(DB.Floor);
    public static explicit operator DB.Floor(Floor value) => value?.Value;
    public new DB.Floor Value => base.Value as DB.Floor;

    public Floor() { }
    public Floor(DB.Floor floor) : base(floor) { }

    public override Plane Location
    {
      get
      {
        if (Value is DB.Floor floor && floor.GetSketch() is DB.Sketch sketch)
        {
          var center = Point3d.Origin;
          var count = 0;
          foreach (var curveArray in sketch.Profile.Cast<DB.CurveArray>())
          {
            foreach (var curve in curveArray.Cast<DB.Curve>())
            {
              count++;
              center += curve.Evaluate(0.0, normalized: true).ToPoint3d();
              count++;
              center += curve.Evaluate(1.0, normalized: true).ToPoint3d();
            }
          }
          center /= count;

          if (floor.Document.GetElement(floor.LevelId) is DB.Level level)
            center.Z = level.GetHeight() * Revit.ModelUnits;

          center.Z += Revit.ModelUnits * floor.get_Parameter(DB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;

          var plane = sketch.SketchPlane.GetPlane().ToPlane();
          var origin = center;
          var xAxis = plane.XAxis;
          var yAxis = plane.YAxis;

          return new Plane(origin, xAxis, yAxis);
        }

        return base.Location;
      }
    }
  }
}
