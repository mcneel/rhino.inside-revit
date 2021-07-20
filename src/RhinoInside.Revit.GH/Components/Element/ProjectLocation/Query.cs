using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  public class QueryProjectLocations : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("62641279-D4CE-4F93-8430-BD342BE123AB");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "⌖";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.ProjectLocation));

    public QueryProjectLocations()
    : base
    (
      name: "Query Shared Sites",
      nickname: "Shared Sites",
      description: "Get all document shared sites.",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Shared site name", optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Shared Sites", "SS", "Shared sites list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Filter", out DB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var sitesCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          sitesCollector = sitesCollector.WherePasses(filter);

        var sites = collector.Cast<DB.ProjectLocation>();

        if (name is object)
          sites = sites.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Shared Sites", sites.Select(x => new Types.ProjectLocation(x)));
      }
    }
  }
}
