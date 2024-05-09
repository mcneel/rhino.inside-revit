using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using ARDB = Autodesk.Revit.DB;
#if RHINO_8
using Grasshopper.Rhinoceros;
using Grasshopper.Rhinoceros.Render;
#endif

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Render;
  using Convert.System.Drawing;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Material")]
  public class Material : Element, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.Material);
    public new ARDB.Material Value => base.Value as ARDB.Material;

    public Material() { }
    public Material(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Material(ARDB.Material value) : base(value) { }

    public override bool CastFrom(object source)
    {
      switch (source)
      {
        case GeometryFace face:
          if (face.Material is Material material)
          {
            SetValue(material.Document, material.Id);
            return true;
          }
          break;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo<Q>(out target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(Grasshopper.Kernel.Types.GH_Colour)))
      {
        target = (Q) (object) new Grasshopper.Kernel.Types.GH_Colour(ObjectColor);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(Grasshopper.Kernel.Types.GH_Material)))
      {
        if (RhinoDoc.ActiveDoc is RhinoDoc doc)
        {
          if(Value?.ToRenderMaterial(doc) is Rhino.Render.RenderMaterial renderMaterial)
            target = (Q) (object) new Grasshopper.Kernel.Types.GH_Material(renderMaterial);
          else
            target = default;

          return true;
        }
      }

#if RHINO_8
      if (typeof(Q).IsAssignableFrom(typeof(ModelRenderMaterial)))
      {
        target = (Q) (object) ToModelContent(new Dictionary<ARDB.ElementId, ModelContent>());
        return true;
      }
#endif

      return false;
    }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is ARDB.Material material)
      {
        // 2. Check if already exist
        var index = doc.Materials.Find(material.Name, true);
        var mat = index < 0 ?
          new Rhino.DocObjects.Material() { Name = material.Name } :
          doc.Materials[index];

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
#if REVIT_2018
          if (AppearanceAsset is AppearanceAssetElement asset)
          {
            if (asset.BakeRenderMaterial(overwrite, doc, material.Name, out var renderMaterialId))
            {
              if (Rhino.Render.RenderContent.FromId(doc, renderMaterialId) is Rhino.Render.RenderMaterial renderMaterial)
              {
                renderMaterial.SimulateMaterial(ref mat, Rhino.Render.RenderTexture.TextureGeneration.Allow);

                if (mat.Name != material.Name)
                {
                  mat.Name = material.Name;
                  mat.RenderMaterialInstanceId = Guid.Empty;
                }
                else mat.RenderMaterialInstanceId = renderMaterialId;
              }
            }
          }
          else
#endif
          {
            mat.DiffuseColor = material.Color.ToColor();
            mat.Shine = material.Shininess / 128.0 * Rhino.DocObjects.Material.MaxShine;
            mat.Reflectivity = 1.0 / Math.Exp((1.0 - (material.Smoothness / 100.0)) * 10);
            mat.Transparency = material.Transparency / 100.0;
          }

          if (index < 0) { index = doc.Materials.Add(mat); mat = doc.Materials[index]; }
          else if (overwrite) doc.Materials.Modify(mat, index, true);
        }

        idMap.Add(Id, guid = mat.Id);
        return true;
      }

      return false;
    }

    internal System.Drawing.Color ObjectColor
    {
      get
      {
        if (Value is ARDB.Material material)
        {
          var color = System.Drawing.Color.FromArgb
          (
            255 - (int) Math.Round(material.Transparency / 100.0 * 255.0),
            material.Color.ToColor()
          );

          return color;
        }

        return System.Drawing.Color.Empty;
      }
    }
    #endregion

    #region ModelContent
#if RHINO_8
    internal ModelContent ToModelContent(IDictionary<ARDB.ElementId, ModelContent> idMap)
    {
      if (idMap.TryGetValue(Id, out var modelContent))
        return modelContent;

      if (Value is ARDB.Material material)
      {
        var attributes = new ModelRenderMaterial.Attributes()
        {
          Path = material.Name,
          RenderMaterial = material.ToRenderMaterial(Grasshopper.Instances.ActiveRhinoDoc)
        };

        idMap.Add(Id, modelContent = attributes.ToModelData() as ModelContent);
        return modelContent;
      }

      return null;
    }
