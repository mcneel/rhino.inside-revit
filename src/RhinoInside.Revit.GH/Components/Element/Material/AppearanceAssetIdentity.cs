using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
#if REVIT_2018
using Autodesk.Revit.DB.Visual;
#else
using Autodesk.Revit.Utility;
#endif

namespace RhinoInside.Revit.GH.Components.Materials
{
  [ComponentVersion(introduced: "1.22")]
  public class AppearanceAssetIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("87171C24-F6AF-4561-A66F-21ADDA0F2C3E");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    public AppearanceAssetIdentity()
    : base
    (
      "Appearance Asset Identity",
      "Identity",
      "Appearance Asset Identity Data.",
      "Revit",
      "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.AppearanceAsset>("Appearance Asset", "AA"),

      ParamDefinition.Create<Param_String>("Category", "CT", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Description", "D", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Keyword", "C", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.AppearanceAsset>("Appearance Asset", "AA", relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Param_String>("Name", "N", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Category", "CT", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Description", "D", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Keyword", "K", relevance: ParamRelevance.Primary),

      ParamDefinition.Create<Param_String>("UIName", "UI", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Schema", "S", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_String>("Version", "V", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Boolean>("Hidden", "H", relevance: ParamRelevance.Secondary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Appearance Asset", out Types.AppearanceAssetElement assetElement, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Category", out string category);
      update |= Params.GetData(DA, "Description", out string description);
      update |= Params.GetData(DA, "Keyword", out string keyword);

      if (update)
      {
        StartTransaction(assetElement.Document);
        using (var editScope = new AppearanceAssetEditScope(assetElement.Document))
        {
          using (var asset = editScope.Start(assetElement.Id))
          {
            if (asset.IsValidObject && !asset.IsReadOnly)
            {
              if (description is object && asset.FindByName(SchemaCommon.Description) is AssetPropertyString _description && _description.Value != description) _description.Value = description;
              if (keyword is object && asset.FindByName(SchemaCommon.Keyword) is AssetPropertyString _keyword && _keyword.Value != keyword) _keyword.Value = keyword;
              if (category is object && asset.FindByName(SchemaCommon.Category) is AssetPropertyString _category && _category.Value != category) _category.Value = category;
            }

            editScope.Commit(false);
          }
        }
      }
      Params.TrySetData(DA, "Appearance Asset", () => assetElement);
      Params.TrySetData(DA, "Name", () => assetElement.Nomen);
      {
        using (var asset = assetElement.Value.GetRenderingAsset())
        {
          if (asset.IsValidObject)
          {
            Params.TrySetData(DA, "Category", () => (asset.FindByName(SchemaCommon.Category) as AssetPropertyString)?.Value);
            Params.TrySetData(DA, "Description", () => (asset.FindByName(SchemaCommon.Description) as AssetPropertyString)?.Value);
            Params.TrySetData(DA, "Keyword", () => (asset.FindByName(SchemaCommon.Keyword) as AssetPropertyString)?.Value);

            Params.TrySetData(DA, "UIName", () => (asset.FindByName(SchemaCommon.UIName) as AssetPropertyString)?.Value);
            Params.TrySetData(DA, "Schema", () => (asset.FindByName(SchemaCommon.BaseSchema) as AssetPropertyString)?.Value);
            Params.TrySetData(DA, "Version", () => (asset.FindByName(SchemaCommon.VersionGUID) as AssetPropertyString)?.Value);
            Params.TrySetData(DA, "Hidden", () => (asset.FindByName(SchemaCommon.Hidden) as AssetPropertyBoolean)?.Value);
          }
        }
      }
    }
  }
}
