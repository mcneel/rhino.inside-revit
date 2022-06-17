using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.8")]
  public class AddReferenceLine : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("A2ADB132-6956-423B-AAA4-315A8E6F234F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddReferenceLine() : base
    (
      name: "Add Reference Line",
      nickname: "Reference Line",
      description: "Given a curve, it adds a Reference Line to the current Revit document",
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
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.ModelCurve>
      (
        doc.Value, _ReferenceLine_, referenceLine =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Params.GetData(DA, "Work Plane", out Types.SketchPlane sketchPlane)) return null;

          var plane = sketchPlane.Location;
          if ((curve = Rhino.Geometry.Curve.ProjectToPlane(curve, plane)) is null)
            throw new Exceptions.RuntimeArgumentException("Curve", "Failed to project curve in the 'Work Plane'.", curve);

          // Compute
          referenceLine = Reconstruct(referenceLine, doc.Value, curve.ToCurve(), sketchPlane.Value);

          DA.SetData(_ReferenceLine_, referenceLine);
          return referenceLine;
        }
      );
    }

    bool Reuse
    (
      ARDB.ModelCurve referenceLine,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      if (referenceLine is null) return false;

      if (referenceLine.SketchPlane.IsEquivalent(sketchPlane))
      {
        if (!curve.AlmostEquals(referenceLine.GeometryCurve, GeometryTolerance.Internal.VertexTolerance))
          referenceLine.SetGeometryCurve(curve, true);
      }
      else referenceLine.SetSketchPlaneAndCurve(sketchPlane, curve);

      return true;
    }

    ARDB.ModelCurve Create
    (
      ARDB.Document doc,
      ARDB.Curve curve, ARDB.SketchPlane sketchPlane
    )
    {
      var referenceLine = default(ARDB.ModelCurve);

      if (doc.IsFamilyDocument)
        referenceLine = doc.FamilyCreate.NewModelCurve(curve, sketchPlane);
      else
        referenceLine = doc.Create.NewModelCurve(curve, sketchPlane);

      referenceLine.ChangeToReferenceLine();
      return referenceLine;
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
