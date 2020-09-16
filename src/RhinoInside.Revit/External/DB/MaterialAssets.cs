using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using RDB = Autodesk.Revit.DB;
using RDBV = Autodesk.Revit.DB.Visual;
using Autodesk.Private.Windows;
using Eto.Forms;

namespace RhinoInside.Revit.External.DB
{
  #region Custom Asset GH Type Data

  /// <summary>
  /// Base class for GH accept parameters that can connect to texture assets
  /// </summary>
  public class AssetParameterFlex
  {
    public TextureData TextureValue;
    public AssetParameterFlex() { }
    public AssetParameterFlex(TextureData textureData)
      => TextureValue = textureData;
    public bool HasTexture
      => TextureValue != null && TextureValue.Schema != string.Empty;
  }

  /// <summary>
  /// Parameter that accepts a single double value or a texture
  /// </summary>
  public class AssetParameterDouble1DMap: AssetParameterFlex
  {
    public double Value = 0;
    public AssetParameterDouble1DMap(TextureData tdata) : base(tdata) { }
    public AssetParameterDouble1DMap(double value) : base()
      => Value = value;
  }

  /// <summary>
  /// Parameter that can accept a single double 4d value or texture
  /// </summary>
  public class AssetParameterDouble4DMap: AssetParameterFlex
  {
    public double Value1 = 0;
    public double Value2 = 0;
    public double Value3 = 0;
    public double Value4 = 0;
    public AssetParameterDouble4DMap(TextureData tdata) : base(tdata) { }
    public AssetParameterDouble4DMap(double one, double two, double three, double four)
    {
      Value1 = one; Value2 = two; Value3 = three; Value4 = four;
    }
    public AssetParameterDouble4DMap(double val) : this(val, val, val, val) { }
    public AssetParameterDouble4DMap(System.Drawing.Color color)
      => ValueAsColor = color;

    public System.Drawing.Color ValueAsColor
    {
      get
      {
        return System.Drawing.Color.FromArgb(
          (int)(Value1 * 255),
          (int)(Value2 * 255),
          (int)(Value3 * 255),
          (int)(Value4 * 255)
          );
      }
      set
      {
        Value1 = value.A / 255.0;
        Value2 = value.R / 255.0;
        Value3 = value.G / 255.0;
        Value4 = value.B / 255.0;
      }
    }

    public double Average => (Value1 + Value2 + Value3 + Value4) / 4;
  }

  #endregion

  #region Wrappers for Revit Assets

  #region Attributes
  [AttributeUsage(AttributeTargets.Class)]
  public class APIAsset : Attribute
  {
    public Type DataType;

