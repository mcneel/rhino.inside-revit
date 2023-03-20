using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.8", updated: "1.10")]
  public class AddReferenceLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("A2ADB132-6956-423B-AAA4-315A8E6F234F");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public AddReferenceLine() : base
    (
      name: "Add Reference Line",
      nickname: "Reference Line",
      description: "Given a curve, it adds a Reference Line to the provided Work Plane",
      category: "Revit",
      subCategory: "Annotation"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = "Work Plane element",
        }
      ),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "Curve",
          Description = "Curve to sketch",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = _ReferenceLine_,
          NickName = "RL",
          Description = $"Output {_ReferenceLine_}",
        }
      ),
    };

    const string _ReferenceLine_ = "Reference Line";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Work Plane", out Types.SketchPlane sketchPlane)) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        sketchPlane.Document, _ReferenceLine_, referenceLine =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;

          var plane = sketchPlane.Location;
          var tol = GeometryTolerance.Model;

          if (curve.IsShort(tol.ShortCurveTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve is NurbsCurve && curve.IsClosed(tol.ShortCurveTolerance * 1.01) && !curve.IsEllipse(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (!curve.IsParallelToPlane(plane, tol.VertexTolerance, tol.AngleTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be planar and parallel to view plane.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          if ((curve = Curve.ProjectToPlane(curve, plane)) is null)
            throw new Exceptions.RuntimeArgumentException("Curve", "Failed to project Curve into 'Work Plane'", curve);

          if (curve.GetNextDiscontinuity(Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, Math.Cos(tol.AngleTolerance), Rhino.RhinoMath.SqrtEpsilon, out var _))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be C1 continuous.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          // Compute
          referenceLine = Reconstruct(referenceLine, sketchPlane.Document, curve.ToCurve(), sketchPlane.Value);

          DA.SetData(_ReferenceLine_, referenceLine);
          return referenceLine;
        }
      );
    }

    bool Reuse
    (
      ARDB.ModelCurve modelCurve,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      if (modelCurve is null) return false;

      using (var geometryCurve = modelCurve.GeometryCurve)
      {
        if (!curve.IsSameKindAs(geometryCurve)) return false;
        if (modelCurve.SketchPlane.IsEquivalent(sketchPlane))
        {
          if (!curve.AlmostEquals(geometryCurve, modelCurve.Document.Application.VertexTolerance))
            modelCurve.SetGeometryCurve(curve, true);
        }
        else modelCurve.SetSketchPlaneAndCurve(sketchPlane, curve);
      }

      return true;
    }

    ARDB.ModelCurve Create
    (
      ARDB.Document doc,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      using (var create = doc.Create())
      {
        var referenceLine = create.NewModelCurve(curve, sketchPlane);
        referenceLine.ChangeToReferenceLine();
        return referenceLine;
      }
    }

    ARDB.ModelCurve Reconstruct
    (
      ARDB.ModelCurve referenceLine, ARDB.Document doc,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      if (!Reuse(referenceLine, curve, sketchPlane))
      {
        referenceLine = referenceLine.ReplaceElement
        (
          Create(doc, curve, sketchPlane),
          ExcludeUniqueProperties
        );
      }

      return referenceLine;
    }
  }
}
