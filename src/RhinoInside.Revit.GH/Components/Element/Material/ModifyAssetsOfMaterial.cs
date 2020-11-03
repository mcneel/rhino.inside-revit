using System;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
{
#if REVIT_2019
  public class ModifyAssetsOfMaterial : TransactionalComponent
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
      // get input
      var material = default(DB.Material);
      if (!DA.GetData("Material", ref material))
        return;

      // get assets
      var appearanceAsset = default(DB.AppearanceAssetElement);
      DA.GetData("Appearance Asset", ref appearanceAsset);
      var structuralAsset = default(DB.PropertySetElement);
      DA.GetData("Physical Asset", ref structuralAsset);
      var thermalAsset = default(DB.PropertySetElement);
      DA.GetData("Thermal Asset", ref thermalAsset);

      var doc = material.Document;
      using (var transaction = NewTransaction(doc))
      {
        transaction.Start();

        // validate and apply appearance asset if provided
        if (appearanceAsset != null)
          if (!doc.Equals(appearanceAsset.Document))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Document mismatch: Skipping Appearance Asset since it is not in the same document as Material");
          else
            material.AppearanceAssetId = appearanceAsset.Id;

        // validate and apply structural asset if provided
        if (structuralAsset != null)
          if (!doc.Equals(structuralAsset.Document))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Document mismatch: Skipping Physical Asset since it is not in the same document as Material");
          else
            material.StructuralAssetId = structuralAsset.Id;

        // validate and apply thermal asset if provided
        if (thermalAsset != null)
          if (!doc.Equals(thermalAsset.Document))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Document mismatch: Skipping Thermal Asset since it is not in the same document as Material");
          else
            material.ThermalAssetId = thermalAsset.Id;

        CommitTransaction(doc, transaction);
      }

      DA.SetData("Material", material);
    }
  }
#endif
}
