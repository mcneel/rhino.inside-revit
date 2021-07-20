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
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.LayerFunction>()
        {
          Name = "Function",
          NickName = "F",
          Description = "Function of the given compound structure layer",
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
          Optional = true
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
          Optional = true
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
          Optional = true
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
          Optional = true
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
          Optional = true
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
        new Parameters.CompoundStructureLayer()
        {
          Name = "Layer",
          NickName = "L",
          Description = "Constructed compound structure layer",
        }
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Function", out Types.LayerFunction function);
      update |= Params.GetData(DA, "Material", out Types.Material material);
      update |= Params.GetData(DA, "Thickness", out double? width);
      update |= Params.GetData(DA, "Wraps", out bool? wraps);
      update |= Params.GetData(DA, "Structural Material", out bool? structuralMaterial);
      update |= Params.GetData(DA, "Variable", out bool? variableThickness);
      update |= Params.GetData(DA, "Deck Profile", out Types.FamilySymbol deckProfile);
      update |= Params.GetData(DA, "Deck Usage", out Types.DeckEmbeddingType deckType);

      var layer = update ? new Types.CompoundStructureLayer(doc)
      {
        Function = function?.IsValid == true ? function :
        new Types.LayerFunction(width == 0.0 ? DB.MaterialFunctionAssignment.Membrane : DB.MaterialFunctionAssignment.Structure),
        Material = material,
        Width = width,
        LayerCapFlag = wraps,
        StructuralMaterial = structuralMaterial,
        VariableWidth = variableThickness,
        DeckProfile = deckProfile,
        DeckEmbeddingType = deckType
      } :
      default;

      if (layer is object)
      {
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
      }

      DA.SetData("Layer", layer);
    }
  }
}
