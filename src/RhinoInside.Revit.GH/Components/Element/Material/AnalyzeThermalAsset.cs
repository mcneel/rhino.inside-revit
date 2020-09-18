using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class AnalyzeThermalAsset : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("c3be363d-c01d-4cf3-b8d2-c345734ae66d");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public AnalyzeThermalAsset() : base(
      name: "Analyze Thermal Asset",
      nickname: "A-THAST",
      description: "Analyze given thermal asset",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.ThermalAsset(),
        name: "Thermal Asset",
        nickname: "TA",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      // information
      pManager.AddTextParameter(
        name: "Name",
        nickname: "N",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Description",
        nickname: "D",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Keywords",
        nickname: "K",
        description: string.Empty,
        access: GH_ParamAccess.item
        );
      pManager.AddParameter(
        param: new Parameters.Param_Enum<Types.ThermalMaterialType>(),
        name: "Type",
        nickname: "T",
        description: "Thermal asset material asset type",
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Subclass",
        nickname: "SC",
        description: "Thermal asset subclass",
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Source",
        nickname: "S",
        description: "Thermal asset source",
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Source URL",
        nickname: "SU",
        description: "Thermal asset source url",
        access: GH_ParamAccess.item
        );

      // behaviour
      pManager.AddParameter(
        param: new Parameters.Param_Enum<Types.StructuralBehavior>(),
        name: "Behaviour",
        nickname: "B",
        description: "Thermal asset behaviour",
        access: GH_ParamAccess.item
        );

      // properties
      pManager.AddBooleanParameter(
        name: "Transmits Light",
        nickname: "TL",
        description: "Thermal asset transmits light",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Thermal Conductivity",
        nickname: "TC",
        description: "Thermal asset thermal conductivity",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Specific Heat",
        nickname: "SH",
        description: "Thermal asset specific heat",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Density",
        nickname: "D",
        description: "Thermal asset density",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Emissivity",
        nickname: "E",
        description: "Thermal asset emissivity",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Permeability",
        nickname: "PE",
        description: "Thermal asset permeability",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Porosity",
        nickname: "PO",
        description: "Thermal asset porosity",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Reflectivity",
        nickname: "R",
        description: "Thermal asset reflectivity",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Gas Viscosity",
        nickname: "GV",
        description: "Thermal asset gas viscosity",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Electrical Resistivity",
        nickname: "ER",
        description: "Thermal asset electrical resistivity",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Liquid Viscosity",
        nickname: "LV",
        description: "Thermal asset liquid viscosity",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Specific Heat Of Vaporization",
        nickname: "SHV",
        description: "Thermal asset specific heat of vaporization",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Vapor Pressure",
        nickname: "VP",
        description: "Thermal asset vapor pressure",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Compressibility",
        nickname: "C",
        description: "Thermal asset compressibility",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.PropertySetElement psetElement = default;
      if (!DA.GetData("Thermal Asset", ref psetElement))
        return;

      var thermalAsset = psetElement.GetThermalAsset();

      // information
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_NAME, "Name");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_DESCRIPTION, "Description");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_KEYWORDS, "Keywords");
      DA.SetData("Type", thermalAsset?.ThermalMaterialType);
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, "Subclass");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, "Source");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, "Source URL");

      // behaviour
      PipeHostParameter<Types.StructuralBehavior>(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR, "Behaviour");

      DA.SetData("Transmits Light", thermalAsset?.TransmitsLight);
      DA.SetData("Thermal Conductivity", thermalAsset?.ThermalConductivity);
      DA.SetData("Specific Heat", thermalAsset?.SpecificHeat);
      DA.SetData("Density", thermalAsset?.Density);
      DA.SetData("Emissivity", thermalAsset?.Emissivity);
      DA.SetData("Permeability", thermalAsset?.Permeability);
      DA.SetData("Porosity", thermalAsset?.Porosity);
      DA.SetData("Reflectivity", thermalAsset?.Reflectivity);
      DA.SetData("Gas Viscosity", thermalAsset?.GasViscosity);
      DA.SetData("Electrical Resistivity", thermalAsset?.ElectricalResistivity);
      DA.SetData("Liquid Viscosity", thermalAsset?.LiquidViscosity);
      DA.SetData("Specific Heat Of Vaporization", thermalAsset?.SpecificHeatOfVaporization);
      DA.SetData("Vapor Pressure", thermalAsset?.VaporPressure);
      DA.SetData("Compressibility", thermalAsset?.Compressibility);
    }
  }
}
