using System;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Materials
{
  public class ModifyAssetsOfMaterial : TransactionalChainComponent
  {
    public override Guid ComponentGuid =>
      new Guid("2f1ec561-2c4b-4c44-9587-12b32c6b8351");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public ModifyAssetsOfMaterial() : base(
      name: "Replace Material's Assets",
      nickname: "R-MAST",
      description: "Replace existing assets on the given material, with given assets",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Material>(
        name: "Material",
        nickname: "M",
        description: string.Empty,
        access: GH_ParamAccess.item
        ),
      ParamDefinition.Create<Parameters.AppearanceAsset>(
        name: "Appearance Asset",
        nickname: "AA",
        description: string.Empty,
        access: GH_ParamAccess.item,
        optional: true
      ),
      ParamDefinition.Create<Parameters.StructuralAsset>(
        name: "Physical Asset",
        nickname: "PA",
        description: string.Empty,
        access: GH_ParamAccess.item,
        optional: true
        ),
      ParamDefinition.Create<Parameters.ThermalAsset>(
        name: "Thermal Asset",
        nickname: "TA",
        description: string.Empty,
        access: GH_ParamAccess.item,
        optional: true
        )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Material>(
        name: "Material",
        nickname: "M",
        description: string.Empty,
        access: GH_ParamAccess.item,
        optional: true
        )
    };


    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Material", out Types.Material material, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Appearance Asset", out Types.AppearanceAssetElement appearanceAsset);
      update |= Params.GetData(DA, "Physical Asset", out Types.StructuralAssetElement structuralAsset);
      update |= Params.GetData(DA, "Thermal Asset", out Types.ThermalAssetElement thermalAsset);

      if (update)
      {
        StartTransaction(material.Document);
        material.AppearanceAsset = appearanceAsset;
        material.StructuralAsset = structuralAsset;
        material.ThermalAsset = thermalAsset;
      }

      DA.SetData("Material", material);
    }
  }
}
