using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public class AnalyzePhysicalAsset : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("ec93f8e0-d2af-4a44-a040-89a7c40b9fc7");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public AnalyzePhysicalAsset() : base(
      name: "Analyze Physical Asset",
      nickname: "A-PHAST",
      description: "Analyze given physical asset",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(
        param: new Parameters.PhysicalAsset(),
        name: "Physical Asset",
        nickname: "PA",
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
        param: new Parameters.Param_Enum<Types.StructuralAssetClass>(),
        name: "Type",
        nickname: "T",
        description: "Physical asset type",
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Subclass",
        nickname: "SC",
        description: "Physical asset subclass",
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Source",
        nickname: "S",
        description: "Physical asset source",
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Source URL",
        nickname: "SU",
        description: "Physical asset source url",
        access: GH_ParamAccess.item
        );

      // behaviour
      pManager.AddParameter(
        param: new Parameters.Param_Enum<Types.StructuralBehavior>(),
        name: "Behaviour",
        nickname: "B",
        description: "Physical asset behaviour",
        access: GH_ParamAccess.item
        );

      // basic thermal
      pManager.AddNumberParameter(
        name: "Thermal Expansion Coefficient X",
        nickname: "TECX",
        description: "The only, X or 1 component of thermal expansion coefficient (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Thermal Expansion Coefficient Y",
        nickname: "TECY",
        description: "Y or 2 component of thermal expansion coefficient (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Thermal Expansion Coefficient Z",
        nickname: "TECZ",
        description: "Z component of thermal expansion coefficient (depending on behaviour)",
        access: GH_ParamAccess.item
        );

      // mechanical
      pManager.AddNumberParameter(
        name: "Youngs Modulus X",
        nickname: "YMX",
        description: "The only, X, or 1 component of young's modulus (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Youngs Modulus Y",
        nickname: "YMY",
        description: "Y, or 1 component of young's modulus (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Youngs Modulus Z",
        nickname: "YMZ",
        description: "Z component of young's modulus (depending on behaviour)",
        access: GH_ParamAccess.item
        );

      pManager.AddNumberParameter(
        name: "Poissons Ratio X",
        nickname: "PRX",
        description: "The only, X, or 12 component of poisson's ratio (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Poissons Ratio Y",
        nickname: "PRY",
        description: "Y, or 23 component of poisson's ratio (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Poissons Ratio Z",
        nickname: "PRZ",
        description: "Z component of poisson's ratio (depending on behaviour)",
        access: GH_ParamAccess.item
        );

      pManager.AddNumberParameter(
        name: "Shear Modulus X",
        nickname: "SMX",
        description: "The only, X, or 12 component of poisson's ratio (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Shear Modulus Y",
        nickname: "SMY",
        description: "Y component of poisson's ratio (depending on behaviour)",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Shear Modulus Z",
        nickname: "SMZ",
        description: "Z component of poisson's ratio (depending on behaviour)",
        access: GH_ParamAccess.item
        );

      pManager.AddNumberParameter(
        name: "Density",
        nickname: "D",
        description: "Physical asset density",
        access: GH_ParamAccess.item
        );

      // concrete
      pManager.AddNumberParameter(
        name: "Concrete Compression",
        nickname: "CC",
        description: "Physical asset concrete compression",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Concrete Shear Strength Modification",
        nickname: "CSSM",
        description: "Physical asset concrete shear strength modification",
        access: GH_ParamAccess.item
        );
      pManager.AddBooleanParameter(
        name: "Concrete Lightweight",
        nickname: "CL",
        description: "Physical asset lightweight concrete",
        access: GH_ParamAccess.item
        );

      // wood
      pManager.AddTextParameter(
        name: "Wood Species",
        nickname: "WS",
        description: "Physical asset wood species",
        access: GH_ParamAccess.item
        );
      pManager.AddTextParameter(
        name: "Wood Strength Grade",
        nickname: "WSG",
        description: "Physical asset wood strength grade",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Wood Bending",
        nickname: "WB",
        description: "Physical asset wood bending strength",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Wood Compression Parallel to Grain",
        nickname: "WCLG",
        description: "Physical asset wood compression parallel to grain",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Wood Compression Perpendicular to Grain",
        nickname: "WCPG",
        description: "Physical asset wood compression perpendicular to grain",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Wood Shear Parallel to Grain",
        nickname: "WSLG",
        description: "Physical asset wood shear parallel to grain",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Wood Tension Perpendicular to Grain",
        nickname: "WTPG",
        description: "Physical asset wood tension perpendicular to grain",
        access: GH_ParamAccess.item
        );

      // shared
      pManager.AddNumberParameter(
        name: "Yield Strength",
        nickname: "YS",
        description: "Physical asset yield strength",
        access: GH_ParamAccess.item
        );
      pManager.AddNumberParameter(
        name: "Tensile Strength",
        nickname: "TS",
        description: "Physical asset tensile strength",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.PropertySetElement psetElement = default;
      if (!DA.GetData("Physical Asset", ref psetElement))
        return;

      var structAsset = psetElement.GetStructuralAsset();

      // information
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_NAME, "Name");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_DESCRIPTION, "Description");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_KEYWORDS, "Keywords");
      PipeHostParameter<Types.StructuralAssetClass>(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_CLASS, "Type");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, "Subclass");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, "Source");
      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, "Source URL");

      // behaviour
      PipeHostParameter<Types.StructuralBehavior>(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR, "Behaviour");

      // basic thermal
      DA.SetData("Thermal Expansion Coefficient X", structAsset?.ThermalExpansionCoefficient.X);
      DA.SetData("Thermal Expansion Coefficient Y", structAsset?.ThermalExpansionCoefficient.Y);
      DA.SetData("Thermal Expansion Coefficient Z", structAsset?.ThermalExpansionCoefficient.Z);

      // mechanical
      DA.SetData("Youngs Modulus X", structAsset?.YoungModulus.X);
      DA.SetData("Youngs Modulus Y", structAsset?.YoungModulus.Y);
      DA.SetData("Youngs Modulus Z", structAsset?.YoungModulus.Z);

      DA.SetData("Poissons Ratio X", structAsset?.PoissonRatio.X);
      DA.SetData("Poissons Ratio Y", structAsset?.PoissonRatio.Y);
      DA.SetData("Poissons Ratio Z", structAsset?.PoissonRatio.Z);

      DA.SetData("Shear Modulus X", structAsset?.ShearModulus.X);
      DA.SetData("Shear Modulus Y", structAsset?.ShearModulus.Y);
      DA.SetData("Shear Modulus Z", structAsset?.ShearModulus.Z);

      DA.SetData("Density", structAsset?.Density);

      // concrete
      DA.SetData("Concrete Compression", structAsset?.ConcreteCompression);
      DA.SetData("Concrete Shear Strength Modification", structAsset?.ConcreteShearStrengthReduction);
      DA.SetData("Concrete Lightweight", structAsset?.Lightweight);

      // metal
      // API: Values are not represented in the material editor
      //DA.SetData("", structAsset?.MetalReductionFactor);
      //DA.SetData("", structAsset?.MetalResistanceCalculationStrength);

      // wood
      DA.SetData("Wood Species", structAsset?.WoodSpecies);
      DA.SetData("Wood Strength Grade", structAsset?.WoodGrade);
      DA.SetData("Wood Bending", structAsset?.WoodBendingStrength);
      DA.SetData("Wood Compression Parallel to Grain", structAsset?.WoodParallelCompressionStrength);
      DA.SetData("Wood Compression Perpendicular to Grain", structAsset?.WoodPerpendicularCompressionStrength);
      DA.SetData("Wood Shear Parallel to Grain", structAsset?.WoodParallelShearStrength);
      DA.SetData("Wood Tension Perpendicular to Grain", structAsset?.WoodPerpendicularShearStrength);
      // API: Values are not represented in the API
      //DA.SetData("Tension Parallel to Grain", );
      //DA.SetData("Tension Perpendicular to Grain", );
      //DA.SetData("Average Modulus", );
      //DA.SetData("Construction", );

      // shared
      DA.SetData("Yield Strength", structAsset?.MinimumYieldStress);
      DA.SetData("Tensile Strength", structAsset?.MinimumTensileStrength);
    }
  }
}
