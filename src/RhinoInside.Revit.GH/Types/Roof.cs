using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Roof")]
  public class Roof : HostObject
  {
    protected override Type ValueType => typeof(DB.RoofBase);
    public static explicit operator DB.RoofBase(Roof value) => value?.Value;
    public new DB.RoofBase Value => base.Value as DB.RoofBase;

    public Roof() { }
    public Roof(DB.RoofBase roof) : base(roof) { }

    public override Level Level
    {
      get
      {
        switch (Value)
        {
          case DB.ExtrusionRoof extrusionRoof:
            return new Level(extrusionRoof.Document, extrusionRoof.get_Parameter(DB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM).AsElementId());
        }

        return base.Level;
      }
    }

    public override Plane Location
    {
      get
      {
        if(Value is DB.RoofBase roof && !(roof.Location is DB.LocationPoint) && !(roof.Location is DB.LocationCurve))
        {
          if (roof.GetSketch() is DB.Sketch sketch)
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
            var origin = new Point3d(center.X, center.Y, Level.Height + levelOffset);
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
