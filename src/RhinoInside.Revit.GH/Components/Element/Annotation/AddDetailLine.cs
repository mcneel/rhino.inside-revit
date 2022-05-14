using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotation
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
          Description = "View to add a specific detail line",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Curve",
          NickName = "C",
          Description = "Curve to create a specific detail line",
          Access = GH_ParamAccess.item
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
          Name = _Output_,
          NickName = _Output_.Substring(0, 1),
          Description = $"Output {_Output_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _Output_ = "Detail Line";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out ARDB.View view)) return;

      ReconstructElement<ARDB.DetailCurve>
      (
        view.Document, _Output_, (detailCurve) =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;

          var tol = GeometryObjectTolerance.Model;
          if
          (
            curve.IsShort(tol.ShortCurveTolerance) ||
            curve.IsClosed ||
            !curve.TryGetPlane(out var plane, tol.VertexTolerance) ||
            plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be a valid horizontal, planar and open curve.", curve);

          // Compute
          detailCurve = Reconstruct(detailCurve, view, curve);

          DA.SetData(_Output_, detailCurve);
          return detailCurve;
        }
      );
    }

    bool Reuse(ARDB.DetailCurve detailCurve, ARDB.View view, Curve curve)
    {
      if (detailCurve is null) return false;

      var genLevel = view.GenLevel;
      if (detailCurve.OwnerViewId != view.Id) return false;

      var levelPlane = Plane.WorldXY;
      levelPlane.Translate(Vector3d.ZAxis * genLevel.GetElevation() * Revit.ModelUnits);

      using (var projectedCurve = Curve.ProjectToPlane(curve, levelPlane).ToCurve())
      {
        if (!projectedCurve.IsAlmostEqualTo(detailCurve.GeometryCurve))
          detailCurve.SetGeometryCurve(projectedCurve, overrideJoins: true);
      }

      return true;
    }

    ARDB.DetailCurve Create(ARDB.View view, Curve curve)
    {
      if (view.GenLevel is ARDB.Level level)
      {
        var sketchPlane = level.GetSketchPlane(ensureSketchPlane: true);
        using (var projectedCurve = Curve.ProjectToPlane(curve, sketchPlane.GetPlane().ToPlane()))
          return view.Document.Create.NewDetailCurve(view, projectedCurve.ToCurve());
      }

      return default;
    }

    ARDB.DetailCurve Reconstruct(ARDB.DetailCurve detailCurve, ARDB.View view, Curve curve)
    {
      if (!Reuse(detailCurve, view, curve))
        detailCurve = Create(view, curve);

      return detailCurve;
    }
  }
}
