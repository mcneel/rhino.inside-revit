using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Hosts
{
  public class ConstructCompoundStructure : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("92BA430E-196F-42B6-BAF1-2FE864EF4F89");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "CS";

    public ConstructCompoundStructure() : base
    (
      name: "Construct Compound Structure",
      nickname: "CStruct",
      description: "Construct compound structure",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Parameters.CompoundStructureLayer()
        {
          Name = "Exterior Layers",
          NickName = "EL",
          Description = "Layers which define the exterior side of the compound structure",
          Access = GH_ParamAccess.list,
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.CompoundStructureLayer()
        {
          Name = "Core Layers",
          NickName = "CL",
          Description = "Layers which define the core of the compound structure",
          Access = GH_ParamAccess.list,
        }
      ),
      new ParamDefinition
      (
        new Parameters.CompoundStructureLayer()
        {
          Name = "Interior Layers",
          NickName = "IL",
          Description = "Layers which define the interior side of the compound structure",
          Access = GH_ParamAccess.list,
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Total Thickness",
          NickName = "TT",
          Description = "Total thickness of the given compound structure",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.OpeningWrappingCondition>()
        {
          Name = "Wrapping At Inserts",
          NickName = "WI",
          Description = "Inserts wrapping condition on compound structure",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.EndCapCondition>()
        {
          Name = "Wrapping At Ends",
          NickName = "WE",
          Description = "End cap condition of compound structure",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Sample Height",
          NickName = "SH",
          Description = "Sample height of compound structure",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Cutoff Height",
          NickName = "CH",
          Description = "Cutoff height of compound structure",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CompoundStructure()
        {
          Name = "Structure",
          NickName = "S",
          Description = "Constructed compound structure",
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      bool update = false;
      update |= Params.GetDataList(DA, "Exterior Layers", out IList<Types.CompoundStructureLayer> exterior);
      update |= Params.GetDataList(DA, "Core Layers", out IList<Types.CompoundStructureLayer> core);
      update |= Params.GetDataList(DA, "Interior Layers", out IList<Types.CompoundStructureLayer> interior);
      update |= Params.GetData(DA, "Total Thickness", out double? minThickness);
      update |= Params.GetData(DA, "Wrapping At Inserts", out Types.OpeningWrappingCondition openingWrapping);
      update |= Params.GetData(DA, "Wrapping At Ends", out Types.EndCapCondition endCaps);
      update |= Params.GetData(DA, "Sample Height", out double? sampleHeight);
      update |= Params.GetData(DA, "Cutoff Height", out double? cutoffHeight);

      var structure = update ? new Types.CompoundStructure(doc) : default;

      if(structure is object)
      {
        structure.SetLayers(exterior, core, interior);
        if(minThickness.HasValue) structure.SetWidth(minThickness.Value);
        structure.OpeningWrapping = openingWrapping;
        structure.EndCap = endCaps;

        if (sampleHeight.HasValue)
        {
          if (structure.Value.IsVerticallyCompound)
          {
            var minSampleHeight = structure.Value?.MinimumSampleHeight ?? 0.0;
            if (sampleHeight.Value < minSampleHeight)
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Sample Height value is below the minimum sample height");

            structure.SampleHeight = Math.Max(minSampleHeight, sampleHeight.Value);
          }
          else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Sample Height is only valid for vertical compound structures");
        }

        if (cutoffHeight.HasValue)
        {
          if (structure.Value.IsVerticallyCompound) structure.CutoffHeight = cutoffHeight;
          else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cutoff Height is only valid for vertical compound structures");
        }
      };

      DA.SetData("Structure", structure);
    }
  }
}
