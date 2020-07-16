using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Roof : HostObject
  {
    public override string TypeDescription => "Represents a Revit roof element";
    protected override Type ScriptVariableType => typeof(DB.RoofBase);
    public static explicit operator DB.RoofBase(Roof value) =>
      value?.IsValid == true ? value.Document.GetElement(value) as DB.RoofBase : default;

    public Roof() { }
    public Roof(DB.RoofBase roof) : base(roof) { }

    public override Level Level
    {
      get
      {
        switch ((DB.RoofBase) this)
        {
          case DB.ExtrusionRoof extrusionRoof:
            return Level.FromElement(extrusionRoof.Document.GetElement(extrusionRoof.get_Parameter(DB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM).AsElementId())) as Level;
        }

        return base.Level;
      }
    }

    public override Plane Location
    {
      get
      {
        var roof = (DB.RoofBase) this;

        if (!(roof.Location is DB.LocationPoint) && !(roof.Location is DB.LocationCurve))
        {
          if (roof.GetFirstDependent<DB.Sketch>() is DB.Sketch sketch)
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

            var levelOffset = 0.0;
            switch (roof)
            {
              case DB.FootPrintRoof footPrintRoof:
                levelOffset = footPrintRoof.get_Parameter(DB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
                break;

              case DB.ExtrusionRoof extrusionRoof:
                levelOffset = extrusionRoof.get_Parameter(DB.BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
                break;
            }

            var plane = sketch.SketchPlane.GetPlane().ToPlane();
            var origin = new Point3d(center.X, center.Y, Level.Elevation + levelOffset);
            var xAxis = plane.XAxis;
            var yAxis = plane.YAxis;

            if (roof is DB.ExtrusionRoof)
              yAxis = plane.ZAxis;

            return new Plane(origin, xAxis, yAxis);
          }
        }

        return base.Location;
      }
    }
  }
}
