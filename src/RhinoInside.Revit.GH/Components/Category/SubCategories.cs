using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Categories
{
  using ElementTracking;
  using External.DB.Extensions;

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
      var category = default(ARDB.Category);
      if (!DA.GetData("Category", ref category))
        return;

      if (category.Document() is ARDB.Document doc)
      {
        using (var subCategories = category.SubCategories)
        {
          var list = subCategories.Cast<ARDB.Category>();
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
      ParamDefinition.Create<Param_String>("Name", "N", "SubCategory name", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Category>("Template", "T", "Template category", optional: true, relevance: ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Category>("SubCategory", "S", "New SubCategory")
    };

    protected override bool CurrentDocumentOnly => false;
    const string _SubCategory_ = "SubCategory";
    static readonly ARDB.BuiltInParameter[] ExcludeUniqueProperties = { };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Parent", out Types.Category parent, x => x.IsValid)) return;

      ReconstructElement<ARDB.Element>
      (
        parent.Document, _SubCategory_, (element) =>
        {
          // Input
          if (!Params.TryGetData(DA, "Name", out string name, x => x is object)) return null;
          if (!Params.TryGetData(DA, "Template", out Types.Category template, x => x.IsValid)) return null;

          // Compute
          StartTransaction(parent.Document);
          if
          (
            CanReconstruct
            (
              _SubCategory_, out var untracked, ref element,
              parent.Document, name,
              (d, n) => parent.APIObject.SubCategories.Contains(name) ?
                d.GetElement(parent.APIObject.SubCategories.get_Item(name).Id) : null
            )
          )
          {
            var category = Types.Category.FromCategory(DocumentExtension.AsCategory(element));
            category = Reconstruct(category, parent, name, template);
            element = category.Value;

            DA.SetData(_SubCategory_, category);
          }

          return untracked ? null : element;
        }
      );
    }

    bool Reuse(Types.Category category, Types.Category parent, string name, Types.Category template)
    {
      if (category is null) return false;
      if (!category.APIObject.Parent.IsEquivalent(parent.APIObject)) return false;
      if (name is object) category.Nomen = name;
      else category.SetIncrementalNomen(template?.Nomen ?? parent.Nomen);

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
      if (name is null)
      {
        name = template?.Nomen ?? parent.Nomen;
        DocumentExtension.TryParseNomenId(name, out var prefix, out var _);
        name = parent.APIObject.SubCategories.
          Cast<ARDB.Category>().
          Select(x => x.Name).
          WhereNomenPrefixedWith(prefix).
          NextNomenOrDefault() ?? $"{prefix} 0";
      }

      // Try to duplicate template
      if (template is object && (!template.Id.IsBuiltInId() && template.APIObject.Parent.Id == parent.Id))
      {
        var ids = ARDB.ElementTransformUtils.CopyElements
        (
          template.Document,
          new ARDB.ElementId[] { template.Id },
          doc, default, default
        );

        category = ids.Select(x => Types.Category.FromElementId(doc, x)).OfType<Types.Category>().FirstOrDefault();
        category.Nomen = name;
      }

      if (category is null)
      {
        using (var categories = doc.Settings.Categories)
          category = Types.Category.FromCategory(categories.NewSubcategory(parent.APIObject, name));

        Reuse(category, parent, category.Nomen, template);
      }

      return category;
    }

    Types.Category Reconstruct(Types.Category category, Types.Category parent, string name, Types.Category template)
    {
      if (!Reuse(category, parent, name, template))
      {
        var element = category?.Value;
        element = element.ReplaceElement
        (
          Create(parent, name, template).Value,
          ExcludeUniqueProperties
        );
        category = Types.Category.FromElementId(element.Document, element.Id);
      }

      return category;
    }
  }
}
