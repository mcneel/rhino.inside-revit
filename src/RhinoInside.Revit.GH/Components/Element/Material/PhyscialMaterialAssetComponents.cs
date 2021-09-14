using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
{
  public abstract class BasePhysicalAssetComponent<T>
    : TransactionalChainComponent where T : PhysicalMaterialData, new()
  {
    protected AssetGHComponentAttribute ComponentInfo
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
    private AssetGHComponentAttribute _compInfo;

    protected ParamDefinition[] GetAssetDataAsInputs(bool skipUnchangable = false)
    {
      List<ParamDefinition> inputs = new List<ParamDefinition>();

      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        if (skipUnchangable && !paramInfo.Modifiable)
          continue;

        var param = (IGH_Param) Activator.CreateInstance(paramInfo.ParamType);
        param.Name = paramInfo.Name;
        param.NickName = paramInfo.NickName;
        param.Description = paramInfo.Description;
        param.Access = paramInfo.ParamAccess;
        param.Optional = paramInfo.Optional;

        inputs.Add(new ParamDefinition(param));
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

        outputs.Add(new ParamDefinition(param));
      }

      return outputs.ToArray();
    }

    protected static DB.PropertySetElement FindPropertySetElement(DB.Document doc, string name)
    {
      using (var collector = new DB.FilteredElementCollector(doc).
             OfClass(typeof(DB.PropertySetElement)).
             WhereParameterEqualsTo(DB.BuiltInParameter.PROPERTY_SET_NAME, name))
      {
        return collector.FirstElement() as DB.PropertySetElement;
      }
    }

    // determines matching assets based on any builtin properties
    // that are marked exclusive
    protected bool MatchesPhysicalAssetType(DB.PropertySetElement psetElement)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
        foreach (var builtInPropInfo in
          _assetData.GetAPIAssetBuiltInPropertyInfos(assetPropInfo))
          if (builtInPropInfo.Exclusive
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

    protected void SetOutputsFromPropertySetElement(IGH_DataAccess DA, DB.PropertySetElement psetElement)
    {
      if (psetElement is null)
        return;

      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        // determine which output parameter to set the value on
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        // grab the value from the first valid builtin param and set on the ouput
        foreach (var builtInPropInfo in _assetData.GetAPIAssetBuiltInPropertyInfos(assetPropInfo))
        {
          if (psetElement.get_Parameter(builtInPropInfo.ParamId) is DB.Parameter parameter)
          {
            DA.SetData(paramInfo.Name, parameter.AsGoo());
            break;
          }
        }
      }
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
            psetElement.UpdateParameterValue(builtInPropInfo.ParamId, inputValue);
        }
      }
    }

    #region Asset Utility Methods
    public object
    VerifyInputValue(string inputName, object inputValue, APIAssetPropValueRangeAttribute valueRangeInfo)
    {
      switch (inputValue)
      {
        case double dblVal:
          // check double max
          if (dblVal < valueRangeInfo.Min)
          {
            AddRuntimeMessage(
              GH_RuntimeMessageLevel.Warning,
              $"\"{inputName}\" value is smaller than the allowed " +
              $"minimum \"{valueRangeInfo.Min}\". Minimum value is " +
              "used instead to avoid errors"
              );
            return (object) valueRangeInfo.Min;
          }
          else if (dblVal > valueRangeInfo.Max)
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

  #region Structural Asset
  public class CreateStructuralAsset : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("af2678c8-2a53-4056-9399-5a06dd9ac14d");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public CreateStructuralAsset() : base
    (
      name: "Create Physical Asset",
      nickname: "Physical Asset",
      description: "Create a Revit structural asset",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Asset Name",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.StructuralAssetClass>()
        {
          Name = "Class",
          NickName = "C",
          Description = "Structural Asset Class",
        }
      ),
      new ParamDefinition
      (
        new Parameters.StructuralAsset()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Asset",
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
        new Parameters.StructuralAsset()
        {
          Name = _Asset_,
          NickName = _Asset_.Substring(0, 1),
          Description = $"Output {_Asset_}",
        }
      ),
    };

    const string _Asset_ = "Physical Asset";
    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.PROPERTY_SET_NAME,
      DB.BuiltInParameter.PHY_MATERIAL_PARAM_CLASS
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.GetData(DA, "Class", out Types.StructuralAssetClass type, x => x.IsValid)) return;
      Params.TryGetData(DA, "Template", out DB.PropertySetElement template);

      // Previous Output
      Params.ReadTrackedElement(_Asset_, doc.Value, out DB.PropertySetElement asset);

      StartTransaction(doc.Value);
      {
        asset = Reconstruct(asset, doc.Value, name, type.Value, template);

        Params.WriteTrackedElement(_Asset_, doc.Value, asset);
        DA.SetData(_Asset_, asset);
      }
    }

    bool Reuse(DB.PropertySetElement assetElement, string name, DB.StructuralAssetClass type, DB.PropertySetElement template)
    {
      if (assetElement is null) return false;
      if (name is object) assetElement.Name = name;

      if (assetElement.GetStructuralAsset() is DB.StructuralAsset asset)
      {
        if (asset.StructuralAssetClass != type) return false;
      }
      else return false;

      if (template?.GetStructuralAsset() is DB.StructuralAsset)
      {
        template.CopyParametersFrom(template, ExcludeUniqueProperties);
      }

      return true;
    }

    DB.PropertySetElement Create(DB.Document doc, string name, DB.StructuralAssetClass type, DB.PropertySetElement template)
    {
      var assetElement = default(DB.PropertySetElement);

      // Make sure the name is unique
      {
        if (name is null)
          name = template?.Name ?? _Asset_;

        name = doc.GetNamesakeElements
        (
          typeof(DB.PropertySetElement), name, categoryId: DB.BuiltInCategory.OST_PropertySet
        ).
        Where(x => Types.StructuralAssetElement.IsValidElement(x)).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
      }

      // Try to duplicate template
      if (template is object)
      {
        if (doc.Equals(template.Document))
        {
          assetElement = template.Duplicate(doc, name);
        }
        else
        {
          var ids = DB.ElementTransformUtils.CopyElements
          (
            template.Document,
            new DB.ElementId[] { template.Id },
            doc,
            default,
            default
          );

          assetElement = ids.Select(x => doc.GetElement(x)).OfType<DB.PropertySetElement>().FirstOrDefault();
          assetElement.Name = name;
        }
      }

      if (assetElement is null)
      {
        var asset = new DB.StructuralAsset(name, type);
        assetElement = DB.PropertySetElement.Create(doc, asset);
      }

      return assetElement;
    }

    DB.PropertySetElement Reconstruct(DB.PropertySetElement assetElement, DB.Document doc, string name, DB.StructuralAssetClass type, DB.PropertySetElement template)
    {
      if (!Reuse(assetElement, name, type, template))
      {
        assetElement = assetElement.ReplaceElement
        (
          Create(doc, name, type, template),
          ExcludeUniqueProperties
        );
      }

      return assetElement;
    }
  }

  public class ModifyStructuralAsset : BasePhysicalAssetComponent<StructuralAssetData>
  {
    public override Guid ComponentGuid =>
      new Guid("67a74d31-0878-4b48-8efb-f4ca97389f74");

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public ModifyStructuralAsset() : base()
    {
      Name = $"Modify {ComponentInfo.Name}";
      NickName = $"M-{ComponentInfo.NickName}";
      Description = $"Modify an existing instance of {ComponentInfo.Description}";
    }

    protected override ParamDefinition[] Inputs => GetInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.StructuralAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };

    private ParamDefinition[] GetInputs()
    {
      var inputs = new List<ParamDefinition>()
        {
          ParamDefinition.Create<Parameters.StructuralAsset>(
            name: ComponentInfo.Name,
            nickname: ComponentInfo.NickName,
            description: ComponentInfo.Description,
            access: GH_ParamAccess.item
          ),
        };

      foreach (ParamDefinition param in GetAssetDataAsInputs(skipUnchangable: true))
        if (param.Param.Name != "Name" && param.Param.Name != "Type")
          inputs.Add(param);

      return inputs.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input structural asset
      var structuralAsset = default(Types.StructuralAssetElement);
      if (!DA.GetData(ComponentInfo.Name, ref structuralAsset) || structuralAsset.Value is null)
        return;

      StartTransaction(structuralAsset.Document);

      // grab asset data from inputs
      var assetData = CreateAssetDataFromInputs(DA);
      UpdatePropertySetElementFromData(structuralAsset.Value, assetData);

      // send the modified asset to output
      DA.SetData(ComponentInfo.Name, structuralAsset);
    }
  }

  public class AnalyzeStructuralAsset : BasePhysicalAssetComponent<StructuralAssetData>
  {
    public override Guid ComponentGuid =>
      new Guid("ec93f8e0-d2af-4a44-a040-89a7c40b9fc7");

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public AnalyzeStructuralAsset() : base()
    {
      Name = $"Analyze {ComponentInfo.Name}";
      NickName = $"A-{ComponentInfo.NickName}";
      Description = $"Analyzes given instance of {ComponentInfo.Description}";

    }

    protected override ParamDefinition[] Inputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.StructuralAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };
    protected override ParamDefinition[] Outputs => GetAssetDataAsOutputs();

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var structuralAsset = default(Types.StructuralAssetElement);
      if (!DA.GetData(ComponentInfo.Name, ref structuralAsset) || structuralAsset.Value is null)
        return;

      SetOutputsFromPropertySetElement(DA, structuralAsset.Value);
    }
  }
