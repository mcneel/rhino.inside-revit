using System;

using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Materials
{
  public class QueryAssetsOfMaterial : Component
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
      var material = default(Types.Material);
      if (!DA.GetData("Material", ref material))
        return;

      DA.SetData("Appearance Asset", material.AppearanceAsset);
      DA.SetData("Physical Asset", material.StructuralAsset);
      DA.SetData("Thermal Asset", material.ThermalAsset);
    }
  }
}
