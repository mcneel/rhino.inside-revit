using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class DeconstructCompoundStructure : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("120090A3-1CD6-4C97-8CA2-AB65587936ED");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "DS";

    public DeconstructCompoundStructure() : base
    (
      name: "Deconstruct Compound Structure",
      nickname: "DStruct",
      description: "Deconstruct compound structure",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.CompoundStructure()
        {
          Name = "Structure",
          NickName = "S",
          Description = "Compound structure to deconstruct",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.CompoundStructureLayer()
        {
          Name = "Exterior Layers",
          NickName = "EL",
          Description = "Layers which define the exterior side of the compound structure",
          Access = GH_ParamAccess.list,
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
        },
        ParamVisibility.Voluntary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Structure", out Types.CompoundStructure structure))
        return;

      structure.GetLayers(out var exterior, out var core, out var interior);

      Params.TrySetDataList(DA, "Exterior Layers", () => exterior);
      Params.TrySetDataList(DA, "Core Layers", () => core);
      Params.TrySetDataList(DA, "Interior Layers", () => interior);
      Params.TrySetData(DA, "Total Thickness", () => structure.GetWidth());
      Params.TrySetData(DA, "Wrapping At Inserts", () => structure.OpeningWrapping);
      Params.TrySetData(DA, "Wrapping At Ends", () => structure.EndCap);
      if (structure.Value.IsVerticallyCompound)
      {
        Params.TrySetData(DA, "Sample Height", () => structure.SampleHeight);
        Params.TrySetData(DA, "Cutoff Height", () => structure.CutoffHeight);
      }
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Since 2021-03-24")]
  public class DeconstructCompoundStructure : Component
  {
    public override Guid ComponentGuid => new Guid("D0853B76-49FA-4BA8-869C-293A9C30FFE1");
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.hidden;
    protected override string IconTag => "DCS";

    public DeconstructCompoundStructure() : base
    (
      name: "Deconstruct Compound Structure",
      nickname: "DecCompStruct",
      description: "Deconstructs given compound structure into its properties",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.CompoundStructure(),
        name: "Compound Structure",
        nickname: "CS",
        description: "Compound Structure",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddNumberParameter(
          name: "Total Thickness",
          nickname: "TT",
          description: "Total thickness of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Sample Height",
          nickname: "SH",
          description: "Sample height of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Cutoff Height",
          nickname: "COH",
          description: "Cutoff height or compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Minimum Sample Height",
          nickname: "MSH",
          description: "Minimum sample height of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Minimum Layer Thickness",
          nickname: "MLT",
          description: "Minimum thickness allowed for compound structure layers",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.CompoundStructureLayer(),
          name: "Layers",
          nickname: "L",
          description: "Individual layers of compound structure",
          access: GH_ParamAccess.list
          );
      manager.AddIntegerParameter(
          name: "First Core Layer Index",
          nickname: "FCLIDX",
          description: "Index of first core layer of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddIntegerParameter(
          name: "Last Core Layer Index",
          nickname: "LCLIDX",
          description: "Index of last core layer of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddIntegerParameter(
          name: "Structural Material Index",
          nickname: "SMIDX",
          description: "Index of layer whose material defines the structural properties of the type for the purposes of analysis",
          access: GH_ParamAccess.item
          );
      manager.AddIntegerParameter(
          name: "Variable Layer Index",
          nickname: "VLIDX",
          description: "Index of layer which is designated as variable",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.OpeningWrappingCondition>(),
          name: "Wrapping At Inserts",
          nickname: "IW",
          description: "Inserts wrapping condition on compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.EndCapCondition>(),
          name: "Wrapping At Ends",
          nickname: "EW",
          description: "End cap condition of compound structure",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Has Structural Deck",
          nickname: "HSD",
          description: "Whether compound structure has structural deck",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Is Vertically Compound",
          nickname: "IVC",
          description: "Whether compound structure is vertically compound",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Is Vertically Homogeneous",
          nickname: "IVH",
          description: "Whether compound structure is vertically homogeneous",
          access: GH_ParamAccess.item
          );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.CompoundStructure structure = default;
      if (!DA.GetData("Compound Structure", ref structure))
        return;

      if (structure.Value is DB.CompoundStructure cstruct)
      {
        DA.SetData("Total Thickness", cstruct.GetWidth() * Revit.ModelUnits);
        DA.SetData("Sample Height", cstruct.SampleHeight * Revit.ModelUnits);
        DA.SetData("Cutoff Height", cstruct.CutoffHeight * Revit.ModelUnits);
        DA.SetData("Minimum Sample Height", cstruct.MinimumSampleHeight * Revit.ModelUnits);
        DA.SetData("Minimum Layer Thickness", DB.CompoundStructure.GetMinimumLayerThickness() * Revit.ModelUnits);
        DA.SetDataList("Layers", cstruct.GetLayers().Select(x => new Types.CompoundStructureLayer(structure.Document, x)));
        DA.SetData("First Core Layer Index", cstruct.GetFirstCoreLayerIndex());
        DA.SetData("Last Core Layer Index", cstruct.GetLastCoreLayerIndex());
        DA.SetData("Structural Material Index", cstruct.StructuralMaterialIndex);
        DA.SetData("Variable Layer Index", cstruct.VariableLayerIndex);
        DA.SetData("Wrapping At Inserts", cstruct.OpeningWrapping);
        DA.SetData("Wrapping At Ends", cstruct.EndCap);
        DA.SetData("Has Structural Deck", cstruct.HasStructuralDeck);
        DA.SetData("Is Vertically Compound", cstruct.IsVerticallyCompound);
        DA.SetData("Is Vertically Homogeneous", cstruct.IsVerticallyHomogeneous());
      }
    }
  }
}