#endif
    #endregion

    #region Identity
    public string MaterialClass
    {
      get => Value?.MaterialClass;
      set
      {
        if (value is object && Value?.MaterialClass != value)
          Value.MaterialClass = value;
      }
    }

    public string MaterialCategory
    {
      get => Value?.MaterialCategory;
      set
      {
        if (value is object && Value?.MaterialCategory != value)
          Value.MaterialCategory = value;
      }
    }
    #endregion

    #region Identity Data
    public virtual string Description
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.Update(value);
      }
    }

    public string Comments
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Update(value);
      }
    }

    public string Manufacturer
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MANUFACTURER)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MANUFACTURER)?.Update(value);
      }
    }

    public string Model
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MODEL)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MODEL)?.Update(value);
      }
    }

    public double? Cost
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_COST)?.AsDouble();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_COST)?.Update(value.Value);
      }
    }

    public string Url
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_URL)?.Update(value);
      }
    }

    public string Keynote
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.KEYNOTE_PARAM)?.AsString();
      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.KEYNOTE_PARAM)?.Update(value);
      }
    }

    public virtual string Mark
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK) is ARDB.Parameter parameter &&
        parameter.HasValue ?
        parameter.AsString() :
        default;

      set
      {
        if (value is object)
          Value?.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_MARK)?.Update(value);
      }
    }
    #endregion

    #region Graphics
    public bool? UseRenderAppearanceForShading
    {
      get => Value?.UseRenderAppearanceForShading;
      set
      {
        if (value is object && Value?.UseRenderAppearanceForShading != value)
          Value.UseRenderAppearanceForShading = value.Value;
      }
    }

    public System.Drawing.Color? Color
    {
      get => Value?.Color.ToColor();
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          var materialColor = material.Color;
          var valueColor = value.Value;

          if(materialColor.ToColor() != valueColor)
            material.Color = valueColor.ToColor();
        }
      }
    }

    public double? Transparency
    {
      get => Value?.Transparency / 100.0;
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 100.0);
          if(0.0 > intValue || intValue > 100)
            throw new ArgumentOutOfRangeException(nameof(Transparency), "Valid value range for transparency is [0.0, 1.0]");

          if (material.Transparency != intValue)
            material.Transparency = intValue;
        }
      }
    }

    public double? Shininess
    {
      get => Value?.Shininess / 128.0;
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 128.0);
          if (0.0 > intValue || intValue > 128)
            throw new ArgumentOutOfRangeException(nameof(Shininess), "Valid value range for shininess is [0.0, 1.0]");

          if (material.Shininess != intValue)
            material.Shininess = intValue;
        }
      }
    }    

    public double? Smoothness
    {
      get => Value?.Smoothness / 100.0;
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 100.0);
          if (0.0 > intValue || intValue > 100)
            throw new ArgumentOutOfRangeException(nameof(Smoothness), "Valid value range for smoothness is [0.0, 1.0]");

          if (material.Smoothness != intValue)
            material.Smoothness = intValue;
        }
      }
    }

#if REVIT_2019
    public FillPatternElement SurfaceForegroundPattern
    {
      get => SurfaceForegroundPatternId is ARDB.ElementId id ? GetElement(new FillPatternElement(Document, id)) : default;
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.SurfaceForegroundPatternId)
        {
          AssertValidDocument(value, nameof(SurfaceForegroundPattern));
          material.SurfaceForegroundPatternId = value.Id;
        }
      }
    }

    public ARDB.ElementId SurfaceForegroundPatternId
    {
      get => Value?.SurfaceForegroundPatternId;
      set
      {
        if(value is object && Value is ARDB.Material material && value != material.SurfaceForegroundPatternId)
          material.SurfaceForegroundPatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceForegroundPatternColor
    {
      get => Value?.SurfaceForegroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          if (material.SurfaceForegroundPatternColor.ToColor() != value.Value)
            material.SurfaceForegroundPatternColor = value.Value.ToColor();
        }
      }
    }

    public FillPatternElement SurfaceBackgroundPattern
    {
      get => SurfaceBackgroundPatternId is ARDB.ElementId id ? GetElement(new FillPatternElement(Document, id)) : default;
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.SurfaceBackgroundPatternId)
        {
          AssertValidDocument(value, nameof(SurfaceBackgroundPattern));
          material.SurfaceBackgroundPatternId = value.Id;
        }
      }
    }

    public ARDB.ElementId SurfaceBackgroundPatternId
    {
      get => Value?.SurfaceBackgroundPatternId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.SurfaceBackgroundPatternId)
          material.SurfaceBackgroundPatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceBackgroundPatternColor
    {
      get => Value?.SurfaceBackgroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          if (material.SurfaceBackgroundPatternColor.ToColor() != value.Value)
            material.SurfaceBackgroundPatternColor = value.Value.ToColor();
        }
      }
    }

    public FillPatternElement CutForegroundPattern
    {
      get => CutForegroundPatternId is ARDB.ElementId id ? GetElement(new FillPatternElement(Document, id)) : default;
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.CutForegroundPatternId)
        {
          AssertValidDocument(value, nameof(CutForegroundPattern));
          material.CutForegroundPatternId = value.Id;
        }
      }
    }

    public ARDB.ElementId CutForegroundPatternId
    {
      get => Value?.CutForegroundPatternId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.CutForegroundPatternId)
          material.CutForegroundPatternId = value;
      }
    }

    public System.Drawing.Color? CutForegroundPatternColor
    {
      get => Value?.CutForegroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          if (material.CutForegroundPatternColor.ToColor() != value.Value)
            material.CutForegroundPatternColor = value.Value.ToColor();
        }
      }
    }

    public FillPatternElement CutBackgroundPattern
    {
      get => CutBackgroundPatternId is ARDB.ElementId id ? GetElement(new FillPatternElement(Document, id)) : default;
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.CutBackgroundPatternId)
        {
          AssertValidDocument(value, nameof(CutBackgroundPattern));
          material.CutBackgroundPatternId = value.Id;
        }
      }
    }

    public ARDB.ElementId CutBackgroundPatternId
    {
      get => Value?.CutBackgroundPatternId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.CutBackgroundPatternId)
          material.CutBackgroundPatternId = value;
      }
    }

    public System.Drawing.Color? CutBackgroundPatternColor
    {
      get => Value?.CutBackgroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          if (material.CutBackgroundPatternColor.ToColor() != value.Value)
            material.CutBackgroundPatternColor = value.Value.ToColor();
        }
      }
    }
