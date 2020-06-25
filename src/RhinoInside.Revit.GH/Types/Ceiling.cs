using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Ceiling : HostObject
  {
    public override string TypeDescription => "Represents a Revit ceiling element";
    protected override Type ScriptVariableType => typeof(DB.Ceiling);
    public static explicit operator DB.Ceiling(Ceiling self) =>
      self.Document?.GetElement(self) as DB.Ceiling;

    public Ceiling() { }
    public Ceiling(DB.Ceiling ceiling) : base(ceiling) { }

    public override Plane Location
    {
      get
      {
        var floor = (DB.Ceiling) this;

        if (floor.GetFirstDependent<DB.Sketch>() is DB.Sketch sketch)
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
            center.Z = level.Elevation * Revit.ModelUnits;

          center.Z += floor.get_Parameter(DB.BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)?.AsDoubleInRhinoUnits() ?? 0.0;

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
