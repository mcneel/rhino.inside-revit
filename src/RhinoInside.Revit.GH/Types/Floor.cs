using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Floor")]
  public class Floor : HostObject
  {
    protected override Type ValueType => typeof(ARDB.Floor);
    public static explicit operator ARDB.Floor(Floor value) => value?.Value;
    public new ARDB.Floor Value => base.Value as ARDB.Floor;

    public Floor() { }
    public Floor(ARDB.Floor floor) : base(floor) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Floor floor && floor.GetSketch() is ARDB.Sketch sketch)
        {
          var center = Point3d.Origin;
          var count = 0;
          foreach (var curveArray in sketch.Profile.Cast<ARDB.CurveArray>())
          {
            foreach (var curve in curveArray.Cast<ARDB.Curve>())
            {
              count++;
              center += curve.Evaluate(0.0, normalized: true).ToPoint3d();
              count++;
              center += curve.Evaluate(1.0, normalized: true).ToPoint3d();
            }
          }
          center /= count;

          if (floor.Document.GetElement(floor.LevelId) is ARDB.Level level)
            center.Z = level.GetHeight() * Revit.ModelUnits;

          center.Z += Revit.ModelUnits * floor.get_Parameter(ARDB.BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;

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
