using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Curtain Grid Line")]
  public class CurtainGridLine : HostObject
  {
    protected override Type ScriptVariableType => typeof(DB.CurtainGridLine);
    public new DB.CurtainGridLine Value => base.Value as DB.CurtainGridLine;
    public static explicit operator DB.CurtainGridLine(CurtainGridLine value) => value?.Value;

    public CurtainGridLine() { }
    public CurtainGridLine(DB.CurtainGridLine gridLine) : base(gridLine) { }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is DB.CurtainGridLine gridLine)
      {
        var points = gridLine.FullCurve?.Tessellate();
        if (points is object)
          args.Pipeline.DrawPatternedPolyline(points.Convert(GeometryDecoder.ToPoint3d), args.Color, 0x00000101, args.Thickness, false);

        foreach (var segment in gridLine.ExistingSegmentCurves.ToCurves())
        {
          if(segment is object)
            args.Pipeline.DrawCurve(segment, args.Color, args.Thickness);
        }
      }
    }
    #endregion

    #region Properties
    public override Plane Location
    {
      get
      {
        if (Value is DB.CurtainGridLine gridLine && gridLine ?.FullCurve is DB.Curve curve)
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

    public override Curve Curve => Value?.FullCurve.ToCurve();
    #endregion
  }
}
