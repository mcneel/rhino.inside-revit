using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Site
{
  [ComponentVersion(introduced: "1.0", updated: "1.14")]
  public sealed class ConstructProjectElevation : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("54C795D0-38F8-4703-8968-0336C9D9B066");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public ConstructProjectElevation() : base
    (
      name: "Project Elevation",
      nickname: "Elevation",
      description: "Constructs a project elevation",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.ProjectElevation>("Elevation", "E", "Elevation in a project", optional: true),
      ParamDefinition.Create<Parameters.BasePoint>("Base Point", "BP", "Reference base point", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the base point", optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ProjectElevation>("Elevation", "E", "Elevation in a project"),
      ParamDefinition.Create<Parameters.BasePoint>("Base Point", "BP", "Reference base point", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the base point", relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Elevation", out Types.ProjectElevation elevation)) return;
      if (!Params.TryGetData(DA, "Base Point", out Types.IGH_BasePoint basePoint)) return;
      if (!Params.TryGetData(DA, "Offset", out double? offset)) return;

      if (basePoint is object) elevation %= basePoint;
      if (offset is object) elevation += offset;

      Params.TrySetData(DA, "Elevation",  () => elevation);
      if (elevation?.IsProjectElevation(out var elevationBase, out var elevationOffset) is true)
      {
        Params.TrySetData(DA, "Base Point", () => elevationBase);
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
      else if (elevation?.IsOffset(out elevationOffset) is true)
      {
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
    }
  }
}
