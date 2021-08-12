using System;
using System.Linq;
#if REVIT_2018
using Autodesk.Revit.DB.Visual;
#else
using Autodesk.Revit.Utility;
#endif
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.Convert.System.Drawing;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
{
  public class MaterialByColor : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("273FF43D-B771-4EB7-A66D-5DA5F7F2731E");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

    public MaterialByColor() : base
    (
      name: "Convert Material",
      nickname: "Material",
      description: "Quickly create a new Revit material from a Shader or Color",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Document>
      (
        name: "Document",
        nickname: "DOC",
        optional: true,
        relevance: ParamRelevance.Occasional
      ),
      ParamDefinition.Create<Param_OGLShader>
      (
        name: "Shader",
        nickname: "S",
        description: "Shader or Color to use as template"
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Material>
      (
        name: _Material_,
        nickname: _Material_.Substring(0, 1),
        description: $"Output {_Material_}",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.AppearanceAsset>
      (
        name: _Asset_,
        nickname: _Asset_.Substring(0, 1),
        description: $"Output {_Asset_}",
        relevance: ParamRelevance.Occasional
      ),
    };

    const string _Material_ = "Material";
    const string _Asset_ = "Appearance Asset";

    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      DB.BuiltInParameter.MATERIAL_NAME
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.GetData(DA, "Shader", out GH_Material template)) return;

      if (name is null)
        name = GetMaterialName(template);

      if (Params.ReadTrackedElement(_Material_, doc.Value, out DB.Material material))
      {
        StartTransaction(doc.Value);
        material = Reconstruct(material, doc.Value, name, template);

        Params.WriteTrackedElement(_Material_, doc.Value, material);
        DA.SetData(_Material_, material);
      }

      if (Params.ReadTrackedElement(_Asset_, doc.Value, out DB.AppearanceAssetElement asset))
      {
        StartTransaction(doc.Value);
        asset = Reconstruct(asset, doc.Value, name, template);
        if (material is object && asset is object)
        {
          material.AppearanceAssetId = asset.Id;
          material.UseRenderAppearanceForShading = true;
        }
        else
        {
          material.AppearanceAssetId = DB.ElementId.InvalidElementId;
          material.UseRenderAppearanceForShading = false;
        }

        Params.WriteTrackedElement(_Asset_, doc.Value, asset);
        DA.SetData(_Asset_, asset);
      }
    }

    #region Utils
    static string GetMaterialName(GH_Material material)
    {
      var name = default(string);

      switch (material.Type)
      {
        case GH_Material.MaterialType.Shader: name = "Display Material"; break;
        case GH_Material.MaterialType.RhinoMaterial:
          if (Rhino.RhinoDoc.ActiveDoc is Rhino.RhinoDoc rhinoDoc)
          {
            using (var content = Rhino.Render.RenderContent.FromId(rhinoDoc, material.RdkMaterialId))
              name = content?.Name;
          }
          break;

        case GH_Material.MaterialType.XmlMaterial:
        {
          using (var content = Rhino.Render.RenderContent.FromXml(material.RdkMaterialXml, null))
            name = content?.Name;
        }
        break;

        case GH_Material.MaterialType.RmtlMaterial:
          name = System.IO.Path.GetFileName(material.RdkMaterialRmtl);
          break;
      }

      if (string.IsNullOrEmpty(name))
        name = "Rhino Material";

      return name;
    }
    #endregion

    #region Material
    bool Reuse(DB.Material material, string name, GH_Material template)
    {
      if (material is null) return false;
      if (name is object) material.Name = name;

      material.Color = template.Value.Diffuse.ToColor();
      material.Transparency = (int) Math.Round(template.Value.Transparency * 100.0);
      material.Shininess = (int) Math.Round(template.Value.Shine * 128.0);

      return true;
    }

    DB.Material CreateMaterial(DB.Document doc, string name, GH_Material template)
    {
      var material = default(DB.Material);

      // Make sure the name is unique
      {
        name = doc.GetNamesakeElements
        (
          typeof(DB.Material), name, categoryId: DB.BuiltInCategory.OST_Materials
        ).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
      }

      material = doc.GetElement(DB.Material.Create(doc, name)) as DB.Material;
      material.MaterialCategory = material.MaterialClass = "Display";

      if (template is object)
      {
        material.Color = template.Value.Diffuse.ToColor();
        material.Transparency = (int) Math.Round(template.Value.Transparency * 100.0);
        material.Shininess = (int) Math.Round(template.Value.Shine * 128.0);
      }

      return material;
    }

    DB.Material Reconstruct(DB.Material material, DB.Document doc, string name, GH_Material template)
    {
      if (!Reuse(material, name, template))
      {
        material = material.ReplaceElement
        (
          CreateMaterial(doc, name, template),
          ExcludeUniqueProperties
        );
      }

      return material;
    }
    #endregion

    #region AppearanceAssetElement
    bool Reuse(DB.AppearanceAssetElement assetElement, string name, GH_Material template)
    {
      if (assetElement is null) return false;
      if (name is object) assetElement.Name = name;

      using (var mat = template.MaterialBestGuess())
      using (var editScope = new AppearanceAssetEditScope(assetElement.Document))
      {
        var asset = editScope.Start(assetElement.Id);

        if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Diffuse, out Rhino.Display.Color4f diffuse))
        {
          var generic_diffuse = asset.FindByName(Generic.GenericDiffuse) as AssetPropertyDoubleArray4d;
          generic_diffuse.SetValueAsDoubles(new double[] { diffuse.R, diffuse.G, diffuse.B, diffuse.A });
        }

        if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.Transparency, out double transparency))
        {
          var generic_transparency = asset.FindByName(Generic.GenericTransparency) as AssetPropertyDouble;
          generic_transparency.Value = transparency;

          if (mat.Fields.TryGetValue(Rhino.Render.RenderMaterial.BasicMaterialParameterNames.TransparencyColor, out Rhino.Display.Color4f transparencyColor))
          {
            diffuse = diffuse.BlendTo((float) transparency, transparencyColor);

            var generic_diffuse = asset.FindByName(Generic.GenericDiffuse) as AssetPropertyDoubleArray4d;
            generic_diffuse.SetValueAsDoubles(new double[] { diffuse.R, diffuse.G, diffuse.B, diffuse.A });
          }
        }

        // TODO: Convert more fields

        editScope.Commit(false);
      }

      return true;
    }

    DB.AppearanceAssetElement CreateAppearanceAsset(DB.Document doc, string name, GH_Material template)
    {
      var assetElement = default(DB.AppearanceAssetElement);

      // Make sure the name is unique
      {
        name = doc.GetNamesakeElements
        (
          typeof(DB.AppearanceAssetElement), name, categoryId: DB.BuiltInCategory.INVALID
        ).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
      }

      var assets = doc.Application.GetAssets(AssetType.Appearance);
      var asset = assets.Where(x => x.Name == "Generic").FirstOrDefault();
      assetElement = DB.AppearanceAssetElement.Create(doc, name, asset);

      Reuse(assetElement, default, template);
      return assetElement;
    }

    DB.AppearanceAssetElement Reconstruct(DB.AppearanceAssetElement asset, DB.Document doc, string name, GH_Material template)
    {
      if (!Reuse(asset, name, template))
      {
        asset = asset.ReplaceElement
        (
          CreateAppearanceAsset(doc, name, template),
          ExcludeUniqueProperties
        );
      }

      return asset;
    }
    #endregion
  }
}
