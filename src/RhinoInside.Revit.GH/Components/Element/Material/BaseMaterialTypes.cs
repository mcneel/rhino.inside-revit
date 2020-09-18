using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.Geometry;

namespace RhinoInside.Revit.GH.Components.Element.Material
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
  public class AssetPropertyDouble1DMap: AssetParameterFlex
  {
    public double Value = 0;
    public AssetPropertyDouble1DMap(TextureData tdata) : base(tdata) { }
    public AssetPropertyDouble1DMap(double value) : base()
      => Value = value;

    public static implicit operator AssetPropertyDouble1DMap(double val)
      => new AssetPropertyDouble1DMap(val);

    public static implicit operator double(AssetPropertyDouble1DMap val)
      => val.Value;
  }

  /// <summary>
  /// Parameter that can accept a single double 4d value or texture
  /// </summary>
  public class AssetPropertyDouble4DMap: AssetParameterFlex
  {
    public double Value1 = 0;
    public double Value2 = 0;
    public double Value3 = 0;
    public double Value4 = 0;
    public AssetPropertyDouble4DMap(TextureData tdata) : base(tdata) { }
    public AssetPropertyDouble4DMap(double one, double two, double three, double four)
    {
      Value1 = one; Value2 = two; Value3 = three; Value4 = four;
    }
    public AssetPropertyDouble4DMap(double val) : this(val, val, val, val) { }
    public AssetPropertyDouble4DMap(System.Drawing.Color color)
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

    public static implicit operator AssetPropertyDouble4DMap(System.Drawing.Color val)
    => new AssetPropertyDouble4DMap(val);

    public static implicit operator System.Drawing.Color(AssetPropertyDouble4DMap val)
      => val.ValueAsColor;
  }

  #endregion

  #region Wrappers for Revit Assets

  #region Attributes
  public enum ExtractMethod
  {
    AssetFirst,
    ValueFirst,
    AssetOnly,
    ValueOnly,
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class APIAsset : Attribute
  {
    public Type DataType;

    public APIAsset(Type type)
    {
      DataType = type;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class APIAssetProp : Attribute
  {
    public string Name;
    public bool Connectable;
    public Type DataType;
    public string Toggle;

    public APIAssetProp(string name, Type type, bool connectable = false, string toggle = null)
    {
      Name = name;
      Connectable = connectable;
      DataType = type;
      Toggle = toggle;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class NoAPIAssetProp : APIAssetProp
  {
    public NoAPIAssetProp(string name, Type type, bool connectable = false)
      : base(name, type, connectable)
    { }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class APIAssetPropValueRange : Attribute
  {
    public double Min = double.NaN;
    public double Max = double.NaN;

    public APIAssetPropValueRange(double min = double.NaN, double max = double.NaN)
    {
      Min = min;
      Max = max;
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
    public GH_ParamAccess ParamAccess;
    public ExtractMethod ExtractMethod;
    public bool Optional;

    public AssetGHParameter(Type param,
                            string name, string nickname, string description,
                            GH_ParamAccess access = GH_ParamAccess.item,
                            ExtractMethod method = ExtractMethod.AssetFirst,
                            bool optional = true)
    {
      ParamType = param;
      Name = name;
      NickName = nickname;
      Description = description;
      ParamAccess = access;
      ExtractMethod = method;
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
    public static AssetData GetSchemaDataType(string schema)
    {
      var rootType = typeof(AssetData);
      foreach (var exportedType in Assembly.GetAssembly(rootType).GetTypes())
        if (!exportedType.IsAbstract
              && rootType.IsAssignableFrom(exportedType))
        {
          var derivedInstance = (AssetData) Activator.CreateInstance(exportedType);
          if (derivedInstance.Schema == schema)
            return derivedInstance;
        }
      return null;
    }

    public abstract string Name { get; set; }

    public string Schema => GetAPIAssetInfo()?.DataType.Name;

    public override string ToString()
    {
      var ghCompInfo = GetGHComponentInfo();
      if (ghCompInfo != null)
#if DEBUG
          return $"{ghCompInfo.Name} ({Schema} Schema)";
#else
          return ghCompInfo.Name;
#endif
      return GetType().Name;
    }

    public PropertyInfo[] GetAssetProperties()
      => GetType().GetProperties();

    public APIAsset GetAPIAssetInfo()
    {
      return GetType().GetCustomAttributes(typeof(APIAsset), false)
                      .Cast<APIAsset>()
                      .FirstOrDefault();
    }

    public AssetGHComponent GetGHComponentInfo()
    {
      return GetType().GetCustomAttributes(typeof(AssetGHComponent), false)
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

    public APIAssetPropValueRange GetAPIAssetPropertyValueRange(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(APIAssetPropValueRange), false)
                     .Cast<APIAssetPropValueRange>()
                     .FirstOrDefault();
    }

    private string GetSchemaPropertyName(APIAssetProp apiAssetPropInfo)
    {
      var apiAssetInfo = GetAPIAssetInfo();
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

    public string GetSchemaPropertyName(PropertyInfo propInfo)
    {
      var apiAssetPropInfo = GetAPIAssetPropertyInfo(propInfo);
      if (apiAssetPropInfo is null)
        return null;

      if (apiAssetPropInfo is NoAPIAssetProp noApiAssetPropInfo)
        return noApiAssetPropInfo.Name;
      else
        return GetSchemaPropertyName(apiAssetPropInfo);
    }
  }

  /// <summary>
  /// Base class for all shader assets
  /// </summary>
  public class ShaderData : AssetData {
    public override string Name { get => ""; set { } }
  }

  /// <summary>
  /// Base class for all texture assets
  /// </summary>
  public class TextureData : AssetData
  {
    public override string Name { get => ""; set { } }
  }

  #endregion

  #region Shader Assets
  [APIAsset(typeof(DB.Visual.Generic))]
  [AssetGHComponent("Shader Asset (Generic)", "GA", "Shader asset of \"Generic\" schema")]
  public class GenericData : ShaderData
  {
    [NoAPIAssetProp("UIName", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameter(typeof(Param_String), "Name", "N", "Asset name", optional: false)]
    public override string Name { get; set; }

    [NoAPIAssetProp("description", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameter(typeof(Param_String), "Description", "D", "Asset description")]
    public string Description { get; set; }

    [NoAPIAssetProp("keyword", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameter(typeof(Param_String), "Keywords", "KW", "Asset keywords")]
    public string Keywords { get; set; }

    [APIAssetProp("GenericDiffuse", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameter(typeof(Param_Colour), "Color", "C", "Diffuse color", method: ExtractMethod.ValueOnly)]
    public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Black;

    [APIAssetProp("GenericDiffuse", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameter(typeof(Parameters.TextureData), "Image", "I", "Diffuse image", method: ExtractMethod.AssetOnly)]
    public TextureData Image { get; set; }

    [APIAssetProp("GenericDiffuseImageFade", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 1)]
    [AssetGHParameter(typeof(Param_Number), "Image Fade", "IF", "Diffuse image fade")]
    public double ImageFade { get; set; } = 1;

    [APIAssetProp("GenericGlossiness", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble1DMap), "Glossiness", "G", "Glossiness")]
    public AssetPropertyDouble1DMap Glossiness { get; set; }

    [APIAssetProp("GenericIsMetal", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameter(typeof(Param_Boolean), "Metallic Highlights", "MH", "Metallic highlights")]
    public bool Metallic { get; set; } = false;

    [APIAssetProp("GenericReflectivityAt0deg", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble1DMap), "Reflectivity (Direct)", "RD", "Direct property of Reflectivity")]
    public AssetPropertyDouble1DMap ReflectivityDirect { get; set; } = 0;

    [APIAssetProp("GenericReflectivityAt90deg", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble1DMap), "Reflectivity (Oblique)", "RO", "Oblique property of Reflectivity")]
    public AssetPropertyDouble1DMap ReflectivityOblique { get; set; } = 0;

    [APIAssetProp("GenericTransparency", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [APIAssetPropValueRange(min: 0, max: 1)]
    [AssetGHParameter(typeof(Param_Number), "Transparency", "T", "Transparency amount")]
    public double Transparency { get; set; } = 0;

    [APIAssetProp("GenericTransparency", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameter(typeof(Parameters.TextureData), "Transparency Image", "TI", "Transparency image", method: ExtractMethod.AssetOnly)]
    public TextureData TransparencyImage { get; set; }

    [APIAssetProp("GenericTransparencyImageFade", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 1)]
    [AssetGHParameter(typeof(Param_Number), "Transparency Image Fade", "TIF", "Transparency image fade")]
    public double TransparencyImageFade { get; set; } = 1;

    [APIAssetProp("GenericRefractionTranslucencyWeight", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [APIAssetPropValueRange(min: 0, max: 1)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble1DMap), "Translucency", "TL", "Translucency amount")]
    public AssetPropertyDouble1DMap Translucency { get; set; } = 0;

    [APIAssetProp("GenericRefractionIndex", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0.01, max: 5)]
    [AssetGHParameter(typeof(Param_Number), "Refraction Index", "RI", "Refraction index")]
    public double RefractionIndex { get; set; } = 1.52;  // Revit defaults to Glass

    [APIAssetProp("GenericCutoutOpacity", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble1DMap), "Cutout", "CO", "Cutout image")]
    public AssetPropertyDouble1DMap Cutout { get; set; } = 0;

    [APIAssetProp("GenericSelfIllumFilterMap", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble4DMap), "Illumination Filter Color", "LF", "Self-illumination filter color")]
    public AssetPropertyDouble4DMap IlluminationFilter { get; set; } = System.Drawing.Color.White;

    [APIAssetProp("GenericSelfIllumLuminance", typeof(DB.Visual.AssetPropertyDouble))]
    [AssetGHParameter(typeof(Param_Number), "Luminance", "L", "Self-illumination luminance amount")]
    public double Luminance { get; set; } = 0;

    [APIAssetProp("GenericSelfIllumColorTemperature", typeof(DB.Visual.AssetPropertyDouble))]
    [AssetGHParameter(typeof(Param_Number), "Color Temperature", "CT", "Self-illumination color temperature")]
    public double ColorTemperature { get; set; } = 6500;  // Revit default

    [APIAssetProp("GenericBumpMap", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble4DMap), "Bump Image", "BI", "Bump image")]
    public AssetPropertyDouble4DMap BumpImage { get; set; } = System.Drawing.Color.White;

    [APIAssetProp("GenericBumpAmount", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameter(typeof(Parameters.AssetPropertyDouble1DMap), "Bump Amount", "B", "Bump amount")]
    public AssetPropertyDouble1DMap Bump { get; set; } = 0;

    [APIAssetProp("CommonTintColor", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true, toggle: "CommonTintToggle")]
    [AssetGHParameter(typeof(Param_Colour), "Tint Color", "TC", "Tint color")]
    public System.Drawing.Color Tint { get; set; } = System.Drawing.Color.Black;
  }
  #endregion

  #region 2D Texture Assets

  /// <summary>
  /// Base class providing shared 2d mapping properties among texture assets
  /// </summary>
  public abstract class TextureData2D : TextureData
  {
    [APIAssetProp("TextureLinkTextureTransforms", typeof(DB.Visual.AssetPropertyBoolean))]
    public bool TxLock { get; set; } = false;

    [APIAssetProp("TextureRealWorldOffsetX", typeof(DB.Visual.AssetPropertyDistance))]
    [AssetGHParameter(typeof(Param_Number), "OffsetU", "OU", "Texture offset along U axis")]
    public double OffsetU { get; set; } = 0;

    [APIAssetProp("TextureRealWorldOffsetY", typeof(DB.Visual.AssetPropertyDistance))]
    [AssetGHParameter(typeof(Param_Number), "OffsetV", "OV", "Texture offset along V axis")]
    public double OffsetV { get; set; } = 0;

    [APIAssetProp("TextureOffsetLock", typeof(DB.Visual.AssetPropertyBoolean))]
    public bool OffsetLock { get; set; } = false;

    [APIAssetProp("TextureRealWorldScaleX", typeof(DB.Visual.AssetPropertyDistance))]
    [APIAssetPropValueRange(min: 0.01)]
    [AssetGHParameter(typeof(Param_Number), "SizeU", "SU", "Texture size along U axis")]
    public double SizeU { get; set; } = 1;

    [APIAssetProp("TextureRealWorldScaleY", typeof(DB.Visual.AssetPropertyDistance))]
    [APIAssetPropValueRange(min: 0.01)]
    [AssetGHParameter(typeof(Param_Number), "SizeV", "SV", "Texture size along V axis")]
    public double SizeV { get; set; } = 1;

    [APIAssetProp("TextureScaleLock", typeof(DB.Visual.AssetPropertyBoolean))]
    public bool SizeLock { get; set; } = false;

    [APIAssetProp("TextureURepeat", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameter(typeof(Param_Boolean), "RepeatU", "RU", "Texture repeat along U axis")]
    public bool RepeatU { get; set; } = true;

    [APIAssetProp("TextureVRepeat", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameter(typeof(Param_Boolean), "RepeatV", "RV", "Texture repeat along V axis")]
    public bool RepeatV { get; set; } = true;

    [APIAssetProp("TextureWAngle", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 360)]
    [AssetGHParameter(typeof(Param_Number), "Angle", "A", "Texture angle")]
    public double Angle { get; set; } = 0;
  }

  [APIAsset(typeof(DB.Visual.UnifiedBitmap))]
  [AssetGHComponent("Bitmap Asset", "BT", "Bitmap Asset")]
  public class UnifiedBitmapData : TextureData2D
  {
    [APIAssetProp("UnifiedbitmapBitmap", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameter(typeof(Param_String), "Source", "S", "Full path of bitmap texture source image file", optional: false)]
    public string SourceFile { get; set; }

    [APIAssetProp("UnifiedbitmapInvert", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameter(typeof(Param_Boolean), "Invert", "I", "Invert source image colors")]
    public bool Invert { get; set; } = false;

    [APIAssetProp("UnifiedbitmapRGBAmount", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 1)]
    [AssetGHParameter(typeof(Param_Number), "Brightness", "B", "Texture brightness")]
    public double Brightness { get; set; } = 1;

    public override string ToString()
    {
      return $"{base.ToString()} ({SourceFile})";
    }
  }

  [APIAsset(typeof(DB.Visual.Checker))]
  [AssetGHComponent("Checker Asset", "CT", "Checker Asset")]
  public class CheckerData : TextureData2D
  {

    [APIAssetProp("CheckerColor1", typeof(DB.Visual.AssetPropertyDoubleArray4d))]
    [AssetGHParameter(typeof(Param_Colour), "Color1", "C1", "First color")]
    public System.Drawing.Color Color1 { get; set; } = System.Drawing.Color.White;

    [APIAssetProp("CheckerColor2", typeof(DB.Visual.AssetPropertyDoubleArray4d))]
    [AssetGHParameter(typeof(Param_Colour), "Color2", "C2", "Second color")]
    public System.Drawing.Color Color2 { get; set; } = System.Drawing.Color.Black;

    [APIAssetProp("CheckerSoften", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRange(min: 0, max: 5)]
    [AssetGHParameter(typeof(Param_Number), "Soften Amount", "S", "Amount of softening")]
    public double SoftenAmount { get; set; } = 0;
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
