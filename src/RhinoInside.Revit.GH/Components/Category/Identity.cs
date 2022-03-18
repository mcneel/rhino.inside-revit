using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Categories
{
  [ComponentVersion(introduced: "1.0", updated: "1.6")]
  public class CategoryIdentity : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("D794361E-DE8C-4D0A-BC77-52293F27D3AA");

    public CategoryIdentity() : base
    (
      name: "Category Identity",
      nickname: "Identity",
      description: "Query category identity information",
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Category>("Category", "C", "Category to query")
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Param_Enum<Types.CategoryType>>("Type", "T", "Category type", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Category>("Parent", "P", "Parent category", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Category name"),
      ParamDefinition.Create<Param_Boolean>("Is Subcategory", "ISC", "Is subcategory"),
      ParamDefinition.Create<Param_Boolean>("Allows Subcategories", "ASC", "Category allows subcategories to be added", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Allows Parameters", "AP", "Category allows bound parameters", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Has Material Quantities", "HMQ", "Category has material quantities", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Cuttable", "C", "Category is cuttable", relevance: ParamRelevance.Occasional),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Category", out Types.Category category) || category.APIObject is null) return;

      Params.TrySetData(DA, "Type", () => category.CategoryType);
      Params.TrySetData(DA, "Parent", () => category.APIObject.Parent);
      Params.TrySetData(DA, "Name", () => category.FullName);
      Params.TrySetData(DA, "Is Subcategory", () => category.APIObject.Parent is object);
      Params.TrySetData(DA, "Allows Subcategories", () => category.APIObject.CanAddSubcategory);
      Params.TrySetData(DA, "Allows Parameters", () => category.APIObject.AllowsBoundParameters);
      Params.TrySetData(DA, "Has Material Quantities", () => category.APIObject.HasMaterialQuantities);
      Params.TrySetData(DA, "Cuttable", () => category.APIObject.IsCuttable);
    }
  }
}
