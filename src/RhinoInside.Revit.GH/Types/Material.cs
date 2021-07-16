using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using RhinoInside.Revit.Convert.System.Drawing;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Material")]
  public class Material : Element, Bake.IGH_BakeAwareElement
  {
    protected override Type ScriptVariableType => typeof(DB.Material);
    public new DB.Material Value => base.Value as DB.Material;

    public Material() { }
    public Material(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public Material(DB.Material value) : base(value) { }

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
          var renderMaterial = Rhino.Render.RenderMaterial.CreateBasicMaterial(Rhino.DocObjects.Material.DefaultMaterial, doc);
          renderMaterial.Name = Name;

#if REVIT_2018
          if (AppearanceAsset?.Value is DB.AppearanceAssetElement appearance)
          {
            using (var asset = appearance.GetRenderingAsset())
              AppearanceAssetElement.SimulateRenderMaterial(renderMaterial, asset, doc);
          }
          else 
#endif
          if (Value is DB.Material material)
          {
            renderMaterial.Fields.Set(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Diffuse, material.Color.ToColor());
            renderMaterial.Fields.Set(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Shine, material.Shininess / 128.0 * Rhino.DocObjects.Material.MaxShine);
            renderMaterial.Fields.Set(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Reflectivity, 1.0 / Math.Exp((1.0 - (material.Smoothness / 100.0)) * 10));
            renderMaterial.Fields.Set(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Transparency, material.Transparency / 100.0);
          }

          if(renderMaterial is null)
            target = default;
          else
            target = (Q) (object) new Grasshopper.Kernel.Types.GH_Material(renderMaterial);

          return true;
        }
      }

      return false;
    }

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is DB.Material material)
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
                renderMaterial.SimulateMaterial(ref mat, false);

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
        if (Value is DB.Material material)
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
        if (value is object && Value is DB.Material material)
        {
          var materialColor = material.Color;
          var valueColor = value.Value;

          if (materialColor.Red != valueColor.R || materialColor.Green != valueColor.G || materialColor.Blue != valueColor.B)
            material.Color = valueColor.ToColor();
        }
      }
    }

    public double? Transparency
    {
      get => Value?.Transparency / 100.0;
      set
      {
        if (value is object && Value is DB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 100.0);
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
        if (value is object && Value is DB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 128.0);
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
        if (value is object && Value is DB.Material material)
        {
          var intValue = (int) Math.Round(value.Value * 100.0);
          if (material.Smoothness != intValue)
            material.Smoothness = intValue;
        }
      }
    }

#if REVIT_2019
    public FillPatternElement SurfaceForegroundPattern
    {
      get => SurfaceForegroundPatternId is DB.ElementId id ? new FillPatternElement(Document, id) : default;
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.SurfaceForegroundPatternId)
        {
          AssertValidDocument(value, nameof(SurfaceForegroundPattern));
          material.SurfaceForegroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId SurfaceForegroundPatternId
    {
      get => Value?.SurfaceForegroundPatternId;
      set
      {
        if(value is object && Value is DB.Material material && value != material.SurfaceForegroundPatternId)
          material.SurfaceForegroundPatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceForegroundPatternColor
    {
      get => Value?.SurfaceForegroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.SurfaceForegroundPatternColor)
              material.SurfaceForegroundPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement SurfaceBackgroundPattern
    {
      get => SurfaceBackgroundPatternId is DB.ElementId id ? new FillPatternElement(Document, id) : default;
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.SurfaceBackgroundPatternId)
        {
          AssertValidDocument(value, nameof(SurfaceBackgroundPattern));
          material.SurfaceBackgroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId SurfaceBackgroundPatternId
    {
      get => Value?.SurfaceBackgroundPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.SurfaceBackgroundPatternId)
          material.SurfaceBackgroundPatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceBackgroundPatternColor
    {
      get => Value?.SurfaceBackgroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.SurfaceBackgroundPatternColor)
              material.SurfaceBackgroundPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement CutForegroundPattern
    {
      get => CutForegroundPatternId is DB.ElementId id ? new FillPatternElement(Document, id) : default;
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.CutForegroundPatternId)
        {
          AssertValidDocument(value, nameof(CutForegroundPattern));
          material.CutForegroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId CutForegroundPatternId
    {
      get => Value?.CutForegroundPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.CutForegroundPatternId)
          material.CutForegroundPatternId = value;
      }
    }

    public System.Drawing.Color? CutForegroundPatternColor
    {
      get => Value?.CutForegroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.CutForegroundPatternColor)
              material.CutForegroundPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement CutBackgroundPattern
    {
      get => CutBackgroundPatternId is DB.ElementId id ? new FillPatternElement(Document, id) : default;
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.CutBackgroundPatternId)
        {
          AssertValidDocument(value, nameof(CutBackgroundPattern));
          material.CutBackgroundPatternId = value.Id;
        }
      }
    }

    public DB.ElementId CutBackgroundPatternId
    {
      get => Value?.CutBackgroundPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.CutBackgroundPatternId)
          material.CutBackgroundPatternId = value;
      }
    }

    public System.Drawing.Color? CutBackgroundPatternColor
    {
      get => Value?.CutBackgroundPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.CutBackgroundPatternColor)
              material.CutBackgroundPatternColor = color;
          }
        }
      }
    }
