using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.LinePatternElements
{
  using External.DB;

  [ComponentVersion(introduced: "1.0", updated: "1.36")]
  public class QueryLinePatterns : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("A94000FD-8BCD-49D5-9F00-09BEDB88A123");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.LinePatternElement));

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.LinePatterns, "Open Line Patterns…");
    }
    #endregion

    public QueryLinePatterns() : base
    (
      name: "Query Line Patterns",
      nickname: "Line Patterns",
      description: "Get document line patterns list",
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.ElementSource(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Line pattern name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.LinePatternElement>("Line Patterns", "LP", "Line pattern list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.ElementSource.GetElementSourceOrCurrent(this, DA, out var source)) return;

      string name = null;
      DA.GetData("Name", ref name);

      Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter);

      using (var collector = new ARDB.FilteredElementCollector(source.SourceDocument.Value))
      {
        var patternsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          patternsCollector = patternsCollector.WherePasses(filter, source.SourceInstance.Value);

        var patterns =
          Enumerable.Repeat(new Types.LinePatternElement(source.SourceDocument.Value, ARDB.LinePatternElement.GetSolidPatternId()), 1).
          Concat(collector.Cast<ARDB.LinePatternElement>().Select(x => new Types.LinePatternElement(x)));

        if (!string.IsNullOrEmpty(name))
          patterns = patterns.Where(x => x.Nomen.IsSymbolNameLike(name));

        DA.SetDataList
        (
          "Line Patterns",
          patterns.
          FromSource(source).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }

}
