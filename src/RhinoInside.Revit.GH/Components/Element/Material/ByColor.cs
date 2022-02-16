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
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Materials
{
  using Convert.System.Drawing;
  using External.DB.Extensions;
  using GH.ElementTracking;
  using External.ApplicationServices.Extensions;

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
      //new ParamDefinition
      //(
      //  new Param_String()
      //  {
      //    Name = "Name",
      //    NickName = "N",
      //    Description = "Material Name",
      //  },
      //  ParamRelevance.Occasional
      //),
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

    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties =
    {
      ARDB.BuiltInParameter.MATERIAL_NAME
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      // Input
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      if (!Params.GetData(DA, "Shader", out GH_Material template)) return;

      // Compute
      var templateName = GetTemplateName(template);

      var mat = ReconstructElement<ARDB.Material>
      (
        doc.Value, _Material_, (material) =>
        {
          StartTransaction(doc.Value);

          if (CanReconstruct(_Material_, out var untracked, ref material, doc.Value, name, categoryId: ARDB.BuiltInCategory.OST_Materials))
            material = Reconstruct(material, doc.Value, name, templateName, template);

          DA.SetData(_Material_, material);
          return untracked ? null : material;
        }
      );

      ReconstructElement<ARDB.AppearanceAssetElement>
      (
        doc.Value, _Asset_, (asset) =>
        {
          StartTransaction(doc.Value);

          if (CanReconstruct(_Asset_, out var untracked, ref asset, doc.Value, name, categoryId: ARDB.BuiltInCategory.INVALID))
          {
            asset = Reconstruct(asset, doc.Value, name, templateName, template);

            if (mat is object && asset is object)
            {
              mat.AppearanceAssetId = asset.Id;
              mat.UseRenderAppearanceForShading = true;
            }
          }

          DA.SetData(_Asset_, asset);
          return untracked ? null : asset;
        }
      );
    }

    #region Utils
    static string GetTemplateName(GH_Material material)
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
    bool Reuse(ARDB.Material material, string name, string templateName, GH_Material template)
    {
      if (material is null) return false;
      if (name is object) { if (material.Name != name) material.Name = name; }
      else material.SetIncrementalNomen(templateName ?? _Material_);

      material.Color = template.Value.Diffuse.ToColor();
      material.Transparency = (int) Math.Round(template.Value.Transparency * 100.0);
      material.Shininess = (int) Math.Round(template.Value.Shine * 128.0);

      return true;
    }

    ARDB.Material CreateMaterial(ARDB.Document doc, string name, string templateName, GH_Material template)
    {
      var material = default(ARDB.Material);

      // Make sure the name is unique
      {
        name = doc.NextIncrementalNomen
        (
          name ?? templateName ?? _Material_,
          typeof(ARDB.Material),
          categoryId: ARDB.BuiltInCategory.OST_Materials
        );
      }

      material = doc.GetElement(ARDB.Material.Create(doc, name)) as ARDB.Material;
      material.MaterialCategory = material.MaterialClass = "Display";

      if (template is object)
      {
        material.Color = template.Value.Diffuse.ToColor();
        material.Transparency = (int) Math.Round(template.Value.Transparency * 100.0);
        material.Shininess = (int) Math.Round(template.Value.Shine * 128.0);
      }

      return material;
    }

    ARDB.Material Reconstruct(ARDB.Material material, ARDB.Document doc, string name, string templateName, GH_Material template)
    {
      if (!Reuse(material, name, templateName, template))
      {
        material = material.ReplaceElement
        (
          CreateMaterial(doc, name, templateName, template),
          ExcludeUniqueProperties
        );
      }

      return material;
    }
    #endregion

    #region AppearanceAssetElement
    bool Reuse(ARDB.AppearanceAssetElement assetElement, string name, string templateName, GH_Material template)
    {
#if REVIT_2018
      if (assetElement is null) return false;
      if (name is object) { if (assetElement.Name != name) assetElement.Name = name; }
      else assetElement.SetIncrementalNomen(templateName ?? _Asset_);

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
#else
      return false;
#endif
    }

    ARDB.AppearanceAssetElement CreateAppearanceAsset(ARDB.Document doc, string name, string templateName, GH_Material template)
    {
      var assetElement = default(ARDB.AppearanceAssetElement);

      // Make sure the name is unique
      {
        name = doc.NextIncrementalNomen
        (
          name ?? templateName ?? _Asset_,
          typeof(ARDB.AppearanceAssetElement),
          categoryId: ARDB.BuiltInCategory.INVALID
        );
      }

      var assets = doc.Application.GetAssets(AssetType.Appearance);
      var asset = assets.FirstOrDefault(x => x.Name == "Generic");
      assetElement = ARDB.AppearanceAssetElement.Create(doc, name, asset);

      Reuse(assetElement, name, templateName, template);
      return assetElement;
    }

    ARDB.AppearanceAssetElement Reconstruct(ARDB.AppearanceAssetElement asset, ARDB.Document doc, string name, string templateName, GH_Material template)
    {
      if (!Reuse(asset, name, templateName, template))
      {
        asset = asset.ReplaceElement
        (
          CreateAppearanceAsset(doc, name, templateName, template),
          ExcludeUniqueProperties
        );
      }

      return asset;
    }
    #endregion
  }
}
