using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Building Pad")]
  public class BuildingPad : HostObject
  {
    protected override Type ValueType => typeof(ARDB.Architecture.BuildingPad);
    public static explicit operator ARDB.Architecture.BuildingPad(BuildingPad value) => value?.Value;
    public new ARDB.Architecture.BuildingPad Value => base.Value as ARDB.Architecture.BuildingPad;

    public BuildingPad() { }
    public BuildingPad(ARDB.Architecture.BuildingPad floor) : base(floor) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Architecture.BuildingPad pad && pad.GetSketch() is ARDB.Sketch sketch)
        {
          var center = ARDB.XYZ.Zero;
          var count = 0;
          foreach (var curveArray in sketch.Profile.Cast<ARDB.CurveArray>())
          {
            foreach (var curve in curveArray.Cast<ARDB.Curve>())
            {
              count++;
              center += curve.Evaluate(0.0, normalized: true);
              count++;
              center += curve.Evaluate(1.0, normalized: true);
            }
          }
          center /= count;

          if (pad.Document.GetElement(pad.LevelId) is ARDB.Level level)
            center = new ARDB.XYZ(center.X, center.Y, level.GetHeight());

          if (pad.get_Parameter(ARDB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM) is ARDB.Parameter heightAboveLevel)
            center = new ARDB.XYZ(center.X, center.Y, center.Z + heightAboveLevel.AsDouble());

          var plane = sketch.SketchPlane.GetPlane().ToPlane();
          var origin = center.ToPoint3d();
          var xAxis = plane.XAxis;
          var yAxis = plane.YAxis;

          return new Plane(origin, xAxis, yAxis);
        }

        return base.Location;
      }
    }
  }
}
