using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class DeconstructCompoundStructureLayer : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("6B32703E-A8B4-49C9-B855-133D3DF925FE");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "CSL";

    public DeconstructCompoundStructureLayer() : base
    (
      name: "Deconstruct Compound Structure Layer",
      nickname: "DStructLayer",
      description: "Deconstruct compound structure layer",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.CompoundStructureLayer()
        {
          Name = "Layer",
          NickName = "L",
          Description = "Compound struture layer to deconstruct",
        }
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.LayerFunction>()
        {
          Name = "Function",
          NickName = "F",
          Description = "Function of the given compound structure layer",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Material()
        {
          Name = "Material",
          NickName = "M",
          Description = "Material assigned to the given compound structure layer",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Thickness",
          NickName = "T",
          Description = "Thickness of the given compound structure layer",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Wraps",
          NickName = "W",
          Description = "Whether compound structure layer participates in wrapping at end caps and/or inserts",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Structural Material",
          NickName = "SM",
          Description = "Indicates the layer material defines the structural properties of the type",
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Variable",
          NickName = "V",
          Description = "Indicates the layer thickness is variable",
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Deck Profile",
          NickName = "DP",
          Description = "Deck profile of structural deck layer",
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.DeckEmbeddingType>()
        {
          Name = "Deck Usage",
          NickName = "DU",
          Description = "Embedding type for structural deck layer",
        },
        ParamRelevance.Occasional
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Layer", out Types.CompoundStructureLayer layer))
        return;

      Params.TrySetData(DA, "Function", () => layer.Function);
      Params.TrySetData(DA, "Material", () => layer.Material);
      Params.TrySetData(DA, "Thickness", () => layer.Width);
      Params.TrySetData(DA, "Wraps", () => layer.LayerCapFlag);
      Params.TrySetData(DA, "Structural Material", () => layer.StructuralMaterial);
      Params.TrySetData(DA, "Variable", () => layer.VariableWidth);
      Params.TrySetData(DA, "Deck Profile", () => layer.DeckProfile);
      Params.TrySetData(DA, "Deck Usage", () => layer.DeckEmbeddingType);
    }
  }
}

namespace RhinoInside.Revit.GH.Components.Obsolete
{
  [Obsolete("Since 2021-03-24")]
  public class DeconstructCompoundStructureLayer : Component
  {
    public override Guid ComponentGuid => new Guid("BC64525A-10B6-46DB-A134-CF803738B1A0");
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.hidden;
    protected override string IconTag => "DCSL";

    public DeconstructCompoundStructureLayer() : base
    (
      name: "Deconstruct Compound Structure Layer",
      nickname: "DecStructLayer",
      description: "Deconstructs given compound structure layer into its properties",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.CompoundStructureLayer(),
        name: "Compound Structure Layer",
        nickname: "CSL",
        description: "Compound Structure Layer",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddIntegerParameter(
          name: "Index",
          nickname: "IDX",
          description: "Index of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.LayerFunction>(),
          name: "Function",
          nickname: "F",
          description: "Function of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Material(),
          name: "Material",
          nickname: "M",
          description: "Material assigned to the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddNumberParameter(
          name: "Thickness",
          nickname: "T",
          description: "Thickness of the given compound structure layer",
          access: GH_ParamAccess.item
          );
      manager.AddBooleanParameter(
          name: "Wraps",
          nickname: "W",
          description: "Whether compound structure layer participates in wrapping at end caps and/or inserts",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Element(),
          name: "Deck Profile",
          nickname: "DP",
          description: "Deck profile of structural deck layer",
          access: GH_ParamAccess.item
          );
      manager.AddParameter(
          param: new Parameters.Param_Enum<Types.DeckEmbeddingType>(),
          name: "Deck Embedding Type",
          nickname: "DET",
          description: "Embedding type for structural deck layer",
          access: GH_ParamAccess.item
          );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var layer = default(Types.CompoundStructureLayer);
      if (!DA.GetData("Compound Structure Layer", ref layer))
        return;

      if (layer.Value is DB.CompoundStructureLayer cslayer)
      {
        DA.SetData("Index", cslayer.LayerId);
        DA.SetData("Function", cslayer.Function);
        DA.SetData("Material", new Types.Material(layer.Document, cslayer.MaterialId));
        DA.SetData("Thickness", cslayer.Width * Revit.ModelUnits);
        DA.SetData("Wraps", cslayer.LayerCapFlag);
        DA.SetData("Deck Profile", Types.Element.FromElement(layer.Document.GetElement(cslayer.DeckProfileId)));
        DA.SetData("Deck Embedding Type", cslayer.DeckEmbeddingType);
      }
    }
  }
}
