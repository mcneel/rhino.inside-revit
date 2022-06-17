using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Grasshopper.Kernel.Parameters;
  using Kernel.Attributes;

  [ComponentVersion(introduced: "1.0", updated: "1.8")]
  public class AddWorkPlaneByPlane : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("1FA679E4-1821-483A-99F8-DC166B0595F4");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddWorkPlaneByPlane() : base
    (
      name: "Add Work Plane (Plane)",
      nickname: "WorkPlane",
      description: "Given a Plane, it adds an <unconnected> Work Plane element to the active Revit document",
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
        new Param_Plane()
        {
          Name = "Plane",
          NickName = "P",
          Description = "Plane definition",
        },
        ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = _WorkPlane_,
          NickName = "ML",
          Description = $"Output {_WorkPlane_}",
        }
      ),
    };

    const string _WorkPlane_ = "Work Plane";

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("SketchPlane") is IGH_Param sketchPlane)
        sketchPlane.Name = _WorkPlane_;

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.SketchPlane>
      (
        doc.Value, _WorkPlane_, sketchPlane =>
        {
          // Input
          if (!Params.GetData(DA, "Plane", out Plane? plane, x => x.IsValid)) return null;

          // Compute
          sketchPlane = Reconstruct(sketchPlane, doc.Value, plane.Value.ToPlane());

          DA.SetData(_WorkPlane_, sketchPlane);
          return sketchPlane;
        }
      );
    }

    bool Reuse(ARDB.SketchPlane sketchPlane, ARDB.Plane plane)
    {
      if (sketchPlane is null) return false;

      bool pinned = sketchPlane.Pinned;
      sketchPlane.Pinned = false;

      var plane0 = sketchPlane.GetPlane();
      var plane1 = plane;
      {
        if (!plane0.Normal.IsParallelTo(plane1.Normal))
        {
          var axisDirection = plane0.Normal.CrossProduct(plane1.Normal);
          double angle = plane0.Normal.AngleTo(plane1.Normal);

          using (var axis = ARDB.Line.CreateUnbound(plane0.Origin, axisDirection))
            ARDB.ElementTransformUtils.RotateElement(sketchPlane.Document, sketchPlane.Id, axis, angle);

          plane0 = sketchPlane.GetPlane();
        }

        {
          double angle = plane0.XVec.AngleOnPlaneTo(plane1.XVec, plane1.Normal);
          if (angle != 0.0)
          {
            using (var axis = ARDB.Line.CreateUnbound(plane0.Origin, plane1.Normal))
              ARDB.ElementTransformUtils.RotateElement(sketchPlane.Document, sketchPlane.Id, axis, angle);
          }
        }

        var trans = plane1.Origin - plane0.Origin;
        if (!trans.IsZeroLength())
          ARDB.ElementTransformUtils.MoveElement(sketchPlane.Document, sketchPlane.Id, trans);
      }

      sketchPlane.Pinned = pinned;
      return true;
    }

    ARDB.SketchPlane Reconstruct
    (
      ARDB.SketchPlane sketchPlane,
      ARDB.Document doc,
      ARDB.Plane plane
    )
    {
      if (!Reuse(sketchPlane, plane))
      {
        sketchPlane = sketchPlane.ReplaceElement
        (
          ARDB.SketchPlane.Create(doc, plane),
          default
        );
      }

      return sketchPlane;
    }
  }
}
