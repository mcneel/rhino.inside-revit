using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Annotations.Levels
{
  [ComponentVersion(introduced: "1.0", updated: "1.14")]
  public sealed class LevelOffset : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("01C853D8-87A3-4A76-8855-130BECA30DA1");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public LevelOffset() : base
    (
      name: "Level Offset",
      nickname: "L-Offset",
      description: "Get-Set a level offset",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.LevelConstraint>("Elevation", "E", "Level offset elevation", optional: true),
      ParamDefinition.Create<Parameters.Level>("Level", "L", "Reference level", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the Level", optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.LevelConstraint>("Elevation", "E", "Level offset elevation"),
      ParamDefinition.Create<Parameters.Level>("Level", "L", "Reference level", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Offset", "O", "Offset above or below the Level", relevance: ParamRelevance.Primary)
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Constraint") is IGH_Param constraint)
        constraint.Name = "Elevation";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "Elevation", out Types.LevelConstraint elevation)) return;
      if (!Params.TryGetData(DA, "Level", out Types.Level level)) return;
      if (!Params.TryGetData(DA, "Offset", out double? offset)) return;

      if (level is object) elevation %= level;
      if (offset is object) elevation += offset;

      Params.TrySetData(DA, "Elevation", () => elevation);
      if (elevation?.IsLevelConstraint(out var elevationLevel, out var elevationOffset) is true)
      {
        Params.TrySetData(DA, "Level", () => elevationLevel);
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
      else if (elevation?.IsOffset(out elevationOffset) is true)
      {
        Params.TrySetData(DA, "Offset", () => elevationOffset);
      }
    }
  }
}
