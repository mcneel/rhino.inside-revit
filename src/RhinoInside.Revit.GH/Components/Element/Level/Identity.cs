using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Level
{
  public class LevelIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("E996B34D-2C86-4496-8E02-C879228C329E");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => "ID";

    public LevelIdentity()
    : base("Level Identity", "Identity", "Query level identity information", "Revit", "Model")
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Level>("Level", "L"),
      ParamDefinition.Create<Parameters.Elevation>("Elevation", "E", "Level elevation", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Structural", "S", "Level is structural", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Building Story", "BS", "Level is building story", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Level>("Level", "L", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Level name", GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.Elevation>("Elevation", "E", "Level elevation"),
      ParamDefinition.Create<Param_Boolean>("Structural", "S", "Level is structural", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Building Story", "BS", "Level is building story", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.View>("Plan View", "PV", "Associated plan view", relevance: ParamRelevance.Occasional),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Level", out Types.Level level, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Elevation", out double? elevation);
      update |= Params.GetData(DA, "Structural", out bool? structural);
      update |= Params.GetData(DA, "Building Story", out bool? buildingStory);

      if (update)
      {
        StartTransaction(level.Document);
        if(elevation.HasValue)
          level.SetElevationAbove(Params.Output<Parameters.Elevation>("Elevation").ElevationBase, elevation.Value);

        level.IsStructural = structural;
        level.IsBuildingStory = buildingStory;
      }

      Params.TrySetData(DA, "Level", () => level);
      Params.TrySetData(DA, "Name", () => level.Name);
      Params.TrySetData(DA, "Elevation", () => level.GetElevationAbove(Params.Output<Parameters.Elevation>("Elevation").ElevationBase));
      Params.TrySetData(DA, "Structural", () => level.IsStructural);
      Params.TrySetData(DA, "Building Story", () => level.IsBuildingStory);
      Params.TrySetData(DA, "Plan View", () => new Types.View(level.Document, level.Value.FindAssociatedPlanViewId()));
    }
  }
}
