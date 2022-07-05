using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  public class QueryGroupTypes : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("97E9C6BB-8442-4F77-BCA1-6BE8AAFBDC96");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "G";

    protected override ARDB.ElementFilter ElementFilter => External.DB.CompoundElementFilter.Intersect
    (
      new ARDB.ElementCategoryFilter(ARDB.BuiltInCategory.OST_IOSModelGroups),
      new ARDB.ElementClassFilter(typeof(ARDB.GroupType))
    );

    public QueryGroupTypes() : base
    (
      name: "Query Group Types",
      nickname: "GroupTypes",
      description: "Get document group types list",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Group name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true)
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

      string name = null;
      DA.GetData("Name", ref name);

      ARDB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var typesCollector = collector.WherePasses(ElementFilter);

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
