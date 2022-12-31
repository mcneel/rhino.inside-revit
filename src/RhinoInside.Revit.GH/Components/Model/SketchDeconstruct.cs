using System;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.11")]
  public class SketchDeconstruct : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F9BC3F5E-7415-485E-B74C-5CB855B818B8");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    protected override string IconTag => string.Empty;

    public SketchDeconstruct() : base
    (
      name: "Deconstruct Sketch",
      nickname: "DecSktch",
      description: string.Empty,
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Sketch()
        {
          Name = "Sketch",
          NickName = "S",
          Description = "Sketch to deconstruct.",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Sketch()
        {
          Name = "Sketch",
          NickName = "S",
          Description = "Sketch element.",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Owner",
          NickName = "O",
          Description = "Sketch owner element.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.SketchPlane()
        {
          Name = "Work Plane",
          NickName = "WP",
          Description = "Work plane element.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Model Lines",
          NickName = "ML",
          Description = $"Sketch curves grouped by boundary-profile.",
          Access = GH_ParamAccess.tree
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.CurveElement()
        {
          Name = "Slope Arrow",
          NickName = "SA",
          Description = "Sketch Slope Arrow.",
          Access = GH_ParamAccess.item
        }, ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sketch", out Types.Sketch sketch, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Sketch", () => sketch);

      Params.TrySetData(DA, "Owner", () => sketch?.Owner);
      Params.TrySetData(DA, "Work Plane", () => sketch?.SketchPlane);
      Params.TrySetDataTree
      (
        DA, "Model Lines", () =>
        sketch?.Value?.GetProfileCurveElements().
        Select(x => x.Select(Types.CurveElement.FromElement)).
        TakeWhileIsNotEscapeKeyDown(this)
      );

      Params.TrySetData(DA, "Slope Arrow", () => sketch?.SlopeArrow);
    }
  }
}