    public APIAsset(Type type)
    {
      this.DataType = type;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class APIAssetProp : Attribute
  {
    public string Name;
    public Type DataType;

    public APIAssetProp(string name, Type type)
    {
      this.Name = name;
      this.DataType = type;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class APIAssetPropValueRange : Attribute
  {
    public double Min = double.NaN;
    public double Max = double.NaN;

    public APIAssetPropValueRange(double min = double.NaN, double max = double.NaN)
    {
      this.Min = min;
      this.Max = max;
    }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class AssetGHComponent : Attribute
  {
    public string Name;
    public string NickName;
    public string Description;

    public AssetGHComponent(string name, string nickname, string description)
    {
      Name = name;
      NickName = nickname;
      Description = description;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class AssetGHParameter : Attribute
  {
    public Type ParamType;
    public string Name;
    public string NickName;
    public string Description;
    public bool Optional;

    public AssetGHParameter(Type param, string name, string nickname, string description, bool optional = true)
    {
      ParamType = param;
      Name = name;
      NickName = nickname;
      Description = description;
      Optional = optional;
    }
  }

  #endregion

  #region Base Types

  /// <summary>
  /// Base class for all Revit assets
  /// </summary>
  public abstract class AssetData
  {
    public abstract string Schema { get; }

    public PropertyInfo[] GetAssetProperties()
      => this.GetType().GetProperties();

    public AssetGHComponent GetGHComponentInfo()
    {
      return this.GetType()
                 .GetCustomAttributes(typeof(AssetGHComponent), false)
                 .Cast<AssetGHComponent>()
                 .FirstOrDefault();
    }

    public AssetGHParameter GetGHParameterInfo(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(AssetGHParameter), false)
                     .Cast<AssetGHParameter>()
                     .FirstOrDefault();
    }

    public APIAssetProp GetAPIAssetPropertyInfo(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(APIAssetProp), false)
                     .Cast<APIAssetProp>()
                     .FirstOrDefault();
    }

    public string GetSchemaPropertyName(PropertyInfo propInfo)
    {
      var apiAssetPropInfo = GetAPIAssetPropertyInfo(propInfo);
      if (apiAssetPropInfo is null)
        return null;

      APIAsset apiAssetInfo = this.GetType()
                                  .GetCustomAttributes(typeof(APIAsset), false)
                                  .Cast<APIAsset>()
                                  .FirstOrDefault();
      if (apiAssetInfo != null)
      {

        var dataPropInfo =
          apiAssetInfo.DataType.GetProperty(
            apiAssetPropInfo.Name,
            BindingFlags.Public | BindingFlags.Static
            );
        if (dataPropInfo != null)
          return (string) dataPropInfo.GetValue(null);
      }

      return null;
    }
  }

  /// <summary>
  /// Base class for all shader assets
  /// </summary>
  public abstract class ShaderData : AssetData {
    public abstract string Name { get; set; }
  }

  /// <summary>
  /// Base class for all texture assets
  /// </summary>
  public abstract class TextureData: AssetData {}

  #endregion

  #region Shader Assets
  [APIAsset(typeof(RDBV.Generic))]
  [AssetGHComponent("Appearance Asset (Generic)", "GA", "Generic Appearance Asset")]
  public class GenericData : ShaderData
  {
    public override string Schema => "Generic";

    [AssetGHParameter(typeof(Param_String), "Name", "N", "Asset name", false)]
    public override string Name { get; set; }

    [AssetGHParameter(typeof(Param_String), "Description", "D", "Asset description")]
    public string Description { get; set; }

    [AssetGHParameter(typeof(Param_String), "Keywords", "KW", "Asset keywords")]
    public string Keywords { get; set; }

    [APIAssetProp("GenericDiffuse", typeof(RDBV.AssetPropertyDoubleArray4d))]
    [AssetGHParameter(typeof(Param_Colour), "Color", "C", "Diffuse color")]
    public System.Drawing.Color Color { get; set; }
  }
  #endregion

  #region 2D Texture Assets

  /// <summary>
  /// Base class providing shared 2d mapping properties among texture assets
  /// </summary>
  public abstract class TextureData2D : TextureData
  {
    [APIAssetProp("TextureLinkTextureTransforms", typeof(RDBV.AssetPropertyBoolean))]
    public bool TxLock { get; set; } = false;

    [APIAssetProp("TextureRealWorldOffsetX", typeof(RDBV.AssetPropertyDistance))]
    [AssetGHParameter(typeof(Param_Number), "OffsetU", "OU", "Texture offset along U axis")]
    public double OffsetU { get; set; } = 0;

    [APIAssetProp("TextureRealWorldOffsetY", typeof(RDBV.AssetPropertyDistance))]
    [AssetGHParameter(typeof(Param_Number), "OffsetV", "OV", "Texture offset along V axis")]
    public double OffsetV { get; set; } = 0;

    [APIAssetProp("TextureOffsetLock", typeof(RDBV.AssetPropertyBoolean))]
    public bool OffsetLock { get; set; }

    [APIAssetProp("TextureRealWorldScaleX", typeof(RDBV.AssetPropertyDistance))]
    [APIAssetPropValueRange(min: 0.01)]
    [AssetGHParameter(typeof(Param_Number), "SizeU", "SU", "Texture size along U axis")]
    public double SizeU { get; set; }

    [APIAssetProp("TextureRealWorldScaleY", typeof(RDBV.AssetPropertyDistance))]
    [APIAssetPropValueRange(min: 0.01)]
    [AssetGHParameter(typeof(Param_Number), "SizeV", "SV", "Texture size along V axis")]
    public double SizeV { get; set; }

    [APIAssetProp("TextureScaleLock", typeof(RDBV.AssetPropertyBoolean))]
    public bool SizeLock { get; set; }

    [APIAssetProp("TextureURepeat", typeof(RDBV.AssetPropertyBoolean))]
    [AssetGHParameter(typeof(Param_Boolean), "RepeatU", "RU", "Texture repeat along U axis")]
    public bool RepeatU { get; set; } = false;

    [APIAssetProp("TextureVRepeat", typeof(RDBV.AssetPropertyBoolean))]
    [AssetGHParameter(typeof(Param_Boolean), "RepeatV", "RV", "Texture repeat along V axis")]
    public bool RepeatV { get; set; } = false;

    [APIAssetProp("TextureWAngle", typeof(RDBV.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 360)]
    [AssetGHParameter(typeof(Param_Number), "Angle", "A", "Texture angle")]
    public double Angle { get; set; }
  }

  [APIAsset(typeof(RDBV.UnifiedBitmap))]
  [AssetGHComponent("Bitmap Texture", "BT", "Bitmap Texture Data")]
  public class UnifiedBitmapData : TextureData2D
  {
    public override string Schema => "UnifiedBitmap";

    [APIAssetProp("UnifiedbitmapBitmap", typeof(RDBV.AssetPropertyString))]
    [AssetGHParameter(typeof(Param_String), "Source", "S", "Full path of bitmap texture source image file", false)]
    public string SourceFile { get; set; }

    [APIAssetProp("UnifiedbitmapInvert", typeof(RDBV.AssetPropertyBoolean))]
    [AssetGHParameter(typeof(Param_Boolean), "Invert", "I", "Invert source image colors")]
    public bool Invert { get; set; }

    [APIAssetProp("UnifiedbitmapRGBAmount", typeof(RDBV.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 1)]
    [AssetGHParameter(typeof(Param_Boolean), "Brightness", "B", "Texture brightness")]
    public double Brightness { get; set; } = 0;
  }

  [APIAsset(typeof(RDBV.Checker))]
  [AssetGHComponent("Checker Texture", "CT", "Checker Texture Data")]
  public class CheckerData : TextureData2D
  {
    public override string Schema => "Checker";

    [APIAssetProp("CheckerColor1", typeof(RDBV.AssetPropertyDoubleArray4d))]
    [AssetGHParameter(typeof(Param_Colour), "Color1", "C1", "First color", false)]
    public System.Drawing.Color Color1 { get; set; }

    [APIAssetProp("CheckerColor2", typeof(RDBV.AssetPropertyDoubleArray4d))]
    [AssetGHParameter(typeof(Param_Colour), "Color2", "C2", "Second color", false)]
    public System.Drawing.Color Color2 { get; set; }

    [APIAssetProp("CheckerSoften", typeof(RDBV.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 5)]
    [AssetGHParameter(typeof(Param_Number), "Soften Amount", "S", "Amount of softening")]
    public double SoftenAmount { get; set; }
  }

  #endregion

  #region 3D Texture Assets
  /// <summary>
  /// Base class providing shared 3d mapping properties among texture assets
  /// </summary>
  public abstract class TextureData3D : TextureData
  {

  }
  #endregion

  #endregion
}
