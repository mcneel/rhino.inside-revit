using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Material
{
#if REVIT_2018
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
          Optional = true
        }
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

    public override void AddedToDocument(GH_Document document)
    {
      if (ComponentVersion < CurrentVersion)
      {
        if (Params.Output<IGH_Param>("Appearance Asset (Generic)") is IGH_Param asset)
          asset.CopyFrom(Outputs[0].Param);
      }

      base.AddedToDocument(document);
    }

    const string _Asset_ = "Appearance Asset";
    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties =
    {
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => !string.IsNullOrEmpty(x))) return;
      Params.TryGetData(DA, "Template", out DB.AppearanceAssetElement template);

      // Previous Output
      Params.ReadTrackedElement(_Asset_, doc.Value, out DB.AppearanceAssetElement asset);

      StartTransaction(doc.Value);
      {
        asset = Reconstruct(asset, doc.Value, name, template);

        Params.WriteTrackedElement(_Asset_, doc.Value, asset);
        DA.SetData(_Asset_, asset);
      }
    }

    bool Reuse(DB.AppearanceAssetElement assetElement, string name, DB.AppearanceAssetElement template)
    {
      if (assetElement is null) return false;
      if (name is object) assetElement.Name = name;

      if (template is object)
      {
        assetElement.CopyParametersFrom(template, ExcludeUniqueProperties);
        assetElement.SetRenderingAsset(template.GetRenderingAsset());
      }

      return true;
    }

    DB.AppearanceAssetElement Create(DB.Document doc, string name, string schema, DB.AppearanceAssetElement template)
    {
      var assetElement = default(DB.AppearanceAssetElement);

      // Make sure the name is unique
      {
        if (name is null)
          name = template?.Name ?? schema;

        name = doc.GetNamesakeElements
        (
          typeof(DB.Material), name, categoryId: DB.BuiltInCategory.OST_AppearanceAsset
        ).
        Select(x => x.Name).
        WhereNamePrefixedWith(name).
        NextNameOrDefault() ?? name;
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
          var ids = DB.ElementTransformUtils.CopyElements
          (
            template.Document,
            new DB.ElementId[] { template.Id },
            doc,
            default,
            default
          );

          assetElement = ids.Select(x => doc.GetElement(x)).OfType<DB.AppearanceAssetElement>().FirstOrDefault();
          assetElement.Name = name;
        }
      }

      if (assetElement is null)
      {
        var assets = doc.Application.GetAssets(DB.Visual.AssetType.Appearance);
        var asset = assets.Where(x => x.Name == schema).FirstOrDefault();
        assetElement = DB.AppearanceAssetElement.Create(doc, name, asset);
      }

      return assetElement;
    }

    DB.AppearanceAssetElement Reconstruct(DB.AppearanceAssetElement assetElement, DB.Document doc, string name, DB.AppearanceAssetElement template)
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