#else
    public FillPatternElement SurfaceForegroundPattern
    {
      get => new FillPatternElement(Document, SurfaceForegroundPatternId);
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.SurfacePatternId)
        {
          AssertValidDocument(value.Document, nameof(SurfaceForegroundPattern));
          material.SurfacePatternId = value.Id;
        }
      }
    }

    public DB.ElementId SurfaceForegroundPatternId
    {
      get => Value?.SurfacePatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.SurfacePatternId)
          material.SurfacePatternId = value;
      }
    }

    public System.Drawing.Color? SurfaceForegroundPatternColor
    {
      get => Value?.SurfacePatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.SurfacePatternColor)
              material.SurfacePatternColor = color;
          }
        }
      }
    }

    public FillPatternElement SurfaceBackgroundPattern
    {
      get => default;
      set { }
    }

    public DB.ElementId SurfaceBackgroundPatternId
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
        if (value is object && Value is DB.Material material && value.Id != material.CutPatternId)
        {
          AssertValidDocument(value.Document, nameof(CutForegroundPattern));
          material.CutPatternId = value.Id;
        }
      }
    }

    public DB.ElementId CutForegroundPatternId
    {
      get => Value?.CutPatternId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.CutPatternId)
          material.CutPatternId = value;
      }
    }

    public System.Drawing.Color? CutForegroundPatternColor
    {
      get => Value?.CutPatternColor.ToColor();
      set
      {
        if (value is object && Value is DB.Material material)
        {
          using (var color = value.Value.ToColor())
          {
            if (color != material.CutPatternColor)
              material.CutPatternColor = color;
          }
        }
      }
    }

    public FillPatternElement CutBackgroundPattern
    {
      get => default;
      set { }
    }

    public DB.ElementId CutBackgroundPatternId
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
    public DB.ElementId AppearanceAssetId
    {
      get => Value?.AppearanceAssetId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.AppearanceAssetId)
          material.AppearanceAssetId = value;
      }
    }

    public AppearanceAssetElement AppearanceAsset
    {
      get => AppearanceAssetId is DB.ElementId id ? new AppearanceAssetElement(Document, id) : default;
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.AppearanceAssetId)
        {
          AssertValidDocument(value, nameof(AppearanceAssetId));
          material.AppearanceAssetId = value.Id;
        }
      }
    }

    public DB.ElementId StructuralAssetId
    {
      get => Value?.StructuralAssetId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.StructuralAssetId)
          material.StructuralAssetId = value;
      }
    }

    public StructuralAssetElement StructuralAsset
    {
      get => StructuralAssetId is DB.ElementId id ? new StructuralAssetElement(Document, id) : default;
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.StructuralAssetId)
        {
          AssertValidDocument(value, nameof(StructuralAssetId));
          material.StructuralAssetId = value.Id;
        }
      }
    }

    public DB.ElementId ThermalAssetId
    {
      get => Value?.ThermalAssetId;
      set
      {
        if (value is object && Value is DB.Material material && value != material.ThermalAssetId)
          material.ThermalAssetId = value;
      }
    }

    public ThermalAssetElement ThermalAsset
    {
      get => ThermalAssetId is DB.ElementId id ? new ThermalAssetElement(Document, id) : default;
      set
      {
        if (value is object && Value is DB.Material material && value.Id != material.ThermalAssetId)
        {
          AssertValidDocument(value, nameof(ThermalAssetId));
          material.ThermalAssetId = value.Id;
        }
      }
    }
    #endregion
  }
}
