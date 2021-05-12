using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.Units;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
{
#if REVIT_2018
  public abstract class BaseAssetComponent<T>
    : TransactionalChainComponent where T : AppearanceAssetData, new()
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

    public BaseAssetComponent() : base("", "", "", "Revit", "Material") { }

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
      // make sure the schemas match
      if (asset.Name.Replace("Schema", "") != _assetData.Schema)
      {
        AddRuntimeMessage(
          GH_RuntimeMessageLevel.Warning,
          $"Incorrect asset schema \"{asset.Name}\""
          );
        return;
      }

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

        // check the toggle if available and output only when toggle
        // is active. otherwise we assume that the property has no value
        bool sendValueToOutput = true;
        var schemaTogglePropName = _assetData.GetSchemaTogglePropertyName(assetPropInfo);
        if (schemaTogglePropName != null)
          // then get the asset property object and check its boolean value
          // if false, the output will not be set
          sendValueToOutput =
            ((bool?) GetAssetParamValue(asset, schemaTogglePropName)) == true;

        if (sendValueToOutput)
        {
          switch (paramInfo.ExtractMethod)
          {
            case ExtractMethod.AssetOnly:
              // set the value from asset to the output param
              SetTextureDataFromAssetParam(DA, paramInfo.Name, asset, schemaPropName);
              break;

            case ExtractMethod.ValueOnly:
            // to the same to value first
            case ExtractMethod.ValueFirst:
              // set the value from asset to the output param
              SetDataFromAssetParam(DA, paramInfo.Name, asset, schemaPropName);
              break;

            case ExtractMethod.AssetFirst:
              // first check whether asset param has a nested asset
              if (AssetParamHasNestedAsset(asset, schemaPropName))
                SetTextureDataFromAssetParam(DA, paramInfo.Name, asset, schemaPropName);
              else
                SetDataFromAssetParam(DA, paramInfo.Name, asset, schemaPropName);
              break;
          }
        }
        else
          DA.SetData(paramInfo.Name, null);
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
        var paramIdx = Params.IndexOfInputParam(paramInfo.Name);
        if (paramIdx < 0)
          continue;

        bool hasInput = DA.GetData(paramIdx, ref inputGHType);
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

    private void UpdateAssetFromData(DB.Visual.Asset editableAsset, T assetData)
    {
      foreach (var assetPropInfo in _assetData.GetAssetProperties())
      {
        // skip name because it is already set
        if (assetPropInfo.Name == "Name")
          continue;

        // determine schema prop name associated with with asset property
        string schemaPropName = _assetData.GetSchemaPropertyName(assetPropInfo);
        if (schemaPropName is null)
          continue;

        bool hasValue = assetData.IsMarked(assetPropInfo.Name);
        if (hasValue)
        {
          object inputValue = assetPropInfo.GetValue(assetData);

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
              case Rhino.Display.ColorRGBA colorVal:
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
                  SetAssetParamValue(editableAsset, schemaPropName, d4dMapVal.ToColorRGBA());
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

        var schemaTogglePropName = _assetData.GetSchemaTogglePropertyName(assetPropInfo);
        if (schemaTogglePropName != null)
          SetAssetParamValue(editableAsset, schemaTogglePropName, hasValue);
      }
    }

    protected void UpdateAssetElementFromInputs(DB.AppearanceAssetElement assetElement, T assetData)
    {
      // open asset for editing
      using (var scope = new DB.Visual.AppearanceAssetEditScope(assetElement.Document))
      {
        var editableAsset = scope.Start(assetElement.Id);

        UpdateAssetFromData(editableAsset, assetData);

        // commit the changes after all changes has been made
        scope.Commit(true);
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

    public static object
    ConvertFromAssetPropertyValue(DB.Visual.AssetProperty prop)
    {
      // TODO: implement all prop types
      switch (prop)
      {
        case DB.Visual.AssetPropertyBoolean boolProp:
          return boolProp.Value;

        case DB.Visual.AssetPropertyDistance distProp:
          return DB.UnitUtils.Convert
          (
            distProp.Value,
            distProp.GetUnitTypeId(),
            (Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem ?? Rhino.UnitSystem.Meters).ToUnitType()
          );

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
          var list = colorProp.GetValueAsDoubles();
          return new Rhino.Display.ColorRGBA(list[0], list[1], list[2], list[3]);

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

    static DB.Visual.Asset FindLibraryAsset
    (
      Autodesk.Revit.ApplicationServices.Application app,
      DB.Visual.AssetType assetType,
      string schema
    )
    {
      return app.GetAssets(assetType).Where(x => x.Name == schema).FirstOrDefault();
    }

    public static List<DB.AppearanceAssetElement> QueryAppearanceAssetElements(DB.Document doc)
    {
      return new DB.FilteredElementCollector(doc).
        OfClass(typeof(DB.AppearanceAssetElement)).
        Cast<DB.AppearanceAssetElement>().
        ToList();
    }

    public static DB.AppearanceAssetElement
    EnsureAsset(string schemaName, DB.Document doc, string name)
    {
      var existingAsset = DB.AppearanceAssetElement.GetAppearanceAssetElementByName(doc, name);
      if (existingAsset != null)
        return existingAsset;

      var baseAsset = FindLibraryAsset(doc.Application, DB.Visual.AssetType.Appearance, schemaName);
      return DB.AppearanceAssetElement.Create(doc, name, baseAsset);
    }

    public DB.AppearanceAssetElement EnsureThisAsset(DB.Document doc, string name)
     => EnsureAsset(_assetData.Schema, doc, name);

    public static bool AssetParamHasNestedAsset(DB.Visual.Asset asset, string name)
    {
      // find param
      var prop = asset.FindByName(name);
      return prop != null && prop.GetSingleConnectedAsset() != null;
    }

    public static object GetAssetParamValue(DB.Visual.Asset asset, string name)
    {
      // find param
      if(asset.FindByName(name) is DB.Visual.AssetProperty prop)
        return ConvertFromAssetPropertyValue(prop);

      return null;
    }

    public static void SetAssetParamValue(DB.Visual.Asset asset, string name, bool value, bool removeAsset = true)
    {
      var prop = asset.FindByName(name);
      switch (prop)
      {
        case DB.Visual.AssetPropertyBoolean boolProp:
          if (removeAsset) boolProp.RemoveConnectedAsset();

          boolProp.Value = value;
          break;
      }
    }

    public static void SetAssetParamValue(DB.Visual.Asset asset, string name, string value, bool removeAsset = true)
    {
      var prop = asset.FindByName(name);
      switch (prop)
      {
        case DB.Visual.AssetPropertyString stringProp:
          if (removeAsset) stringProp.RemoveConnectedAsset();

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
          if (removeAsset) doubleProp.RemoveConnectedAsset();

          doubleProp.Value = value;
          break;

        case DB.Visual.AssetPropertyDistance distProp:
          if (removeAsset) distProp.RemoveConnectedAsset();

          distProp.Value = DB.UnitUtils.Convert
          (
            value,
            (Rhino.RhinoDoc.ActiveDoc?.ModelUnitSystem ?? Rhino.UnitSystem.Meters).ToUnitType(),
            distProp.GetUnitTypeId()
          );
          break;
      }
    }

    public static void
    SetAssetParamValue(DB.Visual.Asset asset, string name, Rhino.Display.ColorRGBA value, bool removeAsset = true)
    {
      var prop = asset.FindByName(name);
      switch (prop)
      {
        case DB.Visual.AssetPropertyDoubleArray3d tdProp:
          if (removeAsset) tdProp.RemoveConnectedAsset();

          tdProp.SetValueAsXYZ(new DB.XYZ(
            value.R,
            value.G,
            value.B
          ));
          break;
        case DB.Visual.AssetPropertyDoubleArray4d fdProp:
          if (removeAsset) fdProp.RemoveConnectedAsset();

          fdProp.SetValueAsDoubles(new double[] {
            value.R,
            value.G,
            value.B,
            value.A
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
          case Rhino.Display.ColorRGBA colorVal:
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
      if (asset.FindByName(schemaPropName) is DB.Visual.AssetProperty prop)
      {
        // determine data type, and set output
        DA.SetData(paramName, ConvertFromAssetPropertyValue(prop));
      }
    }

    public static void
    SetTextureDataFromAssetParam(IGH_DataAccess DA, string paramName,
                                 DB.Visual.Asset asset, string schemaPropName)
    {
      // find param
      if (asset.FindByName(schemaPropName) is DB.Visual.AssetProperty prop)
      {
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
#endif
}
