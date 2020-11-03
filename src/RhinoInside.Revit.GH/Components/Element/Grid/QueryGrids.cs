using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryGrids : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("218FDACD-15CE-4B3A-8D70-F7F41362A4F4");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Grid));

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
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_Interval>("Elevation", "E", "Grid extents interval", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Grid name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Grid>("Grids", "G", "Grids list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (Params.TryGetData(DA, "Elevation", out Interval? elevation) && !elevation.Value.IsValid)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Elevation value is not valid.");
        return;
      }
      Params.TryGetData(DA, "Name", out string name);
      Params.TryGetData(DA, "Filter", out DB.ElementFilter filter);

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var gridsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          gridsCollector = gridsCollector.WherePasses(filter);

        if (TryGetFilterStringParam(DB.BuiltInParameter.DATUM_TEXT, ref name, out var nameFilter))
          gridsCollector = gridsCollector.WherePasses(nameFilter);

        var grids = gridsCollector.Cast<DB.Grid>();

        if (!string.IsNullOrEmpty(name))
          grids = grids.Where(x => x.Name.IsSymbolNameLike(name));

        if (elevation.HasValue)
        {
          grids = grids.Where
          (
            x =>
            {
              var extents = x.GetExtents();
              var interval = new Interval(extents.MinimumPoint.Z * Revit.ModelUnits, extents.MaximumPoint.Z * Revit.ModelUnits);
              return Interval.FromIntersection(elevation.Value, interval).IsValid;
            }
          );
        }

        DA.SetDataList("Grids", grids);
      }
    }
  }
}
