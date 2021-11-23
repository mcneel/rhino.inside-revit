using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  public class QueryFilters : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("B7B1740B-0721-49C8-92F5-057775DA9792");
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    protected override string IconTag => "F";

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.FilterElement));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.EditSelection);
      Menu_AppendItem
      (
        menu, $"Edit Filters…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryFilters() : base
    (
      name: "Query Filters",
      nickname: "Filters",
      description: "Get document filters list",
      category: "Revit",
      subCategory: "Filter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition (new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Filter name", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Parameter Filters", "P", "Parameter filter list", GH_ParamAccess.list),
      ParamDefinition.Create<Parameters.Element>("Selection Filters", "S", "Selection filter list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      string name = null;
      DA.GetData("Name", ref name);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var filtersCollector = collector.WherePasses(ElementFilter);

        var filters = filtersCollector.TakeWhileIsNotEscapeKeyDown(this).Cast<ARDB.FilterElement>();

        if (!string.IsNullOrEmpty(name))
          filters = filters.Where(x => x.Name.IsSymbolNameLike(name));

        DA.SetDataList("Parameter Filters", filters.Where(x => x is ARDB.ParameterFilterElement));
        DA.SetDataList("Selection Filters", filters.Where(x => x is ARDB.SelectionFilterElement));
      }
    }
  }
}