# endregion

  #region Thermal Asset
  public class CreateThermalAsset : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("BD9164C4-EFFB-4145-BB96-006DAEAEB99A");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public CreateThermalAsset() : base
    (
      name: "Create Thermal Asset",
      nickname: "Thermal Asset",
      description: "Create a Revit thermal asset",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Asset Name",
          Optional = true
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ThermalMaterialType>()
        {
          Name = "Class",
          NickName = "C",
          Description = "Asset Class",
        }
      ),
      new ParamDefinition
      (
        new Parameters.ThermalAsset()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Asset",
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
        new Parameters.ThermalAsset()
        {
          Name = _Asset_,
          NickName = _Asset_.Substring(0, 1),
          Description = $"Output {_Asset_}",
        }
      ),
    };

    const string _Asset_ = "Thermal Asset";
    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.PROPERTY_SET_NAME,
      DB.BuiltInParameter.PHY_MATERIAL_PARAM_CLASS
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.GetData(DA, "Class", out Types.ThermalMaterialType type, x => x.IsValid)) return;
      Params.TryGetData(DA, "Template", out DB.PropertySetElement template);

      // Previous Output
      Params.ReadTrackedElement(_Asset_, doc.Value, out DB.PropertySetElement asset);

      StartTransaction(doc.Value);
      {
        asset = Reconstruct(asset, doc.Value, name, type.Value, template);

        Params.WriteTrackedElement(_Asset_, doc.Value, asset);
        DA.SetData(_Asset_, asset);
      }
    }

    bool Reuse(DB.PropertySetElement assetElement, string name, DB.ThermalMaterialType type, DB.PropertySetElement template)
    {
      if (assetElement is null) return false;
      if (name is object) assetElement.Name = name;

      if (assetElement.GetThermalAsset() is DB.ThermalAsset asset)
      {
        if (asset.ThermalMaterialType != type) return false;
      }
      else return false;

      if (template?.GetThermalAsset() is DB.ThermalAsset)
      {
        template.CopyParametersFrom(template, ExcludeUniqueProperties);
      }

      return true;
    }

    DB.PropertySetElement Create(DB.Document doc, string name, DB.ThermalMaterialType type, DB.PropertySetElement template)
    {
      var assetElement = default(DB.PropertySetElement);

      // Make sure the name is unique
      {
        if (name is null)
          name = template?.Name ?? _Asset_;

        name = doc.GetNamesakeElements
        (
          typeof(DB.PropertySetElement), name, categoryId: DB.BuiltInCategory.OST_PropertySet
        ).
        Where(x => Types.ThermalAssetElement.IsValidElement(x)).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
      }

      // Try to duplicate template
      if (template is object)
      {
        if (doc.Equals(template.Document))
        {
          assetElement = template.Duplicate(doc, name);
        }
        else
        {
          var ids = DB.ElementTransformUtils.CopyElements
          (
            template.Document,
            new DB.ElementId[] { template.Id },
            doc,
            default,
            default
          );

          assetElement = ids.Select(x => doc.GetElement(x)).OfType<DB.PropertySetElement>().FirstOrDefault();
          assetElement.Name = name;
        }
      }

      if (assetElement is null)
      {
        var asset = new DB.ThermalAsset(name, type);
        assetElement = DB.PropertySetElement.Create(doc, asset);
      }

      return assetElement;
    }

    DB.PropertySetElement Reconstruct(DB.PropertySetElement assetElement, DB.Document doc, string name, DB.ThermalMaterialType type, DB.PropertySetElement template)
    {
      if (!Reuse(assetElement, name, type, template))
      {
        assetElement = assetElement.ReplaceElement
        (
          Create(doc, name, type, template),
          ExcludeUniqueProperties
        );
      }

      return assetElement;
    }
  }

  public class ModifyThermalAsset : BasePhysicalAssetComponent<ThermalAssetData>
  {
    public override Guid ComponentGuid =>
      new Guid("2c8f541a-f831-41e1-9e19-3c5a9b07aed4");

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public ModifyThermalAsset() : base()
    {
      Name = $"Modify {ComponentInfo.Name}";
      NickName = $"M-{ComponentInfo.NickName}";
      Description = $"Modify an existing instance of {ComponentInfo.Description}";
    }

    protected override ParamDefinition[] Inputs => GetInputs();
    protected override ParamDefinition[] Outputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.ThermalAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };

    private ParamDefinition[] GetInputs()
    {
      var inputs = new List<ParamDefinition>()
        {
          ParamDefinition.Create<Parameters.ThermalAsset>(
            name: ComponentInfo.Name,
            nickname: ComponentInfo.NickName,
            description: ComponentInfo.Description,
            access: GH_ParamAccess.item
          ),
        };

      foreach (ParamDefinition param in GetAssetDataAsInputs(skipUnchangable: true))
        if (param.Param.Name != "Name" && param.Param.Name != "Type")
          inputs.Add(param);

      return inputs.ToArray();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input structural asset
      var thermalAsset = default(Types.ThermalAssetElement);
      if (!DA.GetData(ComponentInfo.Name, ref thermalAsset) || thermalAsset.Value is null)
        return;

      StartTransaction(thermalAsset.Document);

      // grab asset data from inputs
      var assetData = CreateAssetDataFromInputs(DA);
      UpdatePropertySetElementFromData(thermalAsset.Value, assetData);

      // send the modified asset to output
      DA.SetData(ComponentInfo.Name, thermalAsset);
    }
  }

  public class AnalyzeThermalAsset : BasePhysicalAssetComponent<ThermalAssetData>
  {
    public override Guid ComponentGuid =>
      new Guid("c3be363d-c01d-4cf3-b8d2-c345734ae66d");

    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public AnalyzeThermalAsset() : base()
    {
      Name = $"Analyze {ComponentInfo.Name}";
      NickName = $"A-{ComponentInfo.NickName}";
      Description = $"Analyzes given instance of {ComponentInfo.Description}";

    }

    protected override ParamDefinition[] Inputs => new ParamDefinition[]
    {
      ParamDefinition.Create<Parameters.ThermalAsset>(
        name: ComponentInfo.Name,
        nickname: ComponentInfo.NickName,
        description: ComponentInfo.Description,
        access: GH_ParamAccess.item
        ),
    };
    protected override ParamDefinition[] Outputs => GetAssetDataAsOutputs();

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var thermalAsset = default(Types.ThermalAssetElement);
      if (!DA.GetData(ComponentInfo.Name, ref thermalAsset) || thermalAsset.Value is null)
        return;

      SetOutputsFromPropertySetElement(DA, thermalAsset.Value);
    }
  }
  #endregion
}
