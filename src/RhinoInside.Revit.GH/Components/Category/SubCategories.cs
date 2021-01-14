using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class CategorySubCategories : Component
  {
    public override Guid ComponentGuid => new Guid("4915AB87-0BD5-4541-AC43-3FBC450DD883");

    public CategorySubCategories() : base
    (
      name: "Category SubCategories",
      nickname: "SubCats",
      description: "Returns a list containing the subcategories of Category",
      category: "Revit",
      subCategory: "Category"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "Category", "C", "Category to query", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Category(), "SubCategories", "S", string.Empty, GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var category = default(DB.Category);
      if (!DA.GetData("Category", ref category))
        return;

      if (category.Document() is DB.Document doc)
      {
        using (var subCategories = category.SubCategories)
        {
          var list = subCategories.Cast<DB.Category>();
          DA.SetDataList("SubCategories", list.Select(x => new Types.Category(doc, x.Id)));
        }
      }
    }
  }

  public class AddSubCategory : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("8DE336FB-E764-4A8E-BB12-9AECDA19769F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AddSubCategory() : base
    (
      name: "Add SubCategory",
      nickname: "SubCat",
      description: "Add a new subcategory to the given category",
      category: "Revit",
      subCategory: "Category"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Category>("Parent", "P", "Parent category", GH_ParamAccess.item),
      ParamDefinition.Create<Param_String>("Name", "N", "SubCategory name", GH_ParamAccess.item),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Category>("SubCategory", "S", "New SubCategory", GH_ParamAccess.item)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Category parent = null;
      if (!DA.GetData("Parent", ref parent))
        return;

      string newSubcatName = default;
      if (!DA.GetData("Name", ref newSubcatName))
        return;

      // find existing subcat if exists
      var newSubcat = parent.SubCategories.Cast<DB.Category>().
        Where(x => x.Name == newSubcatName).
        FirstOrDefault();

      // if not found, create one
      if (newSubcat is null && newSubcatName != string.Empty)
      {
        var doc = parent.Document();
        StartTransaction(doc);

        using (var categories = doc.Settings.Categories)
          newSubcat = categories.NewSubcategory(parent, newSubcatName);
      }

      // return data to DA
      DA.SetData("SubCategory", newSubcat);
    }
  }
}
