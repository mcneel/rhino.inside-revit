using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;

  [Kernel.Attributes.Name("Curtain Grid Line")]
  public class CurtainGridLine : HostObject
  {
    protected override Type ValueType => typeof(ARDB.CurtainGridLine);
    public new ARDB.CurtainGridLine Value => base.Value as ARDB.CurtainGridLine;
    public static explicit operator ARDB.CurtainGridLine(CurtainGridLine value) => value?.Value;

    public CurtainGridLine() { }
    public CurtainGridLine(ARDB.CurtainGridLine gridLine) : base(gridLine) { }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.CurtainGridLine gridLine)
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
        if (Value is ARDB.CurtainGridLine gridLine && gridLine ?.FullCurve is ARDB.Curve curve)
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
