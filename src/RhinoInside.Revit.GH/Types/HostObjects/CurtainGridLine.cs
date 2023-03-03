using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Curtain Grid Line")]
  public class CurtainGridLine : HostObject
  {
    protected override Type ValueType => typeof(ARDB.CurtainGridLine);
    public new ARDB.CurtainGridLine Value => base.Value as ARDB.CurtainGridLine;
    public static explicit operator ARDB.CurtainGridLine(CurtainGridLine value) => value?.Value;

    public CurtainGridLine() { }
    public CurtainGridLine(ARDB.CurtainGridLine gridLine) : base(gridLine) { }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.CurtainGridLine gridLine)
      {
        foreach (ARDB.Curve segment in gridLine.ExistingSegmentCurves)
        {
          if (segment is object)
            args.Pipeline.DrawCurve(segment.ToCurve(), args.Color, args.Thickness);
        }

        foreach (ARDB.Curve segment in gridLine.SkippedSegmentCurves)
        {
          if (segment is object)
            args.Pipeline.DrawPatternedPolyline(segment.Tessellate().Select(GeometryDecoder.ToPoint3d), args.Color, 0x00000101, args.Thickness, false);
        }
      }
    }

    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    #region Properties
    public override Plane Location
    {
      get
      {
        if (Curve is Curve curve)
        {
          var start = curve.PointAtStart;
          var end = curve.PointAtEnd;
          var axis = end - start;
          var origin = start + (axis * 0.5);
          var perp = axis.RightDirection(GeometryDecoder.Tolerance.DefaultTolerance);
          return new Plane(origin, axis, perp);
        }

        return NaN.Plane;
      }
    }

    public override Curve Curve => Value?.AllSegmentCurves is ARDB.CurveArray segments ? Curve.JoinCurves(segments.ToCurveMany())[0] : default;
    #endregion
  }
}
