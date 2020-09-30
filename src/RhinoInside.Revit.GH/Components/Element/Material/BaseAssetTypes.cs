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
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.Material
{
#if REVIT_2019

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
  public class AssetPropertyDouble1DMap : AssetParameterFlex
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
  public class AssetPropertyDouble4DMap : AssetParameterFlex
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
          (int) (Value1 * 255),
          (int) (Value2 * 255),
          (int) (Value3 * 255),
          (int) (Value4 * 255)
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
  public class APIAssetAttribute : Attribute
  {
    public Type DataType;

    public APIAssetAttribute(Type type)
    {
      DataType = type;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class APIAssetPropAttribute : Attribute
  {
    public string Name;
    public bool Connectable;
    public Type DataType;

    public APIAssetPropAttribute(string name, Type type, bool connectable = false)
    {
      Name = name;
      Connectable = connectable;
      DataType = type;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class NoAPIAssetPropAttribute : APIAssetPropAttribute
  {
    public NoAPIAssetPropAttribute(string name, Type type, bool connectable = false)
      : base(name, type, connectable)
    { }
  }

  [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
  public class APIAssetBuiltInPropAttribute : Attribute
  {
    public BuiltInParameter ParamId;
    public Type DataType;
    public bool Generic;

    public APIAssetBuiltInPropAttribute(BuiltInParameter paramId, Type type, bool generic = true)
    {
      ParamId = paramId;
      DataType = type;
      Generic = generic;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class APIAssetTogglePropAttribute : Attribute
  {
    public string Name;

    public APIAssetTogglePropAttribute(string name)
    {
      Name = name;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class APIAssetPropValueRangeAttribute : Attribute
  {
    public double Min = double.NaN;
    public double Max = double.NaN;

    public APIAssetPropValueRangeAttribute(double min = double.NaN, double max = double.NaN)
    {
      Min = min;
      Max = max;
    }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class AssetGHComponentAttribute : Attribute
  {
    public string Name;
    public string NickName;
    public string Description;

    public AssetGHComponentAttribute(string name, string nickname, string description)
    {
      Name = name;
      NickName = nickname;
      Description = description;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class AssetGHParameterAttribute : Attribute
  {
    public Type ParamType;
    public string Name;
    public string NickName;
    public string Description;
    public GH_ParamAccess ParamAccess;
    public ExtractMethod ExtractMethod;
    public bool Modifiable;
    public bool Optional;

    public AssetGHParameterAttribute(Type param,
                            string name, string nickname, string description,
                            GH_ParamAccess access = GH_ParamAccess.item,
                            ExtractMethod method = ExtractMethod.ValueOnly,
                            bool modifiable = true,
                            bool optional = true)
    {
      ParamType = param;
      Name = name;
      NickName = nickname;
      Description = description;
      ParamAccess = access;
      ExtractMethod = method;
      Modifiable = modifiable;
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
    // list of properties that contain value
    private HashSet<string> _markedProps = new HashSet<string>();

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

    public PropertyInfo GetAssetProperty(string name)
      => GetType().GetProperty(name);

    public PropertyInfo[] GetAssetProperties()
      => GetType().GetProperties();

    public APIAssetAttribute GetAPIAssetInfo()
    {
      return GetType().GetCustomAttributes(typeof(APIAssetAttribute), false)
                      .Cast<APIAssetAttribute>()
                      .FirstOrDefault();
    }

    public AssetGHComponentAttribute GetGHComponentInfo()
    {
      return GetType().GetCustomAttributes(typeof(AssetGHComponentAttribute), false)
                      .Cast<AssetGHComponentAttribute>()
                      .FirstOrDefault();
    }

    public AssetGHParameterAttribute GetGHParameterInfo(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(AssetGHParameterAttribute), false)
                     .Cast<AssetGHParameterAttribute>()
                     .FirstOrDefault();
    }

    public APIAssetPropAttribute GetAPIAssetPropertyInfo(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(APIAssetPropAttribute), false)
                     .Cast<APIAssetPropAttribute>()
                     .FirstOrDefault();
    }

    private APIAssetTogglePropAttribute GetAPIAssetTogglePropertyInfo(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(APIAssetTogglePropAttribute), false)
                     .Cast<APIAssetTogglePropAttribute>()
                     .FirstOrDefault();
    }

    public APIAssetPropValueRangeAttribute GetAPIAssetPropertyValueRange(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(APIAssetPropValueRangeAttribute), false)
                     .Cast<APIAssetPropValueRangeAttribute>()
                     .FirstOrDefault();
    }

    private string GetSchemaPropertyName(string apiPropName)
    {
      var apiAssetInfo = GetAPIAssetInfo();
      if (apiAssetInfo != null)
      {
        var dataPropInfo =
          apiAssetInfo.DataType.GetProperty(
            apiPropName,
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

      if (apiAssetPropInfo is NoAPIAssetPropAttribute noApiAssetPropInfo)
        return noApiAssetPropInfo.Name;
      else
        return GetSchemaPropertyName(apiAssetPropInfo.Name);
    }

    public string GetSchemaTogglePropertyName(PropertyInfo propInfo)
    {
      var apiAssetTogglePropInfo = GetAPIAssetTogglePropertyInfo(propInfo);
      if (apiAssetTogglePropInfo is null)
        return null;
      return GetSchemaPropertyName(apiAssetTogglePropInfo.Name);
    }

    public void Mark(string propName) => _markedProps.Add(propName);
    public void UnMark(string propName) => _markedProps.Remove(propName);
    public bool IsMarked(string propName) => _markedProps.Contains(propName);
  }

  /// <summary>
  /// Base class for all appearance assets
  /// </summary>
  public abstract class AppearanceAssetData : AssetData
  {
  }

  /// <summary>
  /// Base class for all shader assets
  /// </summary>
  public class ShaderData : AppearanceAssetData
  {
    public override string Name { get => ""; set { } }
  }

  /// <summary>
  /// Base class for all texture assets
  /// </summary>
  public class TextureData : AppearanceAssetData
  {
    public override string Name { get => ""; set { } }
  }

  /// <summary>
  /// Base class for all structural and thermal assets
  /// </summary>
  public class PhysicalMaterialData : AssetData
  {
    public override string Name { get => ""; set { } }
    public DB.StructuralBehavior Behaviour { get; set; }

    public IEnumerable<APIAssetBuiltInPropAttribute> GetAPIAssetBuiltInPropertyInfos(PropertyInfo propInfo)
    {
      return propInfo.GetCustomAttributes(typeof(APIAssetBuiltInPropAttribute), false)
                     .Cast<APIAssetBuiltInPropAttribute>();
    }
  }

  #endregion

  #region Shader Assets
  [APIAssetAttribute(typeof(DB.Visual.Generic))]
  [AssetGHComponentAttribute("Shader Asset (Generic)", "GA", "Shader asset of \"Generic\" schema")]
  public class GenericData : ShaderData
  {
    [NoAPIAssetPropAttribute("UIName", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameterAttribute(typeof(Param_String), "Name", "N", "Asset name", optional: false, modifiable: false)]
    public override string Name { get; set; }

    [NoAPIAssetPropAttribute("description", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameterAttribute(typeof(Param_String), "Description", "D", "Asset description")]
    public string Description { get; set; }

    [NoAPIAssetPropAttribute("keyword", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameterAttribute(typeof(Param_String), "Keywords", "KW", "Asset keywords (Separated by :)")]
    public string Keywords { get; set; }

    [APIAssetPropAttribute("GenericDiffuse", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameterAttribute(typeof(Param_Colour), "Color", "C", "Diffuse color", method: ExtractMethod.ValueOnly)]
    public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Black;

    [APIAssetPropAttribute("GenericDiffuse", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.TextureData), "Image", "I", "Diffuse image", method: ExtractMethod.AssetOnly)]
    public TextureData Image { get; set; }

    [APIAssetPropAttribute("GenericDiffuseImageFade", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Image Fade", "IF", "Diffuse image fade")]
    public double ImageFade { get; set; } = 1;

    [APIAssetPropAttribute("GenericGlossiness", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble1DMap), "Glossiness", "G", "Glossiness", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble1DMap Glossiness { get; set; }

    [APIAssetPropAttribute("GenericIsMetal", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameterAttribute(typeof(Param_Boolean), "Metallic Highlights", "MH", "Metallic highlights")]
    public bool Metallic { get; set; } = false;

    [APIAssetPropAttribute("GenericReflectivityAt0deg", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble1DMap), "Reflectivity (Direct)", "RD", "Direct property of Reflectivity", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble1DMap ReflectivityDirect { get; set; } = 0;

    [APIAssetPropAttribute("GenericReflectivityAt90deg", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble1DMap), "Reflectivity (Oblique)", "RO", "Oblique property of Reflectivity", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble1DMap ReflectivityOblique { get; set; } = 0;

    [APIAssetPropAttribute("GenericTransparency", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Transparency", "T", "Transparency amount", method: ExtractMethod.ValueOnly)]
    public double Transparency { get; set; } = 0;

    [APIAssetPropAttribute("GenericTransparency", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.TextureData), "Transparency Image", "TI", "Transparency image", method: ExtractMethod.AssetOnly)]
    public TextureData TransparencyImage { get; set; }

    [APIAssetPropAttribute("GenericTransparencyImageFade", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Transparency Image Fade", "TIF", "Transparency image fade")]
    public double TransparencyImageFade { get; set; } = 1;

    [APIAssetPropAttribute("GenericRefractionTranslucencyWeight", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble1DMap), "Translucency", "TL", "Translucency amount", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble1DMap Translucency { get; set; } = 0;

    [APIAssetPropAttribute("GenericRefractionIndex", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRangeAttribute(min: 0.01, max: 5)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Refraction Index", "RI", "Refraction index")]
    public double RefractionIndex { get; set; } = 1.52;  // Revit defaults to Glass

    [APIAssetPropAttribute("GenericCutoutOpacity", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble1DMap), "Cutout", "CO", "Cutout image", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble1DMap Cutout { get; set; } = 0;

    [APIAssetPropAttribute("GenericSelfIllumFilterMap", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble4DMap), "Illumination Filter Color", "LF", "Self-illumination filter color", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble4DMap IlluminationFilter { get; set; } = System.Drawing.Color.White;

    [APIAssetPropAttribute("GenericSelfIllumLuminance", typeof(DB.Visual.AssetPropertyDouble))]
    [AssetGHParameterAttribute(typeof(Param_Number), "Luminance", "L", "Self-illumination luminance amount")]
    public double Luminance { get; set; } = 0;

    [APIAssetPropAttribute("GenericSelfIllumColorTemperature", typeof(DB.Visual.AssetPropertyDouble))]
    [AssetGHParameterAttribute(typeof(Param_Number), "Color Temperature", "CT", "Self-illumination color temperature")]
    public double ColorTemperature { get; set; } = 6500;  // Revit default

    [APIAssetPropAttribute("GenericBumpMap", typeof(DB.Visual.AssetPropertyDoubleArray4d), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble4DMap), "Bump Image", "BI", "Bump image", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble4DMap BumpImage { get; set; } = System.Drawing.Color.White;

    [APIAssetPropAttribute("GenericBumpAmount", typeof(DB.Visual.AssetPropertyDouble), connectable: true)]
    [AssetGHParameterAttribute(typeof(Parameters.AssetPropertyDouble1DMap), "Bump Amount", "B", "Bump amount", method: ExtractMethod.AssetFirst)]
    public AssetPropertyDouble1DMap Bump { get; set; } = 0;

    [APIAssetPropAttribute("CommonTintColor", typeof(DB.Visual.AssetPropertyDoubleArray4d))]
    [APIAssetTogglePropAttribute("CommonTintToggle")]
    [AssetGHParameterAttribute(typeof(Param_Colour), "Tint Color", "TC", "Tint color")]
    public System.Drawing.Color Tint { get; set; } = System.Drawing.Color.Black;
  }
  #endregion

  #region 2D Texture Assets

  /// <summary>
  /// Base class providing shared 2d mapping properties among texture assets
  /// </summary>
  public abstract class TextureData2D : TextureData
  {
    [APIAssetPropAttribute("TextureLinkTextureTransforms", typeof(DB.Visual.AssetPropertyBoolean))]
    public bool TxLock { get; set; } = false;

    [APIAssetPropAttribute("TextureRealWorldOffsetX", typeof(DB.Visual.AssetPropertyDistance))]
    [AssetGHParameterAttribute(typeof(Param_Number), "OffsetU", "OU", "Texture offset along U axis")]
    public double OffsetU { get; set; } = 0;

    [APIAssetPropAttribute("TextureRealWorldOffsetY", typeof(DB.Visual.AssetPropertyDistance))]
    [AssetGHParameterAttribute(typeof(Param_Number), "OffsetV", "OV", "Texture offset along V axis")]
    public double OffsetV { get; set; } = 0;

    [APIAssetPropAttribute("TextureOffsetLock", typeof(DB.Visual.AssetPropertyBoolean))]
    public bool OffsetLock { get; set; } = false;

    [APIAssetPropAttribute("TextureRealWorldScaleX", typeof(DB.Visual.AssetPropertyDistance))]
    [APIAssetPropValueRangeAttribute(min: 0.01)]
    [AssetGHParameterAttribute(typeof(Param_Number), "SizeU", "SU", "Texture size along U axis")]
    public double SizeU { get; set; } = 1;

    [APIAssetPropAttribute("TextureRealWorldScaleY", typeof(DB.Visual.AssetPropertyDistance))]
    [APIAssetPropValueRangeAttribute(min: 0.01)]
    [AssetGHParameterAttribute(typeof(Param_Number), "SizeV", "SV", "Texture size along V axis")]
    public double SizeV { get; set; } = 1;

    [APIAssetPropAttribute("TextureScaleLock", typeof(DB.Visual.AssetPropertyBoolean))]
    public bool SizeLock { get; set; } = false;

    [APIAssetPropAttribute("TextureURepeat", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameterAttribute(typeof(Param_Boolean), "RepeatU", "RU", "Texture repeat along U axis")]
    public bool RepeatU { get; set; } = true;

    [APIAssetPropAttribute("TextureVRepeat", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameterAttribute(typeof(Param_Boolean), "RepeatV", "RV", "Texture repeat along V axis")]
    public bool RepeatV { get; set; } = true;

    [APIAssetPropAttribute("TextureWAngle", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 360)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Angle", "A", "Texture angle")]
    public double Angle { get; set; } = 0;
  }

  [APIAssetAttribute(typeof(DB.Visual.UnifiedBitmap))]
  [AssetGHComponentAttribute("Bitmap Asset", "BT", "Bitmap Asset")]
  public class UnifiedBitmapData : TextureData2D
  {
    [APIAssetPropAttribute("UnifiedbitmapBitmap", typeof(DB.Visual.AssetPropertyString))]
    [AssetGHParameterAttribute(typeof(Param_String), "Source", "S", "Full path of bitmap texture source image file", optional: false)]
    public string SourceFile { get; set; }

    [APIAssetPropAttribute("UnifiedbitmapInvert", typeof(DB.Visual.AssetPropertyBoolean))]
    [AssetGHParameterAttribute(typeof(Param_Boolean), "Invert", "I", "Invert source image colors")]
    public bool Invert { get; set; } = false;

    [APIAssetPropAttribute("UnifiedbitmapRGBAmount", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Brightness", "B", "Texture brightness")]
    public double Brightness { get; set; } = 1;

    public override string ToString()
    {
      return $"{base.ToString()} ({SourceFile})";
    }
  }

  [APIAssetAttribute(typeof(DB.Visual.Checker))]
  [AssetGHComponentAttribute("Checker Asset", "CT", "Checker Asset")]
  public class CheckerData : TextureData2D
  {

    [APIAssetPropAttribute("CheckerColor1", typeof(DB.Visual.AssetPropertyDoubleArray4d))]
    [AssetGHParameterAttribute(typeof(Param_Colour), "Color1", "C1", "First color")]
    public System.Drawing.Color Color1 { get; set; } = System.Drawing.Color.White;

    [APIAssetPropAttribute("CheckerColor2", typeof(DB.Visual.AssetPropertyDoubleArray4d))]
    [AssetGHParameterAttribute(typeof(Param_Colour), "Color2", "C2", "Second color")]
    public System.Drawing.Color Color2 { get; set; } = System.Drawing.Color.Black;

    [APIAssetPropAttribute("CheckerSoften", typeof(DB.Visual.AssetPropertyDouble))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 5)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Soften Amount", "S", "Amount of softening")]
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

  #region Structural and Thermal Assets

  // GUI: Values are not represented in the material editor
  //DA.SetData("", structAsset?.MetalReductionFactor);
  //DA.SetData("", structAsset?.MetalResistanceCalculationStrength);

  // API: Values are not represented in the API
  // BuiltInParameter.PROPERTY_SET_KEYWORDS
  //DA.SetData("Tension Parallel to Grain", );
  //DA.SetData("Tension Perpendicular to Grain", );
  //DA.SetData("Average Modulus", );
  //DA.SetData("Construction", );

  [APIAssetAttribute(typeof(DB.StructuralAsset))]
  [AssetGHComponentAttribute("Physical Asset", "PHAST", "Physical Asset")]
  public class StructuralAssetData : PhysicalMaterialData
  {
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PROPERTY_SET_NAME, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Name", "N", "Physical asset name", optional: false, modifiable: false)]
    public new string Name { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_CLASS, typeof(DB.StructuralAssetClass))]
    [AssetGHParameterAttribute(typeof(Parameters.Param_Enum<Types.StructuralAssetClass>), "Type", "T", "Physical asset type", optional: false, modifiable: false)]
    public DB.StructuralAssetClass Type { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Subclass", "SC", "Physical asset subclass")]
    public string SubClass { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PROPERTY_SET_DESCRIPTION, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Description", "D", "Physical asset description")]
    public string Description { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Source", "S", "Physical asset source")]
    public string Source { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Source URL", "SU", "Physical asset source url")]
    public string SourceURL { get; set; }

    // behaviour
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR, typeof(DB.StructuralBehavior))]
    [AssetGHParameterAttribute(typeof(Parameters.Param_Enum<Types.StructuralBehavior>), "Behaviour", "B", "Physical asset behaviour", modifiable: false)]
    public new DB.StructuralBehavior Behaviour { get; set; }

    // basic thermal
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF1, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF_1, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 0.00028)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Thermal Expansion Coefficient X", "TECX", "The only, X or 1 component of thermal expansion coefficient (depending on behaviour) [The value is in inverse Kelvin]")]
    public double ThermalExpansionCoefficientX { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF2, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF_2, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 0.00028)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Thermal Expansion Coefficient Y", "TECY", "Y or 2 component of thermal expansion coefficient (depending on behaviour) [The value is in inverse Kelvin]")]
    public double ThermalExpansionCoefficientY { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF3, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 0.00028)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Thermal Expansion Coefficient Z", "TECZ", "Z component of thermal expansion coefficient (depending on behaviour) [The value is in inverse Kelvin]")]
    public double ThermalExpansionCoefficientZ { get; set; }

    // mechanical
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD1, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD_1, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 188549.06)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Youngs Modulus X", "YMX", "The only, X, or 1 component of young's modulus (depending on behaviour) [The value is in Newtons per foot meter]")]
    public double YoungsModulusX { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD2, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD_2, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 188549.06)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Youngs Modulus Y", "YMY", "Y, or 1 component of young's modulus (depending on behaviour) [The value is in Newtons per foot meter]")]
    public double YoungsModulusY { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD3, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 188549.06)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Youngs Modulus Z", "YMZ", "Z component of young's modulus (depending on behaviour) [The value is in Newtons per foot meter]")]
    public double YoungsModulusZ { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD1, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD_12, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 1.0)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Poissons Ratio X", "PRX", "The only, X, or 12 component of poisson's ratio (depending on behaviour)")]
    public double PoissonsRatioX { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD2, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD_23, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 1.0)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Poissons Ratio Y", "PRY", "Y, or 23 component of poisson's ratio (depending on behaviour)")]
    public double PoissonsRatioY { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD3, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 1.0)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Poissons Ratio Z", "PRZ", "Z component of poisson's ratio (depending on behaviour)")]
    public double PoissonsRatioZ { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD1, typeof(double))]
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD_12, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 72518.87)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Shear Modulus X", "SMX", "The only, X, or 12 component of poisson's ratio (depending on behaviour) [The value is in Newtons per foot meter]")]
    public double ShearModulusX { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD2, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 72518.87)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Shear Modulus Y", "SMY", "Y component of poisson's ratio (depending on behaviour) [The value is in Newtons per foot meter]")]
    public double ShearModulusY { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD3, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 72518.87)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Shear Modulus Z", "SMZ", "Z component of poisson's ratio (depending on behaviour) [The value is in Newtons per foot meter]")]
    public double ShearModulusZ { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: -9.39E+15, max: 3.75E+19)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Density", "D", "Physical asset density")]
    public double Density { get; set; }

    // concrete
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_CONCRETE_COMPRESSION, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.01, max: 116.03)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Concrete Compression", "CC", "Physical asset concrete compression [The value is in Newtons per foot meter]")]
    public double ConcreteCompression { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_STRENGTH_REDUCTION, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: -2.94E12, max: 9.49E14)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Concrete Shear Strength Modification", "CSSM", "Physical asset concrete shear strength modification")]
    public double ConcreteShearStrengthModification { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_LIGHT_WEIGHT, typeof(bool))]
    [AssetGHParameterAttribute(typeof(Param_Boolean), "Concrete Lightweight", "CL", "Physical asset lightweight concrete")]
    public bool ConcreteLightweight { get; set; }

    // wood
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SPECIES, typeof(double), generic: false)]
    [AssetGHParameterAttribute(typeof(Param_String), "Wood Species", "WS", "Physical asset wood species")]
    public string WoodSpecies { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_GRADE, typeof(double))]
    [AssetGHParameterAttribute(typeof(Param_String), "Wood Strength Grade", "WSG", "Physical asset wood strength grade")]
    public string WoodStrengthGrade { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_BENDING, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 145.04)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Wood Bending", "WB", "Physical asset wood bending strength")]
    public double WoodBending { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_COMPRESSION_PARALLEL, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 145.04)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Wood Compression Parallel to Grain", "WCLG", "Physical asset wood compression parallel to grain")]
    public double WoodCompressionParallelGrain { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_COMPRESSION_PERPENDICULAR, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 14.50)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Wood Compression Perpendicular to Grain", "WCPG", "Physical asset wood compression perpendicular to grain")]
    public double WoodCompressionPerpendicularGrain { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_PARALLEL, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 145.04)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Wood Shear Parallel to Grain", "WSLG", "Physical asset wood shear parallel to grain")]
    public double WoodShearParallelGrain { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_PERPENDICULAR, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 14.50)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Wood Tension Perpendicular to Grain", "WTPG", "Physical asset wood tension perpendicular to grain")]
    public double WoodTensionPerpendicularGrain { get; set; }

    // shared
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 1450.38)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Yield Strength", "YS", "Physical asset yield strength [The value is in Newtons per foot meter]")]
    public double YieldStrength { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 14503.77)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Tensile Strength", "TS", "Physical asset tensile strength [The value is in Newtons per foot meter]")]
    public double TensileStrength { get; set; }
  }

  [APIAssetAttribute(typeof(DB.ThermalAsset))]
  [AssetGHComponentAttribute("Thermal Asset", "THAST", "Thermal Asset")]
  public class ThermalAssetData : PhysicalMaterialData
  {
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PROPERTY_SET_NAME, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Name", "N", "Thermal asset name", optional: false, modifiable: false)]
    public new string Name { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_CLASS, typeof(DB.ThermalMaterialType))]
    [AssetGHParameterAttribute(typeof(Parameters.Param_Enum<Types.ThermalMaterialType>), "Type", "T", "Thermal asset material asset type", optional: false, modifiable: false)]
    public DB.StructuralAssetClass Type { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_SUBCLASS, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Subclass", "SC", "Thermal asset subclass")]
    public string SubClass { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PROPERTY_SET_DESCRIPTION, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Description", "D", "Thermal asset description")]
    public string Description { get; set; }

    // Note: Keywords are not exposed by the API for the structural asset
    // Disabling thermal asset keywords for consistency
    //[APIAssetBuiltInProp(BuiltInParameter.PROPERTY_SET_KEYWORDS, typeof(string))]
    //[AssetGHParameter(typeof(Param_String), "Keywords", "K", "")]
    //public string Keywords { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Source", "S", "Thermal asset source")]
    public string Source { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.MATERIAL_ASSET_PARAM_SOURCE_URL, typeof(string))]
    [AssetGHParameterAttribute(typeof(Param_String), "Source URL", "SU", "Thermal asset source url")]
    public string SourceURL { get; set; }

    // behaviour
    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR, typeof(DB.StructuralBehavior))]
    [AssetGHParameterAttribute(typeof(Parameters.Param_Enum<Types.StructuralBehavior>), "Behaviour", "B", "Thermal asset behaviour", modifiable: false)]
    public new DB.StructuralBehavior Behaviour { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_TRANSMITS_LIGHT, typeof(bool), generic: false)]
    [AssetGHParameterAttribute(typeof(Param_Boolean), "Transmits Light", "TL", "Thermal asset transmits light")]
    public bool TransmitsLight { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_THERMAL_CONDUCTIVITY, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 2888.9466)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Thermal Conductivity", "TC", "Thermal asset thermal conductivity [The value is in feet-kilograms per Kelvin-cubed-second]")]
    public double ThermalConductivity { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_SPECIFIC_HEAT, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: 0, max: 3.5827)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Specific Heat", "SH", "Thermal asset specific heat [The value is in squared-feet per Kelvin, squared-second]")]
    public double SpecificHeat { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.PHY_MATERIAL_PARAM_STRUCTURAL_DENSITY, typeof(double))]
    [APIAssetPropValueRangeAttribute(min: -8.24E+16, max: 3.75E+23)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Density", "D", "Thermal asset density [The value is in kilograms per cubed feet]")]
    public double Density { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_EMISSIVITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0.01, max: 1.0)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Emissivity", "E", "Thermal asset emissivity")]
    public double Emissivity { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_PERMEABILITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 87.3920)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Permeability", "PE", "Thermal asset permeability [The value is in seconds per foot]")]
    public double Permeability { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_POROSITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0.01, max: 1.0)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Porosity", "PO", "Thermal asset porosity")]
    public double Porosity { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_REFLECTIVITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1.0)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Reflectivity", "R", "Thermal asset reflectivity")]
    public double Reflectivity { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_GAS_VISCOSITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 100000.00)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Gas Viscosity", "GV", "Thermal asset gas viscosity [The value is in kilograms per feet-second]")]
    public double GasViscosity { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_ELECTRICAL_RESISTIVITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1.0000E+24)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Electrical Resistivity", "ER", "Thermal asset electrical resistivity [The value is in ohm-meters]")]
    public double ElectricalResistivity { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_LIQUID_VISCOSITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 100000.00)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Liquid Viscosity", "LV", "Thermal asset liquid viscosity [The value is in kilograms per feet-second]")]
    public double LiquidViscosity { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_SPECIFIC_HEAT_OF_VAPORIZATION, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 1289.7678)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Specific Heat Of Vaporization", "SHV", "Thermal asset specific heat of vaporization [The value is in feet per squared-second]")]
    public double SpecificHeatVaporization { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_VAPOR_PRESSURE, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0, max: 14.50)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Vapor Pressure", "VP", "Thermal asset vapor pressure [The value is in kilograms per feet, squared-second]")]
    public double VaporPressure { get; set; }

    [APIAssetBuiltInPropAttribute(BuiltInParameter.THERMAL_MATERIAL_PARAM_COMPRESSIBILITY, typeof(double), generic: false)]
    [APIAssetPropValueRangeAttribute(min: 0.0, max: 1.0)]
    [AssetGHParameterAttribute(typeof(Param_Number), "Compressibility", "C", "Thermal asset compressibility")]
    public double Compressibility { get; set; }
  }
  #endregion

  #endregion

#endif
}
