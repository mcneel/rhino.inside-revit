using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.11")]
  public class QueryGroupTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("97E9C6BB-8442-4F77-BCA1-6BE8AAFBDC96");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "G";

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.GroupType));

    public QueryGroupTypes() : base
    (
      name: "Query Group Types",
      nickname: "GroupTypes",
      description: "Get document group types list",
      category: "Revit",
      subCategory: "Type"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Category>("Category", "C", "Category to look for a group type", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Name", "N", "Group name", optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Types", "T", "Types list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Category", out Types.Category category)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var typesCollector = collector.WherePasses(ElementFilter);

        if (category is object)
          typesCollector.WhereCategoryIdEqualsTo(category.Id);

        if (filter is object)
          typesCollector = typesCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.ALL_MODEL_TYPE_NAME, ref name, out var nameFilter))
          typesCollector = typesCollector.WherePasses(nameFilter);

        var groupTypes = typesCollector.Cast<ARDB.GroupType>();

        if (!string.IsNullOrEmpty(name))
          groupTypes = groupTypes.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList
        (
          "Types",
          groupTypes.
          Select(Types.Element.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
