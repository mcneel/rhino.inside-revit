using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.16")]
  public class QueryViewports : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("63C816D8-4A84-45B6-BCEC-C60E57FBC547");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override ARDB.ElementFilter ElementFilter => CompoundElementFilter.ElementIsElementTypeFilter(inverted: true).
      Intersect(new ARDB.ElementClassFilter(typeof(ARDB.Viewport)));

    public QueryViewports() : base
    (
      name: "Query Viewports",
      nickname: "Vports",
      description: "Get all viewports placed in a sheet",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.ViewSheet>("Sheet", "S", "Sheet"),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Viewports", "V", "Viewports list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sheet", out Types.ViewSheet sheet, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      var viewports = sheet.Viewports as IEnumerable<Types.Viewport>;

      if (filter is object)
        viewports = viewports.Where(x => filter.PassesFilter(x.Value));

      DA.SetDataList
      (
        "Viewports",
        viewports.
        TakeWhileIsNotEscapeKeyDown(this)
      );
    }
  }
}
