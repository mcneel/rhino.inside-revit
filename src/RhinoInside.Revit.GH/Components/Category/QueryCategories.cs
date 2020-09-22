using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryCategories : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("D150E40E-0970-4683-B517-038F8BA8B0D8");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => null;

    public override bool NeedsToBeExpired(DB.Events.DocumentChangedEventArgs e)
    {
      var document = e.GetDocument();

      var added = e.GetAddedElementIds().Where(x => x.IsCategoryId(document));
      if (added.Any())
        return true;

      var modified = e.GetModifiedElementIds().Where(x => x.IsCategoryId(document));
      if (modified.Any())
        return true;

      var deleted = e.GetDeletedElementIds();
      if (deleted.Any())
      {
        var empty = new DB.ElementId[0];
        var deletedSet = new HashSet<DB.ElementId>(deleted);
        foreach (var param in Params.Output.OfType<Kernel.IGH_ElementIdParam>())
        {
          if (param.NeedsToBeExpired(document, empty, deletedSet, empty))
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
      subCategory: "Category"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Parameters.Param_Enum<Types.CategoryType>>("Type", "T", "Category type", DB.CategoryType.Model, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.Category>("Parent", "P", "Parent category", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Category name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Allows Subcategories", "ASC", "Category allows subcategories to be added", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Allows Parameters", "AP", "Category allows bound parameters", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Has Material Quantities", "HMQ", "Category has material quantities", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Cuttable", "C", "Category is cuttable", GH_ParamAccess.item, optional: true),
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

      var categoryType = DB.CategoryType.Invalid;
      DA.GetData("Type", ref categoryType);

      var Parent = default(DB.Category);
      var _Parent_ = Params.IndexOfInputParam("Parent");
      bool nofilterParent = (!DA.GetData(_Parent_, ref Parent) && Params.Input[_Parent_].DataType == GH_ParamData.@void);
      var ParentCategoryId = Parent?.Id ?? DB.ElementId.InvalidElementId;

      var Name = default(string);
      var _Name_ = Params.IndexOfInputParam("Name");
      bool nofilterName = (!DA.GetData(_Name_, ref Name) && Params.Input[_Name_].DataType == GH_ParamData.@void);

      bool AllowsSubcategories = false;
      var _AllowsSubcategories_ = Params.IndexOfInputParam("Allows Subcategories");
      bool nofilterSubcategories = (!DA.GetData(_AllowsSubcategories_, ref AllowsSubcategories) && Params.Input[_AllowsSubcategories_].DataType == GH_ParamData.@void);

      bool AllowsParameters = false;
      var _AllowsParameters_ = Params.IndexOfInputParam("Allows Parameters");
      bool nofilterParams = (!DA.GetData(_AllowsParameters_, ref AllowsParameters) && Params.Input[_AllowsParameters_].DataType == GH_ParamData.@void);

      bool HasMaterialQuantities = false;
      var _HasMaterialQuantities_ = Params.IndexOfInputParam("Has Material Quantities");
      bool nofilterMaterials = (!DA.GetData(_HasMaterialQuantities_, ref HasMaterialQuantities) && Params.Input[_HasMaterialQuantities_].DataType == GH_ParamData.@void);

      bool Cuttable = false;
      var _Cuttable_ = Params.IndexOfInputParam("Cuttable");
      bool nofilterCuttable = (!DA.GetData(_Cuttable_, ref Cuttable) && Params.Input[_Cuttable_].DataType == GH_ParamData.@void);

      var rootCategories = BuiltInCategoryExtension.BuiltInCategories.Select(x => doc.GetCategory(x)).Where(x => x is object && x.Parent is null);
      var subCategories = rootCategories.SelectMany(x => x.SubCategories.Cast<DB.Category>());

      var nameIsSubcategoryFullName = Name is string && Name.Substring(1).Contains(':');
      var excludeSubcategories = AllowsSubcategories || !nameIsSubcategoryFullName;

      // Default path iterate over categories looking for a match
      var categories = nofilterParent && !excludeSubcategories ?
                       rootCategories.Concat(subCategories) :     // All categories
                       (
                         Parent is null ?
                         rootCategories :                         // Only root categories
                         Parent.SubCategories.Cast<DB.Category>() // Only subcategories of Parent
                       );

      // Fast path for exact match queries
      if (Operator.CompareMethodFromPattern(Name) == Operator.CompareMethod.Equals)
      {
        var components = Name.Split(':');
        if (components.Length == 1)
        {
          if (doc.Settings.Categories.get_Item(components[0]) is DB.Category category)
          {
            nofilterName = true;
            categories = Enumerable.Repeat(category, 1);
          }
        }
        else if (components.Length == 2)
        {
          if (doc.Settings.Categories.get_Item(components[0]) is DB.Category category)
          {
            nofilterName = true;
            if (category.SubCategories.get_Item(components[1]) is DB.Category subCategory)
              categories = Enumerable.Repeat(subCategory, 1);
            else
              categories = Enumerable.Empty<DB.Category>();
          }
        }
        else return;
      }

      if (categoryType != DB.CategoryType.Invalid)
        categories = categories.Where((x) => x.CategoryType == categoryType);

      if (!nofilterSubcategories)
        categories = categories.Where((x) => x.CanAddSubcategory == AllowsSubcategories);

      if (!nofilterParams)
        categories = categories.Where((x) => x.AllowsBoundParameters == AllowsParameters);

      if (!nofilterMaterials)
        categories = categories.Where((x) => x.HasMaterialQuantities == HasMaterialQuantities);

      if (!nofilterCuttable)
        categories = categories.Where((x) => x.IsCuttable == Cuttable);

      if (!nofilterName)
      {
        if (nofilterParent)
          categories = categories.Where((x) => x.FullName().IsSymbolNameLike(Name));
        else
          categories = categories.Where((x) => x.Name.IsSymbolNameLike(Name));
      }

      IEnumerable<DB.Category> list = null;
      foreach (var group in categories.GroupBy((x) => x.CategoryType).OrderBy((x) => x.Key))
      {
        var orderedGroup = group.OrderBy((x) => x.Name);
        list = list?.Concat(orderedGroup) ?? orderedGroup;
      }

      DA.SetDataList("Categories", list);
    }
  }
}
