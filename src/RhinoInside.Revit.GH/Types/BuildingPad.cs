using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class BuildingPad : HostObject
  {
    public override string TypeDescription => "Represents a Revit building pad element";
    protected override Type ScriptVariableType => typeof(DB.Architecture.BuildingPad);
    public static explicit operator DB.Architecture.BuildingPad(BuildingPad self) =>
      self.Document?.GetElement(self) as DB.Architecture.BuildingPad;

    public BuildingPad() { }
    public BuildingPad(DB.Architecture.BuildingPad floor) : base(floor) { }

    public override Plane Location
    {
      get
      {
        var pad = (DB.Architecture.BuildingPad) this;

        if (pad.GetFirstDependent<DB.Sketch>() is DB.Sketch sketch)
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

          if (pad.Document.GetElement(pad.LevelId) is DB.Level level)
            center.Z = level.Elevation * Revit.ModelUnits;

          center.Z += pad.get_Parameter(DB.BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM)?.AsDoubleInRhinoUnits() ?? 0.0;

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
