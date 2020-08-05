using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("0F7DA57E-6C05-4DD0-AABF-69E42DF38859");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(true);

    public QueryElements() : base
    (
      name: "Query Elements",
      nickname: "Elements",
      description: "Get document model elements list",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.DisableGapLogic();

      DB.ElementFilter filter = null;
      if (!DA.GetData("Filter", ref filter))
        return;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        DA.SetDataList
        (
          "Elements",
          collector.
          WherePasses(ElementFilter).
          WherePasses(filter).
          Select(x => Types.Element.FromElement(x))
        );
      }
    }
  }

  public class QueryGraphicalElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("79DAEA3A-13A3-49BF-8BEB-AA28E3BE4515");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementIsElementTypeFilter(true);

    public QueryGraphicalElements() : base
    (
      name: "Query Graphical Elements",
      nickname: "GraphElem",
      description: "Get document model elements list",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View", GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.Category>("Categories", "C", "Category", GH_ParamAccess.list, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc) { }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var view = default(Types.View);
      if (!DA.GetData("View", ref view) || view?.IsValid != true)
        return;

      var Categories = new List<Types.Category>();
      var _Categories_ = Params.IndexOfInputParam("Categories");
      var noFilterCategories = Params.Input[_Categories_].DataType == GH_ParamData.@void;
      if (!noFilterCategories)
        DA.GetDataList(_Categories_, Categories);

      var filter = default(DB.ElementFilter);
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(view.Document, view.Id))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        var ids = Categories.
          Where(x => x.IsValid && x.Document.Equals(view.Document)).
          Select(x => x.Id).ToArray();

        if (noFilterCategories)
        {
          elementCollector = elementCollector.WherePasses(new DB.ElementCategoryFilter(DB.BuiltInCategory.INVALID, true));
        }
        else if (ids.Length == 0)
        {
          var nothing = new DB.ElementFilter[] { new DB.ElementIsElementTypeFilter(true), new DB.ElementIsElementTypeFilter(false) };
          elementCollector = elementCollector.WherePasses(new DB.LogicalAndFilter(nothing));
        }
        else if (ids.Length == 1)
        {
          elementCollector = elementCollector.WherePasses(new DB.ElementCategoryFilter(ids[0]));
        }
        else
        {
          elementCollector = elementCollector.WherePasses(new DB.ElementMulticategoryFilter(ids));
        }

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Elements",
          elementCollector.
          Where(x => Types.GraphicalElement.IsValidElement(x)).
          Select(x => Types.Element.FromElement(x))
        );
      }
    }
  }
}
