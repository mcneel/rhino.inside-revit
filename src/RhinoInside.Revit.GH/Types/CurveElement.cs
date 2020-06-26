using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  class CurveElement : GraphicalElement
  {
    public override string TypeName => "Revit Curve element";

    public override string TypeDescription => "Represents a Revit Curve Element";
    protected override Type ScriptVariableType => typeof(DB.CurveElement);
    public static explicit operator DB.CurveElement(CurveElement value) =>
      value.IsValid ? value.Document?.GetElement(value) as DB.CurveElement : default;

    public CurveElement() { }
    public CurveElement(DB.CurveElement value) : base(value) { }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var curveElement = (DB.CurveElement) this;
      if (curveElement is object)
      {
        var curve = curveElement.GeometryCurve.ToCurve();
        args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
      }
    }

    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    public override Curve Curve
    {
      get
      {
        var curveElement = (DB.CurveElement) this;
        return curveElement?.GeometryCurve?.ToCurve();
      }
    }
  }
}
