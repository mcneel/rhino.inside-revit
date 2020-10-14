using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Extensions;
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

      DA.TryGetData(Params.Input, "Type", out DB.CategoryType? categoryType);
      DA.TryGetData(Params.Input, "Parent", out Types.Category Parent);
      DA.TryGetData(Params.Input, "Name", out string Name);
      DA.TryGetData(Params.Input, "Allows Subcategories", out bool? AllowsSubcategories);
      DA.TryGetData(Params.Input, "Allows Parameters", out bool? AllowsParameters);
      DA.TryGetData(Params.Input, "Has Material Quantities", out bool? HasMaterialQuantities);
      DA.TryGetData(Params.Input, "Cuttable", out bool? Cuttable);

      if(!(Parent?.Document is null || doc.Equals(Parent.Document)))
        throw new System.ArgumentException("Wrong Document.", nameof(Parent));

      IEnumerable<DB.Category> categories = doc.GetCategories(Parent?.Id);

      if (categoryType.HasValue)
        categories = categories.Where(x => x.CategoryType == categoryType);

      if (AllowsSubcategories.HasValue)
        categories = categories.Where(x => x.CanAddSubcategory == AllowsSubcategories);

      if (AllowsParameters.HasValue)
        categories = categories.Where(x => x.AllowsBoundParameters == AllowsParameters);

      if (HasMaterialQuantities.HasValue)
        categories = categories.Where(x => x.HasMaterialQuantities == HasMaterialQuantities);

      if (Cuttable.HasValue)
        categories = categories.Where(x => x.IsCuttable == Cuttable);

      if (Name is object)
      {
        if (Parent?.Id is null)
          categories = categories.Where(x => x.FullName().IsSymbolNameLike(Name));
        else
          categories = categories.Where(x => x.Name.IsSymbolNameLike(Name));
      }

      DA.SetDataList("Categories", categories.OrderBy(x => x.FullName()));
    }
  }
}
