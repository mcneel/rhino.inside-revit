using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  [ComponentVersion(introduced: "1.8")]
  public class AddDetailLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("5A94EA62-3C27-4E48-B885-C98218264981");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddDetailLine() : base
    (
      name: "Add Detail Line",
      nickname: "DetailLine",
      description: "Given a Curve, it adds a detail line to the given View",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to add a specific detail line"
        }
      ),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Curve",
          NickName = "C",
          Description = "Curve to create a specific detail line"
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = _DetailLine_,
          NickName = _DetailLine_.Substring(0, 1),
          Description = $"Output {_DetailLine_}"
        }
      )
    };

    const string _DetailLine_ = "Detail Line";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.DetailCurve>
      (
        view.Document, _DetailLine_, detailCurve =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;

          var viewPlane = new Plane(view.Origin.ToPoint3d(), view.ViewDirection.ToVector3d());
          var tol = GeometryTolerance.Model;

          if (curve.IsShort(tol.ShortCurveTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve.IsClosed(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (!curve.IsParallelToPlane(viewPlane, tol.VertexTolerance, tol.AngleTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be planar and parallel to view plane.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          if ((curve = Curve.ProjectToPlane(curve, viewPlane)) is null)
            throw new Exceptions.RuntimeArgumentException("Curve", "Failed to project Curve into view plane", curve);

          if (curve.GetNextDiscontinuity(Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, Math.Cos(tol.AngleTolerance), Rhino.RhinoMath.SqrtEpsilon, out var _))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be C1 continuous.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          // Compute
          detailCurve = Reconstruct(detailCurve, view, curve.ToCurve());

          DA.SetData(_DetailLine_, detailCurve);
          return detailCurve;
        }
      );
    }

    bool Reuse(ARDB.DetailCurve detailCurve, ARDB.View view, ARDB.Curve curve)
    {
      if (detailCurve is null) return false;
      if (detailCurve.OwnerViewId != view.Id) return false;

      if (!curve.IsSameKindAs(detailCurve.GeometryCurve)) return false;
      if (!curve.AlmostEquals(detailCurve.GeometryCurve, detailCurve.Document.Application.VertexTolerance))
        detailCurve.SetGeometryCurve(curve, overrideJoins: true);

      return true;
    }

    ARDB.DetailCurve Create(ARDB.View view, ARDB.Curve curve)
    {
      if (view.Document.IsFamilyDocument)
        return view.Document.FamilyCreate.NewDetailCurve(view, curve);
      else
        return view.Document.Create.NewDetailCurve(view, curve);
    }

    ARDB.DetailCurve Reconstruct(ARDB.DetailCurve detailCurve, ARDB.View view, ARDB.Curve curve)
    {
      if (!Reuse(detailCurve, view, curve))
        detailCurve = Create(view, curve);

      return detailCurve;
    }
  }
}
