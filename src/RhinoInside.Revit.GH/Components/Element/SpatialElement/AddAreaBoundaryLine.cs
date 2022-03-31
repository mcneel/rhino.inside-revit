using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.SpatialElement  
{
  [ComponentVersion(introduced: "1.7")]
  public class AddAreaBoundaryLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("34D68CDC-892B-4525-959D-49C0AC66317E");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public AddAreaBoundaryLine() : base
    (
      name: "Add Area Boundary Line",
      nickname: "AB",
      description: "Given a Curve, it adds an Area Boundary Line to the given Area Plan",
      category: "Revit",
      subCategory: "Room & Area"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        }, ParamRelevance.Occasional
      ),
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
          Name = _AreaBoundaryLine_,
          NickName = _AreaBoundaryLine_.Substring(0, 1),
          Description = $"Output {_AreaBoundaryLine_}",
          Access = GH_ParamAccess.item
        }
      )
    };

    const string _AreaBoundaryLine_ = "Area Boundary Line";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        doc.Value, _AreaBoundaryLine_, (areaBoundaryLine) =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve)) return null;
          if (!Params.GetData(DA, "Area Plan", out ARDB.ViewPlan viewPlan)) return null;

          var tol = GeometryObjectTolerance.Model;
          if
          (
            curve.IsShort(tol.ShortCurveTolerance) ||
            curve.IsClosed ||
            !curve.TryGetPlane(out var plane, tol.VertexTolerance) ||
            plane.ZAxis.IsParallelTo(Vector3d.ZAxis, tol.AngleTolerance) == 0
          )
            throw new Exceptions.RuntimeArgumentException("Curve", "Curve should be a valid horizontal, coplanar and open curve.", curve);
          
          // Compute
          areaBoundaryLine = Reconstruct(areaBoundaryLine, doc.Value, curve.ToCurve(), viewPlan);

          DA.SetData(_AreaBoundaryLine_, areaBoundaryLine);
          return areaBoundaryLine;
        }
      );
    }

    bool Reuse(ARDB.ModelCurve areaBoundary, ARDB.Curve curve, ARDB.ViewPlan viewPlan)
    {
      if (areaBoundary is null) return false;
      if (areaBoundary.OwnerViewId != viewPlan.Id) return false;
      var projectedCurve = Curve.ProjectToPlane(curve.ToCurve(), viewPlan.SketchPlane.GetPlane().ToPlane());

      if (!projectedCurve.ToCurve().IsAlmostEqualTo(areaBoundary.GeometryCurve))
        areaBoundary.SetGeometryCurve(projectedCurve.ToCurve(), true);
      return true;
    }

    ARDB.ModelCurve Create(ARDB.Document doc, ARDB.Curve curve, ARDB.ViewPlan viewplan)
    {
      return doc.Create.NewAreaBoundaryLine(viewplan.SketchPlane, curve, viewplan);
    }

    ARDB.ModelCurve Reconstruct(ARDB.ModelCurve areaBoundaryLine, ARDB.Document doc, ARDB.Curve curve, ARDB.ViewPlan viewPlan)
    {
      if (!Reuse(areaBoundaryLine, curve, viewPlan))
          areaBoundaryLine = Create(doc, curve, viewPlan);
       
      return areaBoundaryLine;
    }
  }
}
