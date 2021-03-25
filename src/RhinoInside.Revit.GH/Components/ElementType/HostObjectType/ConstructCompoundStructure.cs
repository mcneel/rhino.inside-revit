using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
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
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
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
        ParamVisibility.Default
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
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Total Thickness",
          NickName = "TT",
          Description = "Total thickness of the given compound structure",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.OpeningWrappingCondition>()
        {
          Name = "Wrapping At Inserts",
          NickName = "WI",
          Description = "Inserts wrapping condition on compound structure",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.EndCapCondition>()
        {
          Name = "Wrapping At Ends",
          NickName = "WE",
          Description = "End cap condition of compound structure",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Sample Height",
          NickName = "SH",
          Description = "Sample height of compound structure",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Cutoff Height",
          NickName = "CH",
          Description = "Cutoff height of compound structure",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
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
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      Params.GetDataList(DA, "Exterior Layers", out IList<Types.CompoundStructureLayer> exterior);
      Params.GetDataList(DA, "Core Layers", out IList<Types.CompoundStructureLayer> core);
      Params.GetDataList(DA, "Interior Layers", out IList<Types.CompoundStructureLayer> interior);
      Params.GetData(DA, "Total Thickness", out double? minThickness);
      Params.GetData(DA, "Wrapping At Inserts", out Types.OpeningWrappingCondition openingWrapping);
      Params.GetData(DA, "Wrapping At Ends", out Types.EndCapCondition endCaps);
      Params.GetData(DA, "Sample Height", out double? sampleHeight);
      Params.GetData(DA, "Cutoff Height", out double? cutoffHeight);

      var structure = new Types.CompoundStructure(doc);
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
