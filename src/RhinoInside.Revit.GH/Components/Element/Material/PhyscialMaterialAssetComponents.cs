using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.System.Drawing;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.Convert.Geometry;
using System.Reflection;
using System.Linq.Expressions;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public abstract class BasePhysicalAssetComponent<T>
    : TransactionalComponent where T : PhysicalMaterialData, new()
  {
    protected AssetGHComponent ComponentInfo
    {
      get
      {
        if (_compInfo is null)
        {
          _compInfo = _assetData.GetGHComponentInfo();
          if (_compInfo is null)
            throw new Exception("Data type does not have component info");
        }
        return _compInfo;
      }
    }

    public BasePhysicalAssetComponent() : base("", "", "", "Revit", "Material") { }

    private readonly T _assetData = new T();
    private AssetGHComponent _compInfo;

    protected ParamDefinition[] GetAssetDataAsInputs(bool skipUnchangable = false)
    {
      List<ParamDefinition> inputs = new List<ParamDefinition>();

      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        if (skipUnchangable && paramInfo.Unchangable)
          continue;

        var param = (IGH_Param) Activator.CreateInstance(paramInfo.ParamType);
        param.Name = paramInfo.Name;
        param.NickName = paramInfo.NickName;
        param.Description = paramInfo.Description;
        param.Access = paramInfo.ParamAccess;
        param.Optional = paramInfo.Optional;

        inputs.Add(ParamDefinition.FromParam(param));
      }

      return inputs.ToArray();
    }

    protected ParamDefinition[] GetAssetDataAsOutputs()
    {
      List<ParamDefinition> outputs = new List<ParamDefinition>();

      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        var param = (IGH_Param) Activator.CreateInstance(paramInfo.ParamType);
        param.Name = paramInfo.Name;
        param.NickName = paramInfo.NickName;
        param.Description = paramInfo.Description;
        param.Access = paramInfo.ParamAccess;

        outputs.Add(ParamDefinition.FromParam(param));
      }

      return outputs.ToArray();
    }

    protected bool MatchesPhysicalAssetType(DB.PropertySetElement psetElement)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
        foreach (var builtInPropInfo in
          _assetData.GetAPIAssetBuiltInPropertyInfos(assetPropInfo))
          if (!builtInPropInfo.Generic
                && psetElement.get_Parameter(builtInPropInfo.ParamId) is null)
            return false;
      return true;
    }

    protected T CreateAssetDataFromInputs(IGH_DataAccess DA)
    {
      // instantiate an output object
      var output = new T();

      // set its properties
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        IGH_Goo inputGHType = default;
        var paramIdx = Params.IndexOfInputParam(paramInfo.Name);
        if (paramIdx < 0)
          continue;

        bool hasInput = DA.GetData(paramInfo.Name, ref inputGHType);
        if (hasInput)
        {
          object inputValue = inputGHType.ScriptVariable();

          var valueRange = _assetData.GetAPIAssetPropertyValueRange(assetPropInfo);
          if (valueRange != null)
            inputValue = VerifyInputValue(paramInfo.Name, inputValue, valueRange);

          assetPropInfo.SetValue(output, inputValue);
          output.Mark(assetPropInfo.Name);
        }
      }

      return output;
    }

    protected void UpdatePropertySetElementFromData(DB.PropertySetElement psetElement, T assetData)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        // skip name because it is already set
        if (assetPropInfo.Name == "Name")
          continue;

        bool hasValue = assetData.IsMarked(assetPropInfo.Name);
        if (hasValue)
        {
          object inputValue = assetPropInfo.GetValue(assetData);

          foreach (var builtInPropInfo in
            _assetData.GetAPIAssetBuiltInPropertyInfos(assetPropInfo))
            psetElement.SetParameter(builtInPropInfo.ParamId, inputValue);
        }
      }
    }

    #region Asset Utility Methods
    public object
    VerifyInputValue(string inputName, object inputValue, APIAssetPropValueRange valueRangeInfo)
    {
      switch (inputValue)
      {
        case double dblVal:
          // check double max
          if (valueRangeInfo.Min != double.NaN && dblVal < valueRangeInfo.Min)
          {
            AddRuntimeMessage(
              GH_RuntimeMessageLevel.Warning,
              $"\"{inputName}\" value is smaller than the allowed " +
              $"minimum \"{valueRangeInfo.Min}\". Minimum value is " +
              "used instead to avoid errors"
              );
            return (object) valueRangeInfo.Min;
          }
          else if (valueRangeInfo.Max != double.NaN && dblVal > valueRangeInfo.Max)
          {
            AddRuntimeMessage(
              GH_RuntimeMessageLevel.Warning,
              $"\"{inputName}\" value is larger than the allowed " +
              $"maximum of \"{valueRangeInfo.Max}\". Maximum value is " +
              "used instead to avoid errors"
              );
            return (object) valueRangeInfo.Max;
          }

          break;
      }

      // if no correction has been done, return the original value
      return inputValue;
    }
    #endregion
  }

  public class CreateStructuralAsset : BasePhysicalAssetComponent<StructuralAssetData>
  {
    public override Guid ComponentGuid =>
      new Guid("af2678c8-2a53-4056-9399-5a06dd9ac14d");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public CreateStructuralAsset() : base()
    {
      Name = $"Create {ComponentInfo.Name}";
      NickName = $"C-{ComponentInfo.NickName}";
      Description = $"Create a new instance of {ComponentInfo.Description}";
    }

    protected override ParamDefinition[] Inputs => GetAssetDataAsInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.StructuralAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get required input (name, class)
      string name = default;
      if (!DA.GetData("Name", ref name))
        return;

      DB.StructuralAssetClass assetClass = default;
      if (!DA.GetData("Type", ref assetClass))
        return;

      var doc = Revit.ActiveDBDocument;
      using (var transaction = NewTransaction(doc))
      {
        try
        {
          // check naming conflicts with other asset types
          DB.PropertySetElement psetElement = doc.FindPropertySetElement(name);
          if (psetElement != null && psetElement.Id != DB.ElementId.InvalidElementId)
            if (!MatchesPhysicalAssetType(psetElement))
            {
              AddRuntimeMessage(
                GH_RuntimeMessageLevel.Error,
                $"Thermal asset with same name exists already. Use a different name for this asset"
              );
              return;
            }

          transaction.Start();

          // delete existing matching psetelement
          if (psetElement != null && psetElement.Id != DB.ElementId.InvalidElementId)
            doc.Delete(psetElement.Id);

          // creaet asset from input data
          var structAsset = new DB.StructuralAsset(name, assetClass);
          // set the asset on psetelement
          psetElement = DB.PropertySetElement.Create(doc, structAsset);

          // grab asset data from inputs
          var assetData = CreateAssetDataFromInputs(DA);
          UpdatePropertySetElementFromData(psetElement, assetData);

          // send the new asset to output
          DA.SetData(
            ComponentInfo.Name,
            psetElement
          );
        }
        catch (Exception ex)
        {
          transaction.RollBack();
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Revit API Error | {ex.Message}");
        }

        transaction.Commit();
      }
    }
  }

  //  public class ModifyStructuralAsset : TransactionalComponent
  //  {
  //    public override Guid ComponentGuid =>
  //      new Guid("67a74d31-0878-4b48-8efb-f4ca97389f74");

  //    // modifying and setting a new asset of a DB.PropertySetElement
  //    // return an "internal API error" in Revit
  //    // hiding the modify component for now
  //    // possible solution is to avoid using the DB.StructuralAsset API and
  //    // implement the schema property names manually. These assets however
  //    // are not widely used in the industry so we'll wait for customer feedback
  //#if DEBUG
  //    public override GH_Exposure Exposure => GH_Exposure.hidden;
  //#else
  //    public override GH_Exposure Exposure => GH_Exposure.quinary;
  //#endif

  //    protected override ParamDefinition[] Inputs => GetInputs();
  //    protected override ParamDefinition[] Outputs => new ParamDefinition[]
  //    {
  //      ParamDefinition.FromParam(StructuralAssetSchema.SchemaTypeParam)
  //    };

  //    public ModifyStructuralAsset() : base(
  //      name: "Modify Physical Asset",
  //      nickname: "M-PHAST",
  //      description: "Modify given physical asset",
  //      category: "Revit",
  //      subCategory: "Material"
  //    )
  //    {
  //    }

  //    private ParamDefinition[] GetInputs()
  //    {
  //      var inputs = new List<ParamDefinition>()
  //      {
  //        ParamDefinition.FromParam(StructuralAssetSchema.SchemaTypeParam)
  //      };

  //      foreach (IGH_Param param in StructuralAssetSchema.SchemaParams)
  //        if (param.Name != "Name" && param.Name != "Type")
  //          inputs.Add(ParamDefinition.FromParam(param));

  //      return inputs.ToArray();
  //    }

  //    protected override void TrySolveInstance(IGH_DataAccess DA)
  //    {
  //      // get input structural asset
  //      DB.PropertySetElement psetElement = default;
  //      if (!DA.GetData(StructuralAssetSchema.SchemaTypeParam.Name, ref psetElement))
  //        return;

  //      var doc = Revit.ActiveDBDocument;
  //      using (var transaction = NewTransaction(doc))
  //      {
  //        transaction.Start();

  //        // update the asset properties from input data
  //        try
  //        {
  //          StructuralAssetSchema.SetAssetParamsFromInput(DA, psetElement);
  //        }
  //        catch (Exception ex)
  //        {
  //          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Revit API Error | {ex.Message}");
  //        }


  //        // send the modified asset to output
  //        DA.SetData(
  //          StructuralAssetSchema.SchemaTypeParam.Name,
  //          psetElement
  //        );

  //        transaction.Commit();
  //      }
  //    }
  //  }

  //  public class AnalyzeStructuralAsset : AnalysisComponent
  //  {
  //    public override Guid ComponentGuid =>
  //      new Guid("ec93f8e0-d2af-4a44-a040-89a7c40b9fc7");
  //    public override GH_Exposure Exposure => GH_Exposure.quinary;

  //    public AnalyzeStructuralAsset() : base(
  //      name: "Analyze Physical Asset",
  //      nickname: "A-PHAST",
  //      description: "Analyze given physical asset",
  //      category: "Revit",
  //      subCategory: "Material"
  //    )
  //    {
  //    }

  //    protected override void RegisterInputParams(GH_InputParamManager pManager)
  //      => pManager.AddParameter(StructuralAssetSchema.SchemaTypeParam);

  //    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  //    {
  //      foreach (IGH_Param param in StructuralAssetSchema.SchemaParams)
  //        pManager.AddParameter(param);
  //    }

  //    protected override void TrySolveInstance(IGH_DataAccess DA)
  //    {
  //      DB.PropertySetElement psetElement = default;
  //      if (!DA.GetData("Physical Asset", ref psetElement))
  //        return;

  //      var structAsset = psetElement.GetStructuralAsset();

  //      // information
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_NAME, "Name");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_DESCRIPTION, "Description");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_KEYWORDS, "Keywords");
  //      PipeHostParameter<Types.StructuralAssetClass>(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_CLASS, "Type");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, "Subclass");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, "Source");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, "Source URL");

  //      // behaviour
  //      PipeHostParameter<Types.StructuralBehavior>(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR, "Behaviour");

  //      // basic thermal
  //      DA.SetData("Thermal Expansion Coefficient X", structAsset?.ThermalExpansionCoefficient.X);
  //      DA.SetData("Thermal Expansion Coefficient Y", structAsset?.ThermalExpansionCoefficient.Y);
  //      DA.SetData("Thermal Expansion Coefficient Z", structAsset?.ThermalExpansionCoefficient.Z);

  //      // mechanical
  //      DA.SetData("Youngs Modulus X", structAsset?.YoungModulus.X);
  //      DA.SetData("Youngs Modulus Y", structAsset?.YoungModulus.Y);
  //      DA.SetData("Youngs Modulus Z", structAsset?.YoungModulus.Z);

  //      DA.SetData("Poissons Ratio X", structAsset?.PoissonRatio.X);
  //      DA.SetData("Poissons Ratio Y", structAsset?.PoissonRatio.Y);
  //      DA.SetData("Poissons Ratio Z", structAsset?.PoissonRatio.Z);

  //      DA.SetData("Shear Modulus X", structAsset?.ShearModulus.X);
  //      DA.SetData("Shear Modulus Y", structAsset?.ShearModulus.Y);
  //      DA.SetData("Shear Modulus Z", structAsset?.ShearModulus.Z);

  //      DA.SetData("Density", structAsset?.Density);

  //      // concrete
  //      DA.SetData("Concrete Compression", structAsset?.ConcreteCompression);
  //      DA.SetData("Concrete Shear Strength Modification", structAsset?.ConcreteShearStrengthReduction);
  //      DA.SetData("Concrete Lightweight", structAsset?.Lightweight);

  //      // metal
  //      // API: Values are not represented in the material editor
  //      //DA.SetData("", structAsset?.MetalReductionFactor);
  //      //DA.SetData("", structAsset?.MetalResistanceCalculationStrength);

  //      // wood
  //      DA.SetData("Wood Species", structAsset?.WoodSpecies);
  //      DA.SetData("Wood Strength Grade", structAsset?.WoodGrade);
  //      DA.SetData("Wood Bending", structAsset?.WoodBendingStrength);
  //      DA.SetData("Wood Compression Parallel to Grain", structAsset?.WoodParallelCompressionStrength);
  //      DA.SetData("Wood Compression Perpendicular to Grain", structAsset?.WoodPerpendicularCompressionStrength);
  //      DA.SetData("Wood Shear Parallel to Grain", structAsset?.WoodParallelShearStrength);
  //      DA.SetData("Wood Tension Perpendicular to Grain", structAsset?.WoodPerpendicularShearStrength);
  //      // API: Values are not represented in the API
  //      //DA.SetData("Tension Parallel to Grain", );
  //      //DA.SetData("Tension Perpendicular to Grain", );
  //      //DA.SetData("Average Modulus", );
  //      //DA.SetData("Construction", );

  //      // shared
  //      DA.SetData("Yield Strength", structAsset?.MinimumYieldStress);
  //      DA.SetData("Tensile Strength", structAsset?.MinimumTensileStrength);
  //    }
  //  }

  //  public class CreateThermalAsset : DocumentComponent
  //  {
  //    public override Guid ComponentGuid =>
  //      new Guid("bd9164c4-effb-4145-bb96-006daeaeb99a");
  //    public override GH_Exposure Exposure => GH_Exposure.quinary;

  //    protected override ParamDefinition[] Inputs => GetInputs();
  //    protected override ParamDefinition[] Outputs => new ParamDefinition[]
  //    {
  //      ParamDefinition.FromParam(ThermalAssetSchema.SchemaTypeParam)
  //    };

  //    public CreateThermalAsset() : base(
  //      name: "Create Thermal Asset",
  //      nickname: "C-THAST",
  //      description: "Create a new instance of thermal asset inside document",
  //      category: "Revit",
  //      subCategory: "Material"
  //    )
  //    {
  //    }

  //    private ParamDefinition[] GetInputs()
  //    {
  //      var inputs = new List<ParamDefinition>()
  //      {
  //        ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
  //      };

  //      foreach (IGH_Param param in ThermalAssetSchema.SchemaParams)
  //        inputs.Add(ParamDefinition.FromParam(param));

  //      return inputs.ToArray();
  //    }

  //    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
  //    {
  //      // get required input (name, class)
  //      string name = default;
  //      if (!DA.GetData("Name", ref name))
  //        return;

  //      DB.ThermalMaterialType materialType = default;
  //      if (!DA.GetData("Type", ref materialType))
  //        return;

  //      using (var transaction = NewTransaction(doc))
  //      {
  //        try
  //        {
  //          // check naming conflicts with other asset types
  //          if (doc.FindAppearanceAssetElement(name) != null)
  //          {
  //            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Shader asset with same name exists already. Use a different name for this asset");
  //            return;
  //          }
  //          else if (doc.FindStructuralAssetElement(name) != null)
  //          {
  //            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Physical asset with same name exists already. Use a different name for this asset");
  //            return;
  //          }

  //          transaction.Start();

  //          // delete existing if asset with same name already exists
  //          DB.PropertySetElement psetElement = doc.FindThermalAssetElement(name);
  //          if (psetElement != null && psetElement.Id != DB.ElementId.InvalidElementId)
  //            doc.Delete(psetElement.Id);

  //          // creaet asset from input data
  //          var thermalAsset = new DB.ThermalAsset(name, materialType);
  //          ThermalAssetSchema.SetAssetParamsFromInput(DA, thermalAsset);
  //          // set the asset on psetelement
  //          psetElement = DB.PropertySetElement.Create(doc, thermalAsset);
  //          // set other properties that are not accessible through the schema
  //          ThermalAssetSchema.SetPropertySetElementParamsFromInput(DA, psetElement);

  //          // send the new asset to output
  //          DA.SetData(
  //            ThermalAssetSchema.SchemaTypeParam.Name,
  //            psetElement
  //          );
  //        }
  //        catch (Exception ex)
  //        {
  //          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Revit API Error | {ex.Message}");
  //        }

  //        transaction.Commit();
  //      }
  //    }
  //  }

  //  public class ModifyThermalAsset : TransactionalComponent
  //  {
  //    public override Guid ComponentGuid =>
  //      new Guid("2c8f541a-f831-41e1-9e19-3c5a9b07aed4");

  //    // modifying and setting a new asset of a DB.PropertySetElement
  //    // return an "internal API error" in Revit
  //    // hiding the modify component for now
  //    // possible solution is to avoid using the DB.StructuralAsset API and
  //    // implement the schema property names manually. These assets however
  //    // are not widely used in the industry so we'll wait for customer feedback
  //#if DEBUG
  //    public override GH_Exposure Exposure => GH_Exposure.hidden;
  //#else
  //    public override GH_Exposure Exposure => GH_Exposure.quinary;
  //#endif

  //    protected override ParamDefinition[] Inputs => GetInputs();
  //    protected override ParamDefinition[] Outputs => new ParamDefinition[]
  //    {
  //      ParamDefinition.FromParam(ThermalAssetSchema.SchemaTypeParam)
  //    };

  //    public ModifyThermalAsset() : base(
  //      name: "Modify Thermal Asset",
  //      nickname: "M-THAST",
  //      description: "Modify given thermal asset",
  //      category: "Revit",
  //      subCategory: "Material"
  //    )
  //    {
  //    }

  //    private ParamDefinition[] GetInputs()
  //    {
  //      var inputs = new List<ParamDefinition>()
  //      {
  //        ParamDefinition.FromParam(ThermalAssetSchema.SchemaTypeParam)
  //      };

  //      foreach (IGH_Param param in ThermalAssetSchema.SchemaParams)
  //        if (param.Name != "Name" && param.Name != "Type")
  //          inputs.Add(ParamDefinition.FromParam(param));

  //      return inputs.ToArray();
  //    }

  //    protected override void TrySolveInstance(IGH_DataAccess DA)
  //    {
  //      // get input Thermal asset
  //      DB.PropertySetElement psetElement = default;
  //      if (!DA.GetData(ThermalAssetSchema.SchemaTypeParam.Name, ref psetElement))
  //        return;

  //      var doc = Revit.ActiveDBDocument;
  //      using (var transaction = NewTransaction(doc))
  //      {
  //        transaction.Start();

  //        // update the asset properties from input data
  //        try
  //        {
  //          ThermalAssetSchema.SetAssetParamsFromInput(DA, psetElement);
  //        }
  //        catch (Exception ex)
  //        {
  //          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Revit API Error | {ex.Message}");
  //        }


  //        // send the modified asset to output
  //        DA.SetData(
  //          ThermalAssetSchema.SchemaTypeParam.Name,
  //          psetElement
  //        );

  //        transaction.Commit();
  //      }
  //    }
  //  }

  //  public class AnalyzeThermalAsset : AnalysisComponent
  //  {
  //    public override Guid ComponentGuid =>
  //      new Guid("c3be363d-c01d-4cf3-b8d2-c345734ae66d");
  //    public override GH_Exposure Exposure => GH_Exposure.quinary;

  //    public AnalyzeThermalAsset() : base(
  //      name: "Analyze Thermal Asset",
  //      nickname: "A-THAST",
  //      description: "Analyze given thermal asset",
  //      category: "Revit",
  //      subCategory: "Material"
  //    )
  //    {
  //    }

  //    protected override void RegisterInputParams(GH_InputParamManager pManager)
  //      => pManager.AddParameter(ThermalAssetSchema.SchemaTypeParam);

  //    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  //    {
  //      foreach (IGH_Param param in ThermalAssetSchema.SchemaParams)
  //        pManager.AddParameter(param);
  //    }

  //    protected override void TrySolveInstance(IGH_DataAccess DA)
  //    {
  //      DB.PropertySetElement psetElement = default;
  //      if (!DA.GetData("Thermal Asset", ref psetElement))
  //        return;

  //      var thermalAsset = psetElement.GetThermalAsset();

  //      // information
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_NAME, "Name");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_DESCRIPTION, "Description");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PROPERTY_SET_KEYWORDS, "Keywords");
  //      DA.SetData("Type", thermalAsset?.ThermalMaterialType);
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, "Subclass");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, "Source");
  //      PipeHostParameter(DA, psetElement, DB.BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, "Source URL");

  //      // behaviour
  //      PipeHostParameter<Types.StructuralBehavior>(DA, psetElement, DB.BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR, "Behaviour");

  //      DA.SetData("Transmits Light", thermalAsset?.TransmitsLight);
  //      DA.SetData("Thermal Conductivity", thermalAsset?.ThermalConductivity);
  //      DA.SetData("Specific Heat", thermalAsset?.SpecificHeat);
  //      DA.SetData("Density", thermalAsset?.Density);
  //      DA.SetData("Emissivity", thermalAsset?.Emissivity);
  //      DA.SetData("Permeability", thermalAsset?.Permeability);
  //      DA.SetData("Porosity", thermalAsset?.Porosity);
  //      DA.SetData("Reflectivity", thermalAsset?.Reflectivity);
  //      DA.SetData("Gas Viscosity", thermalAsset?.GasViscosity);
  //      DA.SetData("Electrical Resistivity", thermalAsset?.ElectricalResistivity);
  //      DA.SetData("Liquid Viscosity", thermalAsset?.LiquidViscosity);
  //      DA.SetData("Specific Heat Of Vaporization", thermalAsset?.SpecificHeatOfVaporization);
  //      DA.SetData("Vapor Pressure", thermalAsset?.VaporPressure);
  //      DA.SetData("Compressibility", thermalAsset?.Compressibility);
  //    }
  //  }

}
