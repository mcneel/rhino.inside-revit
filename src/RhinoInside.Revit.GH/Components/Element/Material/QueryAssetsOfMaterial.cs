using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using Autodesk.Private.InfoCenter;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
#if REVIT_2019
  public class QueryAssetsOfMaterial : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("1f644064-035a-4fa1-971b-64d7da824f09");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public QueryAssetsOfMaterial() : base(
      name: "Extract Material's Assets",
      nickname: "E-MA",
      description: "Queries appearance, structural, and other assets from given material",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.Material(),
        name: "Material",
        nickname: "M",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.AppearanceAsset(),
        name: "Appearance Asset",
        nickname: "AA",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
      pManager.AddParameter(
        param: new Parameters.StructuralAsset(),
        name: "Physical Asset",
        nickname: "PA",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
      pManager.AddParameter(
        param: new Parameters.ThermalAsset(),
        name: "Thermal Asset",
        nickname: "TA",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      var material = default(DB.Material);
      if (!DA.GetData("Material", ref material))
        return;

      var doc = material.Document;
      // appearance asset
      if (doc.GetElement(material.AppearanceAssetId) is DB.AppearanceAssetElement aae)
        DA.SetData(
          "Appearance Asset",
          new Types.AppearanceAsset(aae)
          );
      // structural asset
      if (doc.GetElement(material.StructuralAssetId) is DB.PropertySetElement sae)
        DA.SetData(
          "Physical Asset",
          new Types.StructuralAsset(sae)
          );
      // thermal asset
      if (doc.GetElement(material.ThermalAssetId) is DB.PropertySetElement tae)
        DA.SetData(
          "Thermal Asset",
          new Types.ThermalAsset(tae)
          );
    }
  }
#endif
}
