using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ObjectStyles
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.16")]
  public class QueryCategories : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("D150E40E-0970-4683-B517-038F8BA8B0D8");
    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override bool MayNeedToBeExpired
    (
      ARDB.Document document,
      ICollection<ARDB.ElementId> added,
      ICollection<ARDB.ElementId> deleted,
      ICollection<ARDB.ElementId> modified
    )
    {
      if (added.Any(x => x.IsCategoryId(document)))
        return true;

      if (modified.Any(x => x.IsCategoryId(document)))
        return true;

      if (deleted.Any())
      {
        foreach (var param in Params.Output.OfType<Kernel.IGH_ReferenceParam>())
        {
          if (param.NeedsToBeExpired(document, ElementIdExtension.EmptyCollection, deleted, ElementIdExtension.EmptyCollection))
            return true;
        }
      }

      return false;
    }

    public QueryCategories() : base
    (
      name: "Query Categories",
      nickname: "Categories",
      description: "Get document categories list",
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.CategoryType>>("Type", "T", "Category type", ARDB.CategoryType.Model, optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Category>("Parent", "P", "Parent category", optional: true, relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Category name", optional: true),
      ParamDefinition.Create<Param_Boolean>("Is Visible UI", "VUI", "Category is exposed in UI", defaultValue: true, optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Is Subcategory", "ISC", "Is subcategory", defaultValue: false, optional: true),
      ParamDefinition.Create<Param_Boolean>("Allows Subcategories", "ASC", "Category allows subcategories to be added", optional: true, relevance: ParamRelevance.Tertiary),
      ParamDefinition.Create<Param_Boolean>("Allows Parameters", "AP", "Category allows bound parameters", defaultValue: true, optional: true, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Boolean>("Has Material Quantities", "HMQ", "Category has material quantities", defaultValue: true, optional: true, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Boolean>("Cuttable", "C", "Category is cuttable", defaultValue: true, optional: true, relevance: ParamRelevance.Tertiary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Category>("Categories", "C", "Categories list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Type", out ARDB.CategoryType? type)) return;
      if (!Params.TryGetData(DA, "Parent", out Types.Category parent)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Is Subcategory", out bool? isSubcategory)) return;
      if (!Params.TryGetData(DA, "Is Visible UI", out bool? visibleInUI)) return;
      if (!Params.TryGetData(DA, "Allows Subcategories", out bool? allowsSubcategories)) return;
      if (!Params.TryGetData(DA, "Allows Parameters", out bool? allowsParameters)) return;
      if (!Params.TryGetData(DA, "Has Material Quantities", out bool? hasMaterialQuantities)) return;
      if (!Params.TryGetData(DA, "Cuttable", out bool? cuttable)) return;

      if(!(parent?.Document is null || doc.Equals(parent.Document)))
        throw new System.ArgumentException("Wrong Document.", nameof(parent));

      IEnumerable<ARDB.Category> categories = doc.GetCategories(parent?.Id);

      if (type.HasValue)
        categories = categories.Where(x => x.CategoryType == type);

      if (isSubcategory.HasValue)
        categories = categories.Where(x => x.Parent is object == isSubcategory.Value);

      if (allowsSubcategories.HasValue)
        categories = categories.Where(x => x.CanAddSubcategory == allowsSubcategories);

      if (allowsParameters.HasValue)
        categories = categories.Where(x => x.AllowsBoundParameters == allowsParameters);

      if (hasMaterialQuantities.HasValue)
        categories = categories.Where(x => x.HasMaterialQuantities == hasMaterialQuantities);

      if (cuttable.HasValue)
        categories = categories.Where(x => x.IsCuttable == cuttable);

      if (name is object)
      {
        if (parent?.Id is null)
          categories = categories.Where(x => x.FullName().IsSymbolNameLike(name));
        else
          categories = categories.Where(x => x.Name.IsSymbolNameLike(name));
      }

      if (visibleInUI.HasValue)
        categories = categories.Where(x => x.IsVisibleInUI() == visibleInUI);

      DA.SetDataList
      (
        "Categories",
        categories.
        Select(x => new Types.Category(x)).
        TakeWhileIsNotEscapeKeyDown(this).
        OrderBy(x => x.Id.ToValue())
      );
    }
  }
}
