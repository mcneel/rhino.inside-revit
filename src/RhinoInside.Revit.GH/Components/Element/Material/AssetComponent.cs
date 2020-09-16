using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using System.ComponentModel;
using System.Diagnostics;
using RhinoInside.Revit.Convert.System.Drawing;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public abstract class AssetComponent<T>
    : TransactionComponent where T : AssetData, new()
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

    public AssetComponent() : base("", "", "", "Revit", "Material") { }

    private T _assetData = new T();
    private AssetGHComponent _compInfo;

    protected void SetFieldsAsInputs(GH_InputParamManager pManager)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;
        int idx = pManager.AddParameter(
          param: (IGH_Param) Activator.CreateInstance(paramInfo.ParamType),
          name: paramInfo.Name,
          nickname: paramInfo.NickName,
          description: paramInfo.Description,
          access: GH_ParamAccess.item
          );
        pManager[idx].Optional = paramInfo.Optional;
      }
    }

    protected void SetFieldsAsOutputs(GH_OutputParamManager pManager)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;
        pManager.AddParameter(
          param: (IGH_Param) Activator.CreateInstance(paramInfo.ParamType),
          name: paramInfo.Name,
          nickname: paramInfo.NickName,
          description: paramInfo.Description,
          access: GH_ParamAccess.item
          );
      }
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
        if (DA.GetData(paramInfo.Name, ref inputGHType))
        {
          object inputValue = inputGHType.ScriptVariable();
          // check the range for double values
          if (inputValue is double doubleVal)
          {
            var valueRangeInfo =
              assetPropInfo.GetCustomAttributes(typeof(APIAssetPropValueRange), false)
                       .Cast<APIAssetPropValueRange>()
                       .FirstOrDefault();

            if (valueRangeInfo != null)
            {
              if (valueRangeInfo.Min != double.NaN
                    && doubleVal < valueRangeInfo.Min)
              {
                AddRuntimeMessage(
                  GH_RuntimeMessageLevel.Warning,
                  $"\"{paramInfo.Name}\" value is smaller than the allowed " +
                  $"minimum \"{valueRangeInfo.Min}\". Minimum value is " +
                  "used instead to avoid errors"
                  );
                inputValue = (object) valueRangeInfo.Min;
              }

              if (valueRangeInfo.Max != double.NaN
                    && doubleVal > valueRangeInfo.Max)
              {
                AddRuntimeMessage(
                  GH_RuntimeMessageLevel.Warning,
                  $"\"{paramInfo.Name}\" value is larger than the allowed " +
                  $"maximum of \"{valueRangeInfo.Max}\". Maximum value is " +
                  "used instead to avoid errors"
                  );
                inputValue = (object) valueRangeInfo.Max;
              }
            }
          }

          assetPropInfo.SetValue(output, inputValue);
        }
      }

      return output;
    }

    protected void SetOutputsFromAssetData(IGH_DataAccess DA, T input)
    {
      // set its properties
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        object inputValue = assetPropInfo.GetValue(input);
        if (inputValue != null)
          DA.SetData(paramInfo.Name, inputValue);
      }
    }

    //protected DB.Visual.Asset CreateAssetFromInputs(IGH_DataAccess DA)
    //{

    //}

    protected void SetOutputsFromAsset(IGH_DataAccess DA, DB.Visual.Asset asset)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        // determine schema prop name associated with field
        string schemaPropName = _assetData.GetSchemaPropertyName(assetPropInfo);
        if (schemaPropName is null)
          continue;

        // determine which output parameter to set the value on
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        // set the value from asset to the output param
        SetDataFromAssetParam(DA, paramInfo.Name, asset, schemaPropName);
      }
    }

    #region Asset Utility Methods
    public static List<DB.Visual.Asset> GetLibrayAssets(DB.Visual.AssetType assetType)
      => Revit.ActiveDBApplication.GetAssets(assetType).ToList();

    public static DB.Visual.Asset FindLibraryAsset(DB.Visual.AssetType assetType, string schema)
      => GetLibrayAssets(assetType).Where(x => x.Name == schema).FirstOrDefault();

    public static List<DB.AppearanceAssetElement>
    QueryAppearanceAssetElements(DB.Document doc)
    {
      return new DB.FilteredElementCollector(doc)
                    .OfClass(typeof(DB.AppearanceAssetElement))
                    .WhereElementIsNotElementType()
                    .ToElements()
                    .Cast<DB.AppearanceAssetElement>()
                    .ToList();
    }

    public static DB.AppearanceAssetElement
    FindAppearanceAssetElement(DB.Document doc, string name)
      => DB.AppearanceAssetElement.GetAppearanceAssetElementByName(doc, name);

    public static DB.AppearanceAssetElement
    EnsureAsset(string schemaName, DB.Document doc, string name)
    {
      var existingAsset = FindAppearanceAssetElement(doc, name);
      if (existingAsset != null)
        return existingAsset;
      var baseAsset = FindLibraryAsset(DB.Visual.AssetType.Appearance, schemaName);
      return DB.AppearanceAssetElement.Create(doc, name, baseAsset);
    }

    public static void
    SetAssetParamValue(DB.Visual.Asset asset, string name, bool value, bool removeAsset = true)
    {
      var prop = asset.FindByName(name);
      switch (prop)
      {
        case DB.Visual.AssetPropertyBoolean boolProp:
          if (removeAsset)
            boolProp.RemoveConnectedAsset();
          boolProp.Value = value;
          break;
      }
    }

    public static void
    SetAssetParamValue(DB.Visual.Asset asset, string name, string value, bool removeAsset = true)
    {
      var prop = asset.FindByName(name);
      switch (prop)
      {
        case DB.Visual.AssetPropertyString stringProp:
          if (removeAsset)
            stringProp.RemoveConnectedAsset();
          stringProp.Value = value;
          break;
      }
    }

    public static void
    SetAssetParamValue(DB.Visual.Asset asset, string name, double value, bool removeAsset = true)
    {
      var prop = asset.FindByName(name);
      switch (prop)
      {
        case DB.Visual.AssetPropertyDouble doubleProp:
          if (removeAsset)
            doubleProp.RemoveConnectedAsset();
          doubleProp.Value = value;
          break;
        case DB.Visual.AssetPropertyDistance distProp:
          if (removeAsset)
            distProp.RemoveConnectedAsset();
          distProp.Value = value;
          break;
      }
    }

    public static void
    SetAssetParamValue(DB.Visual.Asset asset, string name, System.Drawing.Color value, bool removeAsset = true)
    {
      var prop = asset.FindByName(name);
      switch (prop)
      {
        case DB.Visual.AssetPropertyDoubleArray3d tdProp:
          if (removeAsset)
            tdProp.RemoveConnectedAsset();
          tdProp.SetValueAsXYZ(new DB.XYZ(
            value.R / 255.0,
            value.G / 255.0,
            value.B / 255.0
          ));
          break;
        case DB.Visual.AssetPropertyDoubleArray4d fdProp:
          if (removeAsset)
            fdProp.RemoveConnectedAsset();
          fdProp.SetValueAsDoubles(new double[] {
            value.R / 255.0,
            value.G / 255.0,
            value.B / 255.0,
            value.A / 255.0
          });
          break;
      }
    }

    public static void
    SetAssetParamTexture(DB.Visual.Asset asset, string name, TextureData value)
    {
      var prop = asset.FindByName(name);
      prop.RemoveConnectedAsset();
      prop.AddConnectedAsset(value.Schema);
      var textureAsset = prop.GetSingleConnectedAsset();
      foreach (var assetPropInfo in value.GetAssetProperties())
      {
        string schemaPropName = value.GetSchemaPropertyName(assetPropInfo);
        if (schemaPropName is null)
          continue;

        object fieldValue = assetPropInfo.GetValue(value);
        switch (fieldValue)
        {
          case bool boolVal:
            SetAssetParamValue(textureAsset, schemaPropName, boolVal);
            break;
          case string stringVal:
            SetAssetParamValue(textureAsset, schemaPropName, stringVal);
            break;
          case double doubleVal:
            SetAssetParamValue(textureAsset, schemaPropName, doubleVal);
            break;
        }
      }
    }

    public static void
    SetDataFromAssetParam(IGH_DataAccess DA, string paramName, DB.Visual.Asset asset, string propName)
    {
      // find param
      var prop = asset.FindByName(propName);
      // determine data type, and set output
      // TODO: add the rest of prop types
      switch (prop)
      {
        case DB.Visual.AssetPropertyBoolean boolProp:
          DA.SetData(paramName, boolProp.Value);
          break;

        case DB.Visual.AssetPropertyDoubleArray4d colorProp:
          DA.SetData(paramName, colorProp.GetValueAsColor().ToColor());
          break;
      }
    }
    #endregion
  }
}
