using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Grids
{
  using Convert.Geometry;
  using External.DB.Extensions;

  public class QueryGrids : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("218FDACD-15CE-4B3A-8D70-F7F41362A4F4");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.Grid));

    public QueryGrids() : base
    (
      name: "Query Grids",
      nickname: "Grids",
      description: "Get all document grids",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Grid name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElevationInterval>("Elevation", "E", "Grid extents interval along z-axis", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Grid>("Grids", "G", "Grids list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Parameters.ElevationInterval.TryGetData(this, DA, "Elevation", out var elevation, doc)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc.Value))
      {
        var gridsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          gridsCollector = gridsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.DATUM_TEXT, ref name, out var nameFilter))
          gridsCollector = gridsCollector.WherePasses(nameFilter);

        var grids = gridsCollector.Cast<ARDB.Grid>();

        if (!string.IsNullOrEmpty(name))
          grids = grids.Where(x => x.Name.IsSymbolNameLike(name));

        if (elevation.HasValue)
        {
          var height = elevation.Value.InHostUnits();
          grids = grids.Where
          (
            x =>
            {
              var extents = x.GetExtents();
              var interval = new Interval(extents.MinimumPoint.Z, extents.MaximumPoint.Z);
              return Interval.FromIntersection(height, interval).IsValid;
            }
          );
        }

        DA.SetDataList
        (
          "Grids",
          grids.
          Select(x => new Types.Grid(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
