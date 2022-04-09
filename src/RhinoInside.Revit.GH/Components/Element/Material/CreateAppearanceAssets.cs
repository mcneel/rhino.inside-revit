using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Materials
{
#if REVIT_2018
  using ElementTracking;
  using External.DB.Extensions;

  public class CreateGenericShader : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("0F251F87-317B-4669-BC70-22B29D3EBA6A");

    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public CreateGenericShader() : base
    (
      name: "Create Appearance Asset",
      nickname: "Appearance Asset",
      description: "Create a Revit appearance asset",
      category: "Revit",
      subCategory: "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Document",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Asset Name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.AppearanceAsset()
        {
          Name = "Template",
          NickName = "T",
          Description = "Template Asset",
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AppearanceAsset()
        {
          Name = _Asset_,
          NickName = _Asset_.Substring(0, 1),
          Description = $"Output Generic {_Asset_}",
        }
      ),
    };

    const string _Asset_ = "Appearance Asset";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties = { };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc) || !doc.IsValid) return;

      ReconstructElement<ARDB.AppearanceAssetElement>
      (
        doc.Value, _Asset_, (asset) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return null;
          Params.TryGetData(DA, "Template", out ARDB.AppearanceAssetElement template);

          StartTransaction(doc.Value);
          if (CanReconstruct(_Asset_, out var untracked, ref asset, doc.Value, name))
            asset = Reconstruct(asset, doc.Value, name, template);

          DA.SetData(_Asset_, asset);
          return untracked ? null : asset;
        }
      );
    }

    bool Reuse(ARDB.AppearanceAssetElement assetElement, string name, ARDB.AppearanceAssetElement template)
    {
      if (assetElement is null) return false;
      if (name is object) { if (assetElement.Name != name) assetElement.Name = name; }
      else assetElement.SetIncrementalNomen(template?.Name ?? _Asset_);

      if (template is object)
      {
        assetElement.CopyParametersFrom(template, ExcludeUniqueProperties);
        assetElement.SetRenderingAsset(template.GetRenderingAsset());
      }

      return true;
    }

    ARDB.AppearanceAssetElement Create(ARDB.Document doc, string name, string schema, ARDB.AppearanceAssetElement template)
    {
      var assetElement = default(ARDB.AppearanceAssetElement);

      // Make sure the name is unique
      if (name is null)
      {
        name = doc.NextIncrementalNomen
        (
          name ?? template?.Name ?? _Asset_,
          typeof(ARDB.AppearanceAssetElement),
          categoryId: ARDB.BuiltInCategory.INVALID
        );
      }

      // Try to duplicate template
      if (template is object)
      {
        if (doc.Equals(template.Document))
        {
          assetElement = template.Duplicate(name);
        }
        else
        {
          var ids = ARDB.ElementTransformUtils.CopyElements
          (
            template.Document,
            new ARDB.ElementId[] { template.Id },
            doc,
            default,
            default
          );

          assetElement = ids.Select(x => doc.GetElement(x)).OfType<ARDB.AppearanceAssetElement>().FirstOrDefault();
          assetElement.Name = name;
        }
      }

      if (assetElement is null)
      {
        var assets = doc.Application.GetAssets(ARDB.Visual.AssetType.Appearance);
        var asset = assets.Where(x => x.Name == schema).FirstOrDefault();
        assetElement = ARDB.AppearanceAssetElement.Create(doc, name, asset);
      }

      return assetElement;
    }

    ARDB.AppearanceAssetElement Reconstruct(ARDB.AppearanceAssetElement assetElement, ARDB.Document doc, string name, ARDB.AppearanceAssetElement template)
    {
      if (!Reuse(assetElement, name, template))
      {
        assetElement = assetElement.ReplaceElement
        (
          Create(doc, name, "Generic", template),
          ExcludeUniqueProperties
        );
      }

      return assetElement;
    }
  }
#endif
}
