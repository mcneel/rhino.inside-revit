using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Site
{
  using External.DB;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.36")]
  public class QuerySiteLocations : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("9C352309-F20B-4C9B-AF46-3783D1106CDF");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "⌖";
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.SiteLocation));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.Location, "Open Location…");
    }
    #endregion

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
      new ParamDefinition(Parameters.ElementSource.QuerySource(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Site location name", optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Secondary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.SiteLocation>("Site Locations", "SL", "Site locations list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.ElementSource.GetElementSourceOrCurrent(this, DA, out var source)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;
      if (source.SourceDocument.Value.IsFamilyDocument) return;

      var locations = source.SourceDocument.Value.ProjectLocations.Cast<ARDB.ProjectLocation>().
        Select(x => x.Document.GetElement(x.GetTypeId()) as ARDB.SiteLocation).
        Distinct();

      if (filter is object)
        locations = locations.WherePasses(filter, source.SourceInstance.Value);

      if (name is object)
        locations = locations.Where(x => x.Name.IsSymbolNameLike(name));

      DA.SetDataList
      (
        "Site Locations",
        locations.
        OrderBy(x => x.Id.ToValue()).
        Cast<Types.SiteLocation>().
        FromSource(source).
        TakeWhileIsNotEscapeKeyDown(this)
      );
    }
  }
}
