using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.22")]
  public class ViewWorkPlane : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("81D72C8D-D6FE-4BD8-96B9-325B170D4735");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public ViewWorkPlane() : base
    (
      name: "View Work Plane",
      nickname: "WorkPlane",
      description: "View Get-Set work plane",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to access work plane"
      ),
      ParamDefinition.Create<Parameters.SketchPlane>
      (
        name: "Work Plane",
        nickname: "WP",
        description:  "View Work Plane",
        optional:  true,
        relevance: ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to access work plane",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.SketchPlane>
      (
        name: "Work Plane",
        nickname: "WP",
        description:  "View Work Plane",
        relevance: ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.TryGetData(DA, "Work Plane", out Types.SketchPlane workPlane, x => x.IsValid)) return;

      if (workPlane is object)
      {
        StartTransaction(view.Document);
        view.SketchPlane = workPlane;
      }

      Params.TrySetData(DA, "Work Plane", () => workPlane);
    }
  }
}
