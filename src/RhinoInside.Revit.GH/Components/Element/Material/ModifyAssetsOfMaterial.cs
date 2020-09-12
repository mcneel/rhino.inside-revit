using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
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
      ParamDefinition.Create<Parameters.PhysicalAsset>(
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
        material.AppearanceAssetId = appearanceAsset is null ? material.AppearanceAssetId : appearanceAsset.Id;
        material.StructuralAssetId = structuralAsset is null ? material.StructuralAssetId : structuralAsset.Id;
        material.ThermalAssetId = thermalAsset is null ? material.ThermalAssetId : thermalAsset.Id;
        CommitTransaction(doc, transaction);
      }
    }
  }
}
