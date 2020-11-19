using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  public class QuerySiteLocations : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("9C352309-F20B-4C9B-AF46-3783D1106CDF");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "⌖";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.SiteLocation));

    public QuerySiteLocations()
    : base
    (
      name: "Query Site Locations",
      nickname: "Site Locations",
      description: "Get all document site locations.",
      category: "Revit",
      subCategory: "Site"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_String>("Name", "N", "Site location name", optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamVisibility.Default),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ElementType>("Site Locations", "SL", "Site locations list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Filter", out DB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var locationsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          locationsCollector = locationsCollector.WherePasses(filter);

        var locations = collector.Cast<DB.SiteLocation>();

        if (name is object)
          locations = locations.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Site Locations", locations.Select(x => new Types.SiteLocation(x)));
      }
    }
  }
}
