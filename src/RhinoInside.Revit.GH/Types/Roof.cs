using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Roof")]
  public class Roof : HostObject, ICurtainGridsAccess
  {
    protected override Type ValueType => typeof(ARDB.RoofBase);
    public new ARDB.RoofBase Value => base.Value as ARDB.RoofBase;

    public Roof() { }
    public Roof(ARDB.RoofBase roof) : base(roof) { }

    public override Level Level
    {
      get
      {
        switch (Value)
        {
          case ARDB.ExtrusionRoof extrusionRoof:
            return new Level(extrusionRoof.Document, extrusionRoof.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM).AsElementId());
        }

        return base.Level;
      }
    }

    public override Plane Location
    {
      get
      {
        if(Value is ARDB.RoofBase roof && !(roof.Location is ARDB.LocationPoint) && !(roof.Location is ARDB.LocationCurve))
        {
          if (roof.GetSketch() is ARDB.Sketch sketch)
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

            var levelOffset = 0.0;
            switch (roof)
            {
              case ARDB.FootPrintRoof footPrintRoof:
                levelOffset = footPrintRoof.get_Parameter(ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
                break;

              case ARDB.ExtrusionRoof extrusionRoof:
                levelOffset = extrusionRoof.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
                break;
            }

            var plane = sketch.SketchPlane.GetPlane().ToPlane();
            var origin = new Point3d(center.X, center.Y, Level.Height + levelOffset);
            var xAxis = plane.XAxis;
            var yAxis = plane.YAxis;

            if (roof is ARDB.ExtrusionRoof)
              yAxis = plane.ZAxis;

            return new Plane(origin, xAxis, yAxis);
          }
        }

        return base.Location;
      }
    }

    #region IGH_CurtainGridsAccess
    public IList<CurtainGrid> CurtainGrids
    {
      get
      {
        switch (Value)
        {
          case ARDB.ExtrusionRoof extrusionRoof:
            return extrusionRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>().
              Select(x => new CurtainGrid(extrusionRoof, x)).ToArray();

          case ARDB.FootPrintRoof footPrintRoof:
            return footPrintRoof.CurtainGrids?.Cast<ARDB.CurtainGrid>().
              Select(x => new CurtainGrid(footPrintRoof, x)).ToArray();
        }

        return default;
      }
    }
    #endregion
  }
}
