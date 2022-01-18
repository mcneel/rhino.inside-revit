using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  [ComponentVersion(introduced: "1.0", updated: "1.4")]
  public class DocumentLinks : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("EBCCFDD8-9F3B-44F4-A209-72D06C8082A5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "L";
    protected override ARDB.ElementFilter ElementFilter => External.DB.CompoundElementFilter.Union
    (
      new ARDB.ElementClassFilter(typeof(ARDB.RevitLinkInstance)),
      new ARDB.ElementClassFilter(typeof(ARDB.RevitLinkType))
    );

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.ManageLinks);
      Menu_AppendItem
      (
        menu, $"Manage Links…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public DocumentLinks() : base
    (
      name: "Query Document Links",
      nickname: "Links",
      description: "Gets Revit linked models into given document",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Revit linked model name", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Links", "L", "Revit linked models that are linked into given document", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Document>("Documents", "D", "Revit documents that are linked into given document", GH_ParamAccess.list, relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      Params.TryGetData(DA, "Name", out string name);
      Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var linksCollector = collector.OfClass(typeof(ARDB.RevitLinkInstance));

        if (filter is object)
          linksCollector = linksCollector.WherePasses(filter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.RVT_LINK_INSTANCE_NAME, ref name, out var nameFilter))
          linksCollector = linksCollector.WherePasses(nameFilter);

        var links = collector.Cast<ARDB.RevitLinkInstance>();

        if (!string.IsNullOrEmpty(name))
          links = links.Where(x => x.Name.IsSymbolNameLike(name));

        Params.TrySetDataList(DA, "Links", () => links);
        Params.TrySetDataList(DA, "Documents", () => links.Select(x => x.GetLinkDocument()));
      }
    }
  }
}
