using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public static class StructuralAssetSchema
  {
    public static IGH_Param SchemaTypeParam
    {
      get
      {
        return new Parameters.StructuralAsset()
        {
          Name = "Physical Asset",
          NickName = "PA",
          Description = string.Empty,
          Access = GH_ParamAccess.item
        };
      }
    }

    public static IGH_Param[] SchemaParams
    {
      get
      {
        return new IGH_Param[] {
          // information
          new Param_String() {
            Name = "Name",
            NickName = "N",
            Description=  string.Empty,
          },
          new Parameters.Param_Enum<Types.StructuralAssetClass>() {
            Name = "Type",
            NickName =  "T",
            Description = "Physical asset type",
          },
          new Param_String() {
            Name = "Subclass",
            NickName =  "SC",
            Description = "Physical asset subclass",
            Optional = true
          },
          new Param_String() {
          Name = "Description",
            NickName =  "D",
            Description = string.Empty,
            Optional = true
          },
          new Param_String() {
            Name = "Keywords",
            NickName =  "K",
            Description = string.Empty,
            Optional = true
          },
          new Param_String() {
            Name = "Source",
            NickName =  "S",
            Description = "Physical asset source",
            Optional = true
          },
          new Param_String() {
            Name = "Source URL",
            NickName =  "SU",
            Description = "Physical asset source url",
            Optional = true
          },

          // behaviour
          new Parameters.Param_Enum<Types.StructuralBehavior>() {
            Name = "Behaviour",
            NickName =  "B",
            Description = "Physical asset behaviour",
            Optional = true
          },

          // basic thermal
          new Param_Number() {
            Name = "Thermal Expansion Coefficient X",
            NickName =  "TECX",
            Description = "The only, X or 1 component of thermal expansion coefficient (depending on behaviour)",
            Optional = true
            },
          new Param_Number() {
            Name = "Thermal Expansion Coefficient Y",
            NickName =  "TECY",
            Description = "Y or 2 component of thermal expansion coefficient (depending on behaviour)",
            Optional = true
            },
          new Param_Number() {
            Name = "Thermal Expansion Coefficient Z",
            NickName =  "TECZ",
            Description = "Z component of thermal expansion coefficient (depending on behaviour)",
            Optional = true
          },

          // mechanical
          new Param_Number() {
            Name = "Youngs Modulus X",
            NickName =  "YMX",
            Description = "The only, X, or 1 component of young's modulus (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Youngs Modulus Y",
            NickName =  "YMY",
            Description = "Y, or 1 component of young's modulus (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Youngs Modulus Z",
            NickName =  "YMZ",
            Description = "Z component of young's modulus (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Poissons Ratio X",
            NickName =  "PRX",
            Description = "The only, X, or 12 component of poisson's ratio (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Poissons Ratio Y",
            NickName =  "PRY",
            Description = "Y, or 23 component of poisson's ratio (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Poissons Ratio Z",
            NickName =  "PRZ",
            Description = "Z component of poisson's ratio (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Shear Modulus X",
            NickName =  "SMX",
            Description = "The only, X, or 12 component of poisson's ratio (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Shear Modulus Y",
            NickName =  "SMY",
            Description = "Y component of poisson's ratio (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Shear Modulus Z",
            NickName =  "SMZ",
            Description = "Z component of poisson's ratio (depending on behaviour)",
            Optional = true
          },
          new Param_Number() {
            Name = "Density",
            NickName =  "D",
            Description = "Physical asset density",
            Optional = true
          },

          // concrete
          new Param_Number() {
            Name = "Concrete Compression",
            NickName =  "CC",
            Description = "Physical asset concrete compression",
            Optional = true
          },
          new Param_Number() {
            Name = "Concrete Shear Strength Modification",
            NickName =  "CSSM",
            Description = "Physical asset concrete shear strength modification",
            Optional = true
          },
          new Param_Number() {
            Name = "Concrete Lightweight",
            NickName =  "CL",
            Description = "Physical asset lightweight concrete",
            Optional = true
        },

          // wood
          new Param_String() {
            Name = "Wood Species",
            NickName =  "WS",
            Description = "Physical asset wood species",
            Optional = true
          },
          new Param_String() {
            Name = "Wood Strength Grade",
            NickName =  "WSG",
            Description = "Physical asset wood strength grade",
            Optional = true
          },
          new Param_Number() {
            Name = "Wood Bending",
            NickName =  "WB",
            Description = "Physical asset wood bending strength",
            Optional = true
          },
          new Param_Number() {
            Name = "Wood Compression Parallel to Grain",
            NickName =  "WCLG",
            Description = "Physical asset wood compression parallel to grain",
            Optional = true
          },
          new Param_Number() {
            Name = "Wood Compression Perpendicular to Grain",
            NickName =  "WCPG",
            Description = "Physical asset wood compression perpendicular to grain",
            Optional = true
          },
          new Param_Number() {
            Name = "Wood Shear Parallel to Grain",
            NickName =  "WSLG",
            Description = "Physical asset wood shear parallel to grain",
            Optional = true
          },
          new Param_Number() {
            Name = "Wood Tension Perpendicular to Grain",
            NickName =  "WTPG",
            Description = "Physical asset wood tension perpendicular to grain",
            Optional = true
          },

          // shared
          new Param_Number() {
            Name = "Yield Strength",
            NickName =  "YS",
            Description = "Physical asset yield strength",
            Optional = true
          },
          new Param_Number() {
            Name = "Tensile Strength",
            NickName =  "TS",
            Description = "Physical asset tensile strength",
            Optional = true
          }
        };
      }
    }

    public static void SetAssetParamsFromInput(IGH_DataAccess DA, DB.StructuralAsset structAsset)
    {
      // information
      // asset name and class can not be set

      string subclass = default;
      if (DA.GetData("Subclass", ref subclass))
        structAsset.SubClass = subclass;

      // behaviour
      DB.StructuralBehavior behaviour = default;
      if (DA.GetData("Behaviour", ref behaviour))
        structAsset.Behavior = behaviour;

      // basic thermal
      double tecx = default, tecy = default, tecz = default;
      DA.GetData("Thermal Expansion Coefficient Z", ref tecz);
      DA.GetData("Thermal Expansion Coefficient Y", ref tecy);
      if (DA.GetData("Thermal Expansion Coefficient X", ref tecx))
      {
        if (structAsset.StructuralAssetClass == DB.StructuralAssetClass.Wood || structAsset.Behavior == DB.StructuralBehavior.Isotropic)
          structAsset.SetThermalExpansionCoefficient(tecx);
        else
          structAsset.ThermalExpansionCoefficient = new DB.XYZ(tecx, tecy, tecz);
      }

      // mechanical
      double ymx = default, ymy = default, ymz = default;
      DA.GetData("Youngs Modulus Z", ref ymz);
      DA.GetData("Youngs Modulus Y", ref ymy);
      if (DA.GetData("Youngs Modulus X", ref ymx))
      {
        if (structAsset.StructuralAssetClass == DB.StructuralAssetClass.Wood || structAsset.Behavior == DB.StructuralBehavior.Isotropic)
          structAsset.SetYoungModulus(ymx);
        else
          structAsset.YoungModulus = new DB.XYZ(ymx, ymy, ymz);
      }

      double prx = default, pry = default, prz = default;
      DA.GetData("Poissons Ratio Z", ref prz);
      DA.GetData("Poissons Ratio Y", ref pry);
      if (DA.GetData("Poissons Ratio X", ref prx))
      {
        if (structAsset.StructuralAssetClass == DB.StructuralAssetClass.Wood || structAsset.Behavior == DB.StructuralBehavior.Isotropic)
          structAsset.SetPoissonRatio(prx);
        else
          structAsset.PoissonRatio = new DB.XYZ(prx, pry, prz);
      }

      double smx = default, smy = default, smz = default;
      DA.GetData("Shear Modulus Z", ref smz);
      DA.GetData("Shear Modulus Y", ref smy);
      if (DA.GetData("Shear Modulus X", ref smx))
      {
        if (structAsset.StructuralAssetClass == DB.StructuralAssetClass.Wood || structAsset.Behavior == DB.StructuralBehavior.Isotropic)
          structAsset.SetShearModulus(smx);
        else
          structAsset.ShearModulus = new DB.XYZ(smx, smy, smz);
      }

      double density = default;
      if (DA.GetData("Density", ref density))
        structAsset.Density = density;

      //// concrete
      double concomp = default;
      if (DA.GetData("Concrete Compression", ref concomp))
        structAsset.ConcreteCompression = concomp;

      double concshearmod = default;
      if (DA.GetData("Concrete Shear Strength Modification", ref concshearmod))
        structAsset.ConcreteShearStrengthReduction = concshearmod;

      bool conclightweight = default;
      if (DA.GetData("Concrete Lightweight", ref conclightweight))
        structAsset.Lightweight = conclightweight;

      // metal
      // API: Values are not represented in the material editor
      //DA.SetData("", structAsset?.MetalReductionFactor);
      //DA.SetData("", structAsset?.MetalResistanceCalculationStrength);

      //// wood
      string woodSpecies = default;
      if (DA.GetData("Wood Species", ref woodSpecies))
        structAsset.WoodSpecies = woodSpecies;

      string woodStrength = default;
      if (DA.GetData("Wood Strength Grade", ref woodStrength))
        structAsset.WoodGrade = woodStrength;

      double woodBending = default;
      if (DA.GetData("Wood Bending", ref woodBending))
        structAsset.WoodBendingStrength = woodBending;

      double woodParComp = default;
      if (DA.GetData("Wood Compression Parallel to Grain", ref woodParComp))
        structAsset.WoodParallelCompressionStrength = woodParComp;

      double woodPerComp = default;
      if (DA.GetData("Wood Compression Perpendicular to Grain", ref woodPerComp))
        structAsset.WoodPerpendicularCompressionStrength = woodPerComp;

      double woodParShear = default;
      if (DA.GetData("Wood Shear Parallel to Grain", ref woodParShear))
        structAsset.WoodParallelShearStrength = woodParShear;

      double woodPerShear = default;
      if (DA.GetData("Wood Tension Perpendicular to Grain", ref woodPerShear))
        structAsset.WoodPerpendicularShearStrength = woodPerShear;

      //// API: Values are not represented in the API
      ////DA.SetData("Tension Parallel to Grain", );
      ////DA.SetData("Tension Perpendicular to Grain", );
      ////DA.SetData("Average Modulus", );
      ////DA.SetData("Construction", );

      // shared
      double yieldStrength = default;
      if (DA.GetData("Yield Strength", ref yieldStrength))
        structAsset.MinimumYieldStress = yieldStrength;

      double tensileStrength = default;
      if (DA.GetData("Tensile Strength", ref tensileStrength))
        structAsset.MinimumTensileStrength = tensileStrength;
    }

    public static void SetPropertySetElementParamsFromInput(IGH_DataAccess DA, DB.PropertySetElement psetElement)
    {
      string desc = default;
      if (DA.GetData("Description", ref desc))
        psetElement.get_Parameter(DB.BuiltInParameter.PROPERTY_SET_DESCRIPTION)?.Set(desc);

      string keywords = default;
      if (DA.GetData("Keywords", ref keywords))
        psetElement.get_Parameter(DB.BuiltInParameter.PROPERTY_SET_KEYWORDS)?.Set(keywords);

      string source = default;
      if (DA.GetData("Source", ref source))
        psetElement.get_Parameter(DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE)?.Set(source);

      string sourceUrl = default;
      if (DA.GetData("Source URL", ref sourceUrl))
        psetElement.get_Parameter(DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL)?.Set(sourceUrl);
    }

    public static void SetAssetParamsFromInput(IGH_DataAccess DA, DB.PropertySetElement psetElement)
    {
      var structAsset = psetElement.GetStructuralAsset();

      if (structAsset is null)
        return;

      SetAssetParamsFromInput(DA, structAsset);
      SetPropertySetElementParamsFromInput(DA, psetElement);

      psetElement.SetStructuralAsset(structAsset);
    }
  }

  public class CreateStructuralAsset : DocumentComponent
  {
    public override Guid ComponentGuid =>
      new Guid("af2678c8-2a53-4056-9399-5a06dd9ac14d");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override ParamDefinition[] Inputs => GetInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.FromParam(StructuralAssetSchema.SchemaTypeParam)
    };

    public CreateStructuralAsset() : base(
      name: "Create Physical Asset",
      nickname: "C-PHAST",
      description: "Create a new instance of physical asset inside document",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    private ParamDefinition[] GetInputs()
    {
      var inputs = new List<ParamDefinition>()
      {
        ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      };

      foreach (IGH_Param param in StructuralAssetSchema.SchemaParams)
        inputs.Add(ParamDefinition.FromParam(param));

      return inputs.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      // get required input (name, class)
      string name = default;
      if (!DA.GetData("Name", ref name))
        return;

      DB.StructuralAssetClass assetClass = default;
      if (!DA.GetData("Type", ref assetClass))
        return;

      var psetElements = new DB.FilteredElementCollector(doc)
                               .OfClass(typeof(DB.PropertySetElement))
                               .WhereElementIsNotElementType()
                               .ToElements()
                               .Cast<DB.PropertySetElement>();

      using (var transaction = NewTransaction(doc))
      {
        transaction.Start();

        // delete existing if asset with same name already exists
        DB.PropertySetElement psetElement =
          psetElements.Where(x => x.GetStructuralAsset()?.Name == name)
                      .FirstOrDefault();
        if (psetElement != null)
          doc.Delete(psetElement.Id);

        try
        {
          // creaet asset from input data
          var structAsset = new DB.StructuralAsset(name, assetClass);
          StructuralAssetSchema.SetAssetParamsFromInput(DA, structAsset);
          // set the asset on psetelement
          psetElement = DB.PropertySetElement.Create(doc, structAsset);
          // set other properties that are not accessible through the schema
          StructuralAssetSchema.SetPropertySetElementParamsFromInput(DA, psetElement);
        }
        catch (Exception ex)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Revit API Error | {ex.Message}");
        }

        // send the new asset to output
        DA.SetData(
          StructuralAssetSchema.SchemaTypeParam.Name,
          psetElement
        );

        transaction.Commit();
      }
    }
  }

  public class ModifyStructuralAsset : TransactionalComponent
  {
    public override Guid ComponentGuid =>
      new Guid("67a74d31-0878-4b48-8efb-f4ca97389f74");

    // modifying and setting a new asset of a DB.PropertySetElement
    // return an "internal API error" in Revit
    // hiding the modify component for now
#if DEBUG
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#else
    public override GH_Exposure Exposure => GH_Exposure.quinary;
#endif

    protected override ParamDefinition[] Inputs => GetInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.FromParam(StructuralAssetSchema.SchemaTypeParam)
    };

    public ModifyStructuralAsset() : base(
      name: "Modify Physical Asset",
      nickname: "M-PHAST",
      description: "Modify given physical asset",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    private ParamDefinition[] GetInputs()
    {
      var inputs = new List<ParamDefinition>()
      {
        ParamDefinition.FromParam(StructuralAssetSchema.SchemaTypeParam)
      };

      foreach (IGH_Param param in StructuralAssetSchema.SchemaParams)
        if (param.Name != "Name" && param.Name != "Type")
          inputs.Add(ParamDefinition.FromParam(param));

      return inputs.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input structural asset
      DB.PropertySetElement psetElement = default;
      if (!DA.GetData(StructuralAssetSchema.SchemaTypeParam.Name, ref psetElement))
        return;

      var doc = Revit.ActiveDBDocument;
      using (var transaction = NewTransaction(doc))
      {
        transaction.Start();

        // update the asset properties from input data
        try
        {
          StructuralAssetSchema.SetAssetParamsFromInput(DA, psetElement);
        }
        catch (Exception ex)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Revit API Error | {ex.Message}");
        }


        // send the modified asset to output
        DA.SetData(
          StructuralAssetSchema.SchemaTypeParam.Name,
          psetElement
        );

        transaction.Commit();
      }
    }
  }

  public class AnalyzeStructuralAsset : AnalysisComponent
  {
    public override Guid ComponentGuid =>
      new Guid("ec93f8e0-d2af-4a44-a040-89a7c40b9fc7");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public AnalyzeStructuralAsset() : base(
      name: "Analyze Physical Asset",
      nickname: "A-PHAST",
      description: "Analyze given physical asset",
      category: "Revit",
      subCategory: "Material"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
      => pManager.AddParameter(StructuralAssetSchema.SchemaTypeParam);

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      foreach (IGH_Param param in StructuralAssetSchema.SchemaParams)
        pManager.AddParameter(param);
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
