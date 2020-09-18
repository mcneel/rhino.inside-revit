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
using RhinoInside.Revit.Convert.Geometry;
using System.Reflection;
using System.Linq.Expressions;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
  public abstract class BaseAssetComponent<T>
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

    public BaseAssetComponent() : base("", "", "", "Revit", "Material") { }

    private readonly T _assetData = new T();
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
          access: paramInfo.ParamAccess
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
          access: paramInfo.ParamAccess
          );
      }
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

    protected void SetOutputsFromAsset(IGH_DataAccess DA, DB.Visual.Asset asset)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        // determine schema prop name associated with with asset property
        string schemaPropName = _assetData.GetSchemaPropertyName(assetPropInfo);
        if (schemaPropName is null)
          continue;

        // determine which output parameter to set the value on
        var paramInfo = _assetData.GetGHParameterInfo(assetPropInfo);
        if (paramInfo is null)
          continue;

        if (paramInfo.ExtractMethod == ExtractMethod.AssetOnly)
          // set the value from asset to the output param
          SetTextureDataFromAssetParam(DA, paramInfo.Name, asset, schemaPropName);
        else
          // set the value from asset to the output param
          SetDataFromAssetParam(DA, paramInfo.Name, asset, schemaPropName);
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

          var valueRange = _assetData.GetAPIAssetPropertyValueRange(assetPropInfo);
          if (valueRange != null)
            inputValue = VerifyInputValue(paramInfo.Name, inputValue, valueRange);

          assetPropInfo.SetValue(output, inputValue);
        }
      }

      return output;
    }

    protected DB.AppearanceAssetElement CreateAssetFromInputs(IGH_DataAccess DA)
    {
      // lets process all the inputs into a data structure
      // this step also verifies the input data
      var assetInfo = CreateAssetDataFromInputs(DA);

      // create new doc asset
      var doc = Revit.ActiveDBDocument;
      if (assetInfo.Name is null || assetInfo.Name == string.Empty)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bad Name");
        return null;
      }

      var assetElement = EnsureAsset(_assetData.Schema, doc, assetInfo.Name);
      if (assetElement is null)
        return null;

      // open asset for editing
      var scope = new DB.Visual.AppearanceAssetEditScope(doc);
      var editableAsset = scope.Start(assetElement.Id);

      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        // skip name because it is already set
        if (assetPropInfo.Name == "Name")
          continue;

        // determine schema prop name associated with with asset property
        string schemaPropName = _assetData.GetSchemaPropertyName(assetPropInfo);
        if (schemaPropName is null)
          continue;

        object inputValue = assetPropInfo.GetValue(assetInfo);

        try
        {
          switch (inputValue)
          {
            case bool boolVal:
              SetAssetParamValue(editableAsset, schemaPropName, boolVal);
              break;
            case string stringVal:
              SetAssetParamValue(editableAsset, schemaPropName, stringVal);
              break;
            case double dblVal:
              SetAssetParamValue(editableAsset, schemaPropName, dblVal);
              break;
            case System.Drawing.Color colorVal:
              SetAssetParamValue(editableAsset, schemaPropName, colorVal);
              break;
            case TextureData textureVal:
              SetAssetParamTexture(editableAsset, schemaPropName, textureVal);
              break;
            case AssetPropertyDouble1DMap d1dMapVal:
              if (d1dMapVal.HasTexture)
                SetAssetParamTexture(editableAsset, schemaPropName, d1dMapVal.TextureValue);
              else
                SetAssetParamValue(editableAsset, schemaPropName, d1dMapVal.Value);
              break;
            case AssetPropertyDouble4DMap d4dMapVal:
              if (d4dMapVal.HasTexture)
                SetAssetParamTexture(editableAsset, schemaPropName, d4dMapVal.TextureValue);
              else
                SetAssetParamValue(editableAsset, schemaPropName, d4dMapVal.ValueAsColor);
              break;
          }
        }
        catch (Exception ex)
        {
          AddRuntimeMessage(
            GH_RuntimeMessageLevel.Error,
            $"Can not set value of {inputValue} on schema property {schemaPropName}"
            + $" | Revit API Error: {ex.Message}"
            );
        }
      }

      // commit the changes after all changes has been made
      scope.Commit(true);

      // and send it out
      return assetElement;
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

    public static object
    ConvertFromAssetPropertyValue(DB.Visual.AssetProperty prop)
    {
      switch (prop)
      {
        case DB.Visual.AssetPropertyBoolean boolProp:
          return boolProp.Value;

        case DB.Visual.AssetPropertyDistance distProp:
          return distProp.Value;

        case DB.Visual.AssetPropertyDouble doubleProp:
          return doubleProp.Value;

        case DB.Visual.AssetPropertyDoubleArray2d d2dProp:
          if (d2dProp.Value.Size == 2)
            return new Vector2d(
              d2dProp.Value.get_Item(0),
              d2dProp.Value.get_Item(1)
            );
          break;

        case DB.Visual.AssetPropertyDoubleArray3d d3dProp:
          return d3dProp.GetValueAsXYZ().ToVector3d();

        case DB.Visual.AssetPropertyDoubleArray4d colorProp:
          return colorProp.GetValueAsColor().ToColor();

        //case DB.Visual.AssetPropertyDoubleMatrix44 matrixProp:
        //  break;

        case DB.Visual.AssetPropertyEnum enumProp:
          return enumProp.Value;

        case DB.Visual.AssetPropertyFloat floatProp:
          return floatProp.Value;

        //case DB.Visual.AssetPropertyFloatArray floatArray:
        //  break;

        case DB.Visual.AssetPropertyInt64 int64Prop:
          return int64Prop.Value;

        case DB.Visual.AssetPropertyInteger intProp:
          return intProp.Value;

        //case DB.Visual.AssetPropertyList listProp:
        //  break;

        //case DB.Visual.AssetPropertyReference refProp:
        //  break;

        case DB.Visual.AssetPropertyString textProp:
          return textProp.Value;

        case DB.Visual.AssetPropertyTime timeProp:
          return timeProp.Value;

        case DB.Visual.AssetPropertyUInt64 uint64Prop:
          return uint64Prop.Value;
      }
      return null;
    }

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
      if (name is null || name == string.Empty)
        return null;

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
          case System.Drawing.Color colorVal:
            SetAssetParamValue(textureAsset, schemaPropName, colorVal);
            break;
        }
      }
    }

    public static void
    SetDataFromAssetParam(IGH_DataAccess DA, string paramName,
                          DB.Visual.Asset asset, string schemaPropName)
    {
      // find param
      var prop = asset.FindByName(schemaPropName);
      if (prop is null)
        DA.SetData(schemaPropName, null);

      // determine data type, and set output
      DA.SetData(paramName, ConvertFromAssetPropertyValue(prop));
    }

    public static void
    SetTextureDataFromAssetParam(IGH_DataAccess DA, string paramName,
                                 DB.Visual.Asset asset, string schemaPropName)
    {
      // find param
      var prop = asset.FindByName(schemaPropName);
      if (prop is null)
        DA.SetData(schemaPropName, null);

      var connectedAsset = prop.GetSingleConnectedAsset();
      if (connectedAsset != null)
      {
        var assetData =
          AssetData.GetSchemaDataType(
            // Asset schema names end in "Schema" e.g. "UnifiedBitmapSchema"
            // They do not match the names for API wrapper
            // types e.g. "DB.Visual.UnifiedBitmap"
            // lets remove the extra stuff
            connectedAsset.Name.Replace("Schema", "")
            );
        if (assetData != null)
        {
          SetAssetDataFromAsset(assetData, connectedAsset);
          DA.SetData(paramName, assetData);
        }
      }
    }

    private static void
    SetAssetDataFromAsset(AssetData assetData, DB.Visual.Asset asset)
    {
      foreach (var assetPropInfo in assetData.GetAssetProperties())
      {
        // determine schema prop name associated with with asset property
        string schemaPropName = assetData.GetSchemaPropertyName(assetPropInfo);
        if (schemaPropName is null)
          continue;

        // find param
        var prop = asset.FindByName(schemaPropName);
        if (prop is null)
          continue;

        // determine data type, and set output
        assetPropInfo.SetValue(assetData, ConvertFromAssetPropertyValue(prop));
      }
    }
    #endregion
  }
}
