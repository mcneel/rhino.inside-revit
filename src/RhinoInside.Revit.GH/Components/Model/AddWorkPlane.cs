using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.8")]
  public class AddWorkPlaneByPlane : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("1FA679E4-1821-483A-99F8-DC166B0595F4");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public AddWorkPlaneByPlane() : base
    (
      name: "Add Work Plane (Plane)",
      nickname: "W-Plane",
      description: "Given a Plane, it adds a <not associated> Work Plane element to the active Revit document",
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
        }
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
          NickName = "WP",
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

      sketchPlane.SetLocation(plane.Origin, (ERDB.UnitXYZ) plane.XVec, (ERDB.UnitXYZ) plane.YVec);
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

  [ComponentVersion(introduced: "1.12")]
  public class AddWorkPlaneByFace : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("91757AE0-BFCB-43C3-B762-7C06A7A5D094");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public AddWorkPlaneByFace() : base
    (
      name: "Add Work Plane (Face)",
      nickname: "W-Plane",
      description: "Given a Face, it adds a Work Plane element to the active Revit document",
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
        new Parameters.GeometryFace()
        {
          Name = "Face",
          NickName = "F",
          Description = "Face reference",
        }
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
          NickName = "WP",
          Description = $"Output {_WorkPlane_}",
        }
      ),
    };

    const string _WorkPlane_ = "Work Plane";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.SketchPlane>
      (
        doc.Value, _WorkPlane_, sketchPlane =>
        {
          // Input
          if (!Params.GetData(DA, "Face", out Types.GeometryFace face, x => x.IsValid && x.ReferenceDocument.IsEquivalent(doc.Value))) return null;

          // Compute
          sketchPlane = Reconstruct(sketchPlane, doc.Value, face.GetReference());

          DA.SetData(_WorkPlane_, sketchPlane);
          return sketchPlane;
        }
      );
    }

    bool Reuse(ARDB.SketchPlane sketchPlane, ARDB.Reference reference)
    {
      if (sketchPlane is null) return false;
      if (sketchPlane.GetHost(out var hostFace) is null) return false;
      if (!sketchPlane.Document.AreEquivalentReferences(hostFace, reference)) return false;

      return true;
    }

    ARDB.SketchPlane Reconstruct
    (
      ARDB.SketchPlane sketchPlane,
      ARDB.Document doc,
      ARDB.Reference reference
    )
    {
      if (!Reuse(sketchPlane, reference))
      {
        sketchPlane = sketchPlane.ReplaceElement
        (
          ARDB.SketchPlane.Create(doc, reference),
          default
        );
      }

      return sketchPlane;
    }
  }
}
