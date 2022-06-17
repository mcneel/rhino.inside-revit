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
        doc.Value, _ModelLine_, referenceLine =>
        {
          // Input
          if (!Params.GetData(DA, "Curve", out Curve curve, x => x.IsValid)) return null;
          if (!Params.GetData(DA, "Work Plane", out Types.SketchPlane sketchPlane)) return null;

          var plane = sketchPlane.Location;
          if ((curve = Rhino.Geometry.Curve.ProjectToPlane(curve, plane)) is null)
            throw new Exceptions.RuntimeArgumentException("Curve", "Failed to project curve in the 'Work Plane'.", curve);

          // Compute
          referenceLine = Reconstruct(referenceLine, doc.Value, curve.ToCurve(), sketchPlane.Value);

          DA.SetData(_ModelLine_, referenceLine);
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

