using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.LinePatternElements
{
  [ComponentVersion(introduced: "1.11")]
  public class QueryFillPatterns : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("71C06438-EC02-4A64-A818-49F4F6C5AD55");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.FillPatternElement));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.FillPatterns);
      Menu_AppendItem
      (
        menu, $"Open Fill Patterns…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryFillPatterns() : base
    (
      name: "Query Fill Patterns",
      nickname: "Fill Patterns",
      description: "Get document fill patterns list",
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition (new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Fill pattern name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.FillPatternTarget>>("Type", "T", "Fill pattern type", defaultValue: ARDB.FillPatternTarget.Drafting, GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.FillPatternElement>("Fill Patterns", "FP", "Fill pattern list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      string name = null;
      DA.GetData("Name", ref name);

      Params.TryGetData(DA, "Type", out ARDB.FillPatternTarget? type);
      Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var patternsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          patternsCollector = patternsCollector.WherePasses(filter);

        var patterns = collector.Cast<ARDB.FillPatternElement>().Select(x => new Types.FillPatternElement(x));

        if (!string.IsNullOrEmpty(name))
          patterns = patterns.Where(x => x.Nomen.IsSymbolNameLike(name));

        if (type.HasValue)
          patterns = patterns.Where(x => { using (var pattern = x.Value.GetFillPattern()) return pattern.Target == type.Value; });

        DA.SetDataList
        (
          "Fill Patterns",
          patterns.
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }

}
