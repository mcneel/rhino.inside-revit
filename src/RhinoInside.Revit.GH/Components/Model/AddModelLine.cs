using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.8")]
  public class AddModelLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("240127B1-94EE-47C9-98F8-05DE32447B01");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddModelLine() : base
    (
      name: "Add Model Line",
      nickname: "Model Line",
      description: "Given a curve, it adds a Model Line to the current Revit document",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_Curve()
        {
          Name = "Curve",
          NickName = "Curve",
          Description = "Curve to sketch",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = "Work Plane element",
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
          Name = _ModelLine_,
          NickName = "ML",
          Description = $"Output {_ModelLine_}",
        }
      ),
    };

    const string _ModelLine_ = "Model Line";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM,
      ARDB.BuiltInParameter.ELEM_FAMILY_PARAM,
      ARDB.BuiltInParameter.ELEM_TYPE_PARAM,
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Input<IGH_Param>("SketchPlane") is IGH_Param sketchPlane)
        sketchPlane.Name = "Work Plane";

      if (Params.Output<IGH_Param>("CurveElement") is IGH_Param curveElement)
        curveElement.Name = _ModelLine_;

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        doc.Value, _ModelLine_, modelLine =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Params.GetData(DA, "Work Plane", out Types.SketchPlane sketchPlane)) return null;

          var plane = sketchPlane.Location;
          var tol = GeometryTolerance.Model;

          if (curve.IsShort(tol.ShortCurveTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is too short.\nMin length is {tol.ShortCurveTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (curve.IsClosed(tol.VertexTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve is closed or end points are under tolerance.\nTolerance is {tol.VertexTolerance} {GH_Format.RhinoUnitSymbol()}", curve);

          if (!curve.IsParallelToPlane(plane, tol.VertexTolerance, tol.AngleTolerance))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be planar and parallel to view plane.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          if ((curve = Curve.ProjectToPlane(curve, plane)) is null)
            throw new Exceptions.RuntimeArgumentException("Curve", "Failed to project Curve into 'Work Plane'", curve);

          if (curve.GetNextDiscontinuity(Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, Math.Cos(tol.AngleTolerance), Rhino.RhinoMath.SqrtEpsilon, out var _))
            throw new Exceptions.RuntimeArgumentException("Curve", $"Curve should be C1 continuous.\nTolerance is {Rhino.RhinoMath.ToDegrees(tol.AngleTolerance):N1}°", curve);

          // Compute
          modelLine = Reconstruct(modelLine, doc.Value, curve.ToCurve(), sketchPlane.Value);

          DA.SetData(_ModelLine_, modelLine);
          return modelLine;
        }
      );
    }

    bool Reuse
    (
      ARDB.ModelCurve modelLine,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      if (modelLine is null) return false;

      if (!curve.IsSameKindAs(modelLine.GeometryCurve)) return false;
      if (modelLine.SketchPlane.IsEquivalent(sketchPlane))
      {
        if (!curve.AlmostEquals(modelLine.GeometryCurve, GeometryTolerance.Internal.VertexTolerance))
          modelLine.SetGeometryCurve(curve, true);
      }
      else modelLine.SetSketchPlaneAndCurve(sketchPlane, curve);

      return true;
    }

    ARDB.ModelCurve Create
    (
      ARDB.Document doc,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      var modelLine = default(ARDB.ModelCurve);

      if (doc.IsFamilyDocument)
        modelLine = doc.FamilyCreate.NewModelCurve(curve, sketchPlane);
      else
        modelLine = doc.Create.NewModelCurve(curve, sketchPlane);

      return modelLine;
    }

    ARDB.ModelCurve Reconstruct
    (
      ARDB.ModelCurve modelLine, ARDB.Document doc,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      if (!Reuse(modelLine, curve, sketchPlane))
      {
        modelLine = modelLine.ReplaceElement
        (
          Create(doc, curve, sketchPlane),
          ExcludeUniqueProperties
        );
      }

      return modelLine;
    }
  }
}

