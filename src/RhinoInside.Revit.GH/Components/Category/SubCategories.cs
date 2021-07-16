using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.ElementTracking;
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

  public class AddSubCategory : ElementTrackerComponent
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
      ParamDefinition.Create<Parameters.Category>("Parent", "P", "Parent category"),
      ParamDefinition.Create<Param_String>("Name", "N", "SubCategory name", optional: true),
      ParamDefinition.Create<Parameters.Category>("Template", "T", "Template category", optional: true, relevance: ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Category>("SubCategory", "S", "New SubCategory")
    };

    protected override bool CurrentDocumentOnly => false;
    const string _SubCategory_ = "SubCategory";
    static readonly DB.BuiltInParameter[] ExcludeUniqueProperties = { };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Parent", out Types.Category parent)) return;
      if (!Params.TryGetData(DA, "Name", out string name, x => x is object)) return;
      Params.TryGetData(DA, "Template", out Types.Category template);

      // Previous Output
      var category = Params.ReadTrackedElement(_SubCategory_, parent.Document, out DB.Element element) ?
        Types.Category.FromCategory(DocumentExtension.AsCategory(element)) :
        new Types.Category();

      StartTransaction(parent.Document);
      {
        category = Reconstruct(category, parent, name, template);

        Params.WriteTrackedElement(_SubCategory_, parent.Document, category.Value);
        DA.SetData(_SubCategory_, category);
      }
    }

    bool Reuse(Types.Category category, Types.Category parent, string name, Types.Category template)
    {
      if (!category.IsValid) return false;
      if (!category.APIObject.Parent.IsEquivalent(parent.APIObject)) return false;
      if (name is object) category.Name = name;
      if (template?.IsValid == true)
      {
        category.LineColor = template.LineColor;
        category.Material = template.Material;
        category.ProjectionLinePattern = template.ProjectionLinePattern;
        category.CutLinePattern = template.CutLinePattern;
        category.ProjectionLineWeight = template.ProjectionLineWeight;
        category.CutLineWeight = template.CutLineWeight;
      }

      return true;
    }

    Types.Category Create(Types.Category parent, string name, Types.Category template)
    {
      var category = default(Types.Category);
      var doc = parent.Document;

      // Make sure the name is unique
      {
        if (name is null)
          name = template?.Name ?? parent.Name;

        name = parent.APIObject.SubCategories.
          Cast<DB.Category>().
          Select(x => x.Name).
          WhereNamePrefixedWith(name).
          NextNameOrDefault() ?? name;
      }

      // Try to duplicate template
      if (template?.Id.IsBuiltInId() == false)
      {
        var ids = DB.ElementTransformUtils.CopyElements
        (
          template.Document,
          new DB.ElementId[] { template.Id },
          doc,
          default,
          default
        );

        category = ids.Select(x => Types.Category.FromElementId(doc, x)).OfType<Types.Category>().FirstOrDefault();
        category.Name = name;
      }

      if (category is null)
      {
        using (var categories = doc.Settings.Categories)
          category = Types.Category.FromCategory(categories.NewSubcategory(parent.APIObject, name));

        if (template?.Id.IsBuiltInId() == true)
        {
          category.LineColor = template.LineColor;
          category.Material = template.Material;
          category.ProjectionLinePattern = template.ProjectionLinePattern;
          category.CutLinePattern = template.CutLinePattern;
          category.ProjectionLineWeight = template.ProjectionLineWeight;
          category.CutLineWeight = template.CutLineWeight;
        }
      }

      return category;
    }

    Types.Category Reconstruct(Types.Category category, Types.Category parent, string name, Types.Category template)
    {
      if (!Reuse(category, parent, name, template))
      {
        var element = category.Value;
        element = element.ReplaceElement
        (
          Create(parent, name, template).Value,
          ExcludeUniqueProperties
        );
        category.SetValue(element.Document, element.Id);
      }

      return category;
    }
  }
}
