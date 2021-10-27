using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Phasing
{
  [ComponentVersion(introduced: "1.2")]
  public class QueryPhases : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("91E4D3E1-883A-44D9-A3D2-B836967869E1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "Q";

    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Phase));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Phases);
      Menu_AppendItem
      (
        menu, $"Open Phases…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryPhases() : base
    (
      name: "Query Phases",
      nickname: "Phases",
      description: "Get document construction phases list",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Phase name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional),
    };
    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Phase>("Phases", "P", "Phases list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Filter", out DB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var phasesCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          phasesCollector = phasesCollector.WherePasses(filter);

        if (name is object && TryGetFilterStringParam(DB.BuiltInParameter.PHASE_NAME, ref name, out var nameFilter))
          phasesCollector = phasesCollector.WherePasses(nameFilter);

        var phases = collector.Cast<DB.Phase>();

        if (name is object)
          phases = phases.Where(x => x.get_Parameter(DB.BuiltInParameter.PHASE_NAME).AsString().IsSymbolNameLike(name));

        DA.SetDataList
        (
          "Phases",
          phases.
          Select(x => new Types.Phase(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
