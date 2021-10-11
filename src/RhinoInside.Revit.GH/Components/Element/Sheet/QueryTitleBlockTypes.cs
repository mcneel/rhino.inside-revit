using System;
using System.Linq;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

using RhinoInside.Revit.GH.Kernel.Attributes;
using RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  [Since("v1.2")]
  public class QueryTitleBlockTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("c7a57ec8-d4d3-4251-aa91-cc67f833313b");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => CompoundElementFilter.Intersect
    (
      CompoundElementFilter.ElementIsElementTypeFilter(),
      new DB.ElementCategoryFilter(DB.BuiltInCategory.OST_TitleBlocks)
    );

    public QueryTitleBlockTypes() : base
    (
      name: "Query Title Block Types",
      nickname: "Title Block Types",
      description: "Get all document title block types",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Title Block Types", "TB", "Title Block types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      DB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var typesCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          typesCollector = typesCollector.WherePasses(filter);

        var titleBlockTypes = typesCollector.Cast<DB.ElementType>();

        DA.SetDataList
        (
          "Title Block Types",
          titleBlockTypes.
          Select(Types.ElementType.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
