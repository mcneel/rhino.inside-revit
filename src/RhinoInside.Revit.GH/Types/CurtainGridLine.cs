using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.Geometry.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class CurtainGridLine : HostObject
  {
    public override string TypeDescription => "Represents a Revit curtain grid line Element";
    protected override Type ScriptVariableType => typeof(DB.CurtainGridLine);
    public static explicit operator DB.CurtainGridLine(CurtainGridLine self) =>
      self.Document?.GetElement(self) as DB.CurtainGridLine;

    public CurtainGridLine() { }
    public CurtainGridLine(DB.CurtainGridLine gridLine) : base(gridLine) { }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var bbox = Boundingbox;
      if (!bbox.IsValid)
        return;

      var gridLine = (DB.CurtainGridLine) this;
      var points = gridLine?.FullCurve?.Tessellate();
      if (points is object)
        args.Pipeline.DrawPatternedPolyline(points.Convert(GeometryDecoder.ToPoint3d), args.Color, 0x00003333, args.Thickness, false);
    }
    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion


    public override Plane Location
    {
      get
      {
        var gridLine = (DB.CurtainGridLine) this;

        if (gridLine?.FullCurve is DB.Curve curve)
        {
          var start = curve.Evaluate(0.0, normalized: true).ToPoint3d();
          var end = curve.Evaluate(1.0, normalized: true).ToPoint3d();
          var axis = end - start;
          var origin = start + (axis * 0.5);
          var perp = axis.PerpVector();
          return new Plane(origin, axis, perp);
        }

        return base.Location;
      }
    }

    public override Curve Curve
    {
      get
      {
        var gridLine = (DB.CurtainGridLine) this;
        var axisCurve = gridLine?.FullCurve?.ToCurve();

        return axisCurve;
      }
    }
  }
}