#else
    public FillPatternElement SurfaceForegroundPattern
    {
      get => new FillPatternElement(Document, SurfaceForegroundPatternId);
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.SurfacePatternId)
        {
          AssertValidDocument(value, nameof(SurfaceForegroundPattern));
          material.SurfacePatternId = value.Id;
        }
      }
    }

    public ARDB.ElementId SurfaceForegroundPatternId
    {
      get => Value?.SurfacePatternId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.SurfacePatternId)
          material.SurfacePatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceForegroundPatternColor
    {
      get => Value?.SurfacePatternColor.ToColor();
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          if (material.SurfacePatternColor.ToColor() != value.Value)
            material.SurfacePatternColor = value.Value.ToColor();
        }
      }
    }

    public FillPatternElement SurfaceBackgroundPattern
    {
      get => default;
      set { }
    }

    public ARDB.ElementId SurfaceBackgroundPatternId
    {
      get => default;
      set { }
    }

    public System.Drawing.Color? SurfaceBackgroundPatternColor
    {
      get => default;
      set { }
    }

    public FillPatternElement CutForegroundPattern
    {
      get => new FillPatternElement(Document, CutForegroundPatternId);
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.CutPatternId)
        {
          AssertValidDocument(value, nameof(CutForegroundPattern));
          material.CutPatternId = value.Id;
        }
      }
    }

    public ARDB.ElementId CutForegroundPatternId
    {
      get => Value?.CutPatternId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.CutPatternId)
          material.CutPatternId = value;
      }
    }

    public System.Drawing.Color? CutForegroundPatternColor
    {
      get => Value?.CutPatternColor.ToColor();
      set
      {
        if (value is object && Value is ARDB.Material material)
        {
          if (material.CutPatternColor.ToColor() != value.Value)
            material.CutPatternColor = value.Value.ToColor();
        }
      }
    }

    public FillPatternElement CutBackgroundPattern
    {
      get => default;
      set { }
    }

    public ARDB.ElementId CutBackgroundPatternId
    {
      get => default;
      set { }
    }

    public System.Drawing.Color? CutBackgroundPatternColor
    {
      get => default;
      set { }
    }
#endif
    #endregion

    #region Assets
    public ARDB.ElementId AppearanceAssetId
    {
      get => Value?.AppearanceAssetId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.AppearanceAssetId)
          material.AppearanceAssetId = value;
      }
    }

    public AppearanceAssetElement AppearanceAsset
    {
      get => AppearanceAssetId is ARDB.ElementId id ? GetElement(new AppearanceAssetElement(Document, id)) : default;
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.AppearanceAssetId)
        {
          AssertValidDocument(value, nameof(AppearanceAssetId));
          material.AppearanceAssetId = value.Id;
        }
      }
    }

    public ARDB.ElementId StructuralAssetId
    {
      get => Value?.StructuralAssetId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.StructuralAssetId)
          material.StructuralAssetId = value;
      }
    }

    public StructuralAssetElement StructuralAsset
    {
      get => StructuralAssetId is ARDB.ElementId id ? GetElement(new StructuralAssetElement(Document, id)) : default;
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.StructuralAssetId)
        {
          AssertValidDocument(value, nameof(StructuralAssetId));
          material.StructuralAssetId = value.Id;
        }
      }
    }

    public ARDB.ElementId ThermalAssetId
    {
      get => Value?.ThermalAssetId;
      set
      {
        if (value is object && Value is ARDB.Material material && value != material.ThermalAssetId)
          material.ThermalAssetId = value;
      }
    }

    public ThermalAssetElement ThermalAsset
    {
      get => ThermalAssetId is ARDB.ElementId id ? GetElement(new ThermalAssetElement(Document, id)) : default;
      set
      {
        if (value is object && Value is ARDB.Material material && value.Id != material.ThermalAssetId)
        {
          AssertValidDocument(value, nameof(ThermalAssetId));
          material.ThermalAssetId = value.Id;
        }
      }
    }
    #endregion
  }
}
