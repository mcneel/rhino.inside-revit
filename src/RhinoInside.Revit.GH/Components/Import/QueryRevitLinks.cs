using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Insert
{
  using External.DB;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.36")]
  public class QueryRevitModels : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("EBCCFDD8-9F3B-44F4-A209-72D06C8082A5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "M";
    protected override ARDB.ElementFilter ElementFilter => External.DB.ElementFilters.Union
    (
      new ARDB.ElementClassFilter(typeof(ARDB.RevitLinkInstance)),
      new ARDB.ElementClassFilter(typeof(ARDB.RevitLinkType))
    );

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.ManageLinks, "Manage Links…");
    }
    #endregion

    public QueryRevitModels() : base
    (
      name: "Query Revit Models",
      nickname: "R-Models",
      description: "Gets Revit linked models into given document",
      category: "Revit",
      subCategory: "Insert"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.ElementSource(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Revit linked model instance name", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Document",
          NickName = "DOC",
          Description = "Revit model document",
          DataMapping = GH_DataMapping.Graft
        }
      ),
      new ParamDefinition
      (
        new Parameters.ElementSource()
        {
          Name = "Links",
          NickName = "L",
          Description = "Revit linked models to the given model",
          DataMapping = GH_DataMapping.Graft,
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.Document()
        {
          Name = "Documents",
          NickName = "LD",
          Description = "Revit linked documents to the given model",
          DataMapping = GH_DataMapping.Graft,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Occasional
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.ElementSource.GetElementSourceOrCurrent(this, DA, out var source)) return;
      Params.TrySetData(DA, "Document", () => source.SourceDocument);

      Params.TryGetData(DA, "Name", out string name);
      Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter);

      using (var collector = new ARDB.FilteredElementCollector(source.SourceDocument.Value))
      {
        var linksCollector = collector.OfClass(typeof(ARDB.RevitLinkInstance));

        if (filter is object)
          linksCollector = linksCollector.WherePasses(filter, source.SourceInstance.Value);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.RVT_LINK_INSTANCE_NAME, ref name, out var nameFilter))
          linksCollector = linksCollector.WherePasses(nameFilter);

        var links = collector.Cast<ARDB.RevitLinkInstance>();

        if (!string.IsNullOrEmpty(name))
          links = links.Where(x => x.GetElementNomen(ARDB.BuiltInParameter.RVT_LINK_INSTANCE_NAME).IsSymbolNameLike(name));

        Params.TrySetDataList(DA, "Links", () => links);
        Params.TrySetDataList(DA, "Documents", () => links.Select(x => x.GetLinkDocument()));
      }
    }
  }
}
