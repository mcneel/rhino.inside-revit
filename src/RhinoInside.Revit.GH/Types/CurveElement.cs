using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  class CurveElement : GraphicalElement
  {
    public override string TypeName => "Revit Curve element";

    public override string TypeDescription => "Represents a Revit Curve Element";
    protected override Type ScriptVariableType => typeof(DB.CurveElement);
    public static explicit operator DB.CurveElement(CurveElement value) => value?.Value;
    public new DB.CurveElement Value => base.Value as DB.CurveElement;

    public CurveElement() { }
    public CurveElement(DB.CurveElement value) : base(value) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Curve is Curve curve)
        return curve.GetBoundingBox(xform);

      return base.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Curve is Curve curve)
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
    }

    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    public override Curve Curve => Value?.GeometryCurve.ToCurve();
  }
}
