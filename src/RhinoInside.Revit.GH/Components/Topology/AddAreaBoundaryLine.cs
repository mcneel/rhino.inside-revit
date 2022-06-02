using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Topology
{
  [ComponentVersion(introduced: "1.7")]
  public class AddAreaBoundaryLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("34D68CDC-892B-4525-959D-49C0AC66317E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddAreaBoundaryLine() : base
    (
      name: "Add Area Boundary",
      nickname: "AreaBoundary",
      description: "Given a Curve, it adds an Area boundary line to the given Area Plan",
      category: "Revit",
      subCategory: "Topology"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AreaPlan()
        {
          Name = "Area Plan",
          NickName = "AP",
          Description = "Area Plan to add a specific area boundary line",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Curve
        {
          Name = "Curve",
          NickName = "C",
          Description = "Curve to create a specific area boundary line",
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
          Name = _AreaBoundary_,
          NickName = _AreaBoundary_.Substring(0, 1),
          Description = $"Output {_AreaBoundary_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _AreaBoundary_ = "Area Boundary";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Area Plan", out ARDB.ViewPlan viewPlan)) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        viewPlan.Document, _AreaBoundary_, (areaBoundary) =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;

          var tol = GeometryTolerance.Model;
          if
          (
            curve.IsShort(tol.ShortCurveTolerance) ||
            curve.IsClosed ||
            !curve.TryGetPlane(out var plane, tol.VertexTolerance) ||
            plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be a valid horizontal, planar and open curve.", curve);
          
          // Compute
          areaBoundary = Reconstruct(areaBoundary, viewPlan, curve);

          DA.SetData(_AreaBoundary_, areaBoundary);
          return areaBoundary;
        }
      );
    }

    bool Reuse(ARDB.ModelCurve areaBoundary, ARDB.ViewPlan view, Curve curve)
    {
      if (areaBoundary is null) return false;

      var genLevel = view.GenLevel;
      if (areaBoundary.LevelId != genLevel?.Id) return false;

      var levelPlane = Plane.WorldXY;
      levelPlane.Translate(Vector3d.ZAxis * genLevel.GetElevation() * Revit.ModelUnits);

      using (var projectedCurve = Curve.ProjectToPlane(curve, levelPlane).ToCurve())
      {
        if (!projectedCurve.AlmostEquals(areaBoundary.GeometryCurve, GeometryTolerance.Internal.VertexTolerance))
          areaBoundary.SetGeometryCurve(projectedCurve, overrideJoins: true);
      }

      return true;
    }

    ARDB.ModelCurve Create(ARDB.ViewPlan view, Curve curve)
    {
      if (view.GenLevel is ARDB.Level level)
      {
        var sketchPlane = level.GetSketchPlane(ensureSketchPlane: true);
        using (var projectedCurve = Curve.ProjectToPlane(curve, sketchPlane.GetPlane().ToPlane()))
          return view.Document.Create.NewAreaBoundaryLine(sketchPlane, projectedCurve.ToCurve(), view);
      }

      return default;
    }

    ARDB.ModelCurve Reconstruct(ARDB.ModelCurve areaBoundaryLine, ARDB.ViewPlan view, Curve curve)
    {
      if (!Reuse(areaBoundaryLine, view, curve))
        areaBoundaryLine = Create(view, curve);
       
      return areaBoundaryLine;
    }
  }
}
