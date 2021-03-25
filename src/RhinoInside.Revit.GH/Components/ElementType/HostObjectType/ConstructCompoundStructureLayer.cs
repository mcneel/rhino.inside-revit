using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class ConstructCompoundStructureLayer : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("69EE799C-266F-401E-9F77-A4533EBB5A60");
    public override GH_Exposure Exposure => GH_Exposure.senary;
    protected override string IconTag => "CSL";

    public ConstructCompoundStructureLayer() : base
    (
      name: "Construct Compound Structure Layer",
      nickname: "CStructLayer",
      description: "Construct compound structure layer",
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
        new Parameters.Param_Enum<Types.LayerFunction>()
        {
          Name = "Function",
          NickName = "F",
          Description = "Function of the given compound structure layer",
          Access = GH_ParamAccess.item,
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Parameters.Material()
        {
          Name = "Material",
          NickName = "M",
          Description = "Material assigned to the given compound structure layer",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Thickness",
          NickName = "T",
          Description = "Thickness of the given compound structure layer",
          Access = GH_ParamAccess.item,
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Wraps",
          NickName = "W",
          Description = "Whether compound structure layer participates in wrapping at end caps and/or inserts",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Structural Material",
          NickName = "SM",
          Description = "Indicates the layer material defines the structural properties of the type",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Variable",
          NickName = "V",
          Description = "Indicates the layer thickness is variable",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Parameters.FamilySymbol()
        {
          Name = "Deck Profile",
          NickName = "DP",
          Description = "Deck profile of structural deck layer",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Voluntary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.DeckEmbeddingType>()
        {
          Name = "Deck Usage",
          NickName = "DU",
          Description = "Embedding type for structural deck layer",
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
        new Parameters.CompoundStructureLayer()
        {
          Name = "Layer",
          NickName = "L",
          Description = "Constructed compound structure layer",
          Access = GH_ParamAccess.item
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      Params.GetData(DA, "Function", out Types.LayerFunction function);
      Params.GetData(DA, "Material", out Types.Material material);
      Params.GetData(DA, "Thickness", out double? width);
      Params.GetData(DA, "Wraps", out bool? wraps);
      Params.GetData(DA, "Structural Material", out bool? structuralMaterial);
      Params.GetData(DA, "Variable", out bool? variableThickness);
      Params.GetData(DA, "Deck Profile", out Types.FamilySymbol deckProfile);
      Params.GetData(DA, "Deck Usage", out Types.DeckEmbeddingType deckType);

      var layer = new Types.CompoundStructureLayer(doc)
      {
        Function = function?.IsValid == true ? function : new Types.LayerFunction(DB.MaterialFunctionAssignment.Structure),
        Material = material,
        Width = width.HasValue && width >= 0.0 ? width : 0.0,
        LayerCapFlag = wraps,
        StructuralMaterial = structuralMaterial,
        VariableWidth = variableThickness,
        DeckProfile = deckProfile,
        DeckEmbeddingType = deckType
      };

      if (layer.Function.Value == DB.MaterialFunctionAssignment.Membrane)
      {
        if (layer.Width.Value != 0.0)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, Types.CompoundStructure.ToString(DB.CompoundStructureError.MembraneTooThick));
      }
      else if (layer.Function.Value != DB.MaterialFunctionAssignment.Membrane)
      {
        if (layer.Width.Value == 0.0)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, Types.CompoundStructure.ToString(DB.CompoundStructureError.NonmembraneTooThin));
      }

      DA.SetData("Layer", layer);
    }
  }
}
