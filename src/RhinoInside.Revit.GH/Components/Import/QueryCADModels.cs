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

  [ComponentVersion(introduced: "1.36", updated: "1.36")]
  public class QueryCADModels : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("563C1170-6FB2-4557-9B61-CFB6BEC5E1D3");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => string.Empty;
    protected override ARDB.ElementFilter ElementFilter => External.DB.ElementFilters.Union
    (
      new ARDB.ElementClassFilter(typeof(ARDB.ImportInstance)),
      new ARDB.ElementClassFilter(typeof(ARDB.CADLinkType))
    );

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.ManageLinks, "Manage Links…");
    }
    #endregion

    public QueryCADModels() : base
    (
      name: "Query CAD Models",
      nickname: "CAD-Models",
      description: "Gets Revit imported CAD models into given document",
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
        new Parameters.GraphicalElement()
        {
          Name = "Imports",
          NickName = "I",
          Description = "Imported CAD models into the given model",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Links",
          NickName = "L",
          Description = "Linked CAD models into the given model",
          Access = GH_ParamAccess.list
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.ElementSource.GetElementSourceOrCurrent(this, DA, out var source)) return;
      if (!Params.TryGetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      var elements = view is null ?
                    source is Types.Document document ? document.Value.CollectElements() :
                    (source is Types.RevitLinkInstance link ? view.Value.CollectElements(link.Id) : view.Value.CollectElements()) :
                    Array.Empty<ARDB.Element>();

      if (filter is object)
        elements = elements.WherePasses(filter, source.SourceInstance.Value);

      if (TryGetFilterStringParam(ARDB.BuiltInParameter.IMPORT_SYMBOL_NAME, ref name, out var nameFilter))
        elements = elements.WherePasses(nameFilter);

      if (!string.IsNullOrEmpty(name))
        elements = elements.Where(x => x.GetNomen(ARDB.BuiltInParameter.IMPORT_SYMBOL_NAME).IsSymbolNameLike(name));

      if (Params.Output.Count > 0)
      {
        var imports = elements.Cast<ARDB.ImportInstance>().ToArray();

        Params.TrySetDataList
        (
          DA,
          "Imports",
          () => imports.
          Where(x => !x.IsLinked).
          OfType<Types.ImportInstance>().
          FromSource(source).
          TakeWhileIsNotEscapeKeyDown(this)
        );
        Params.TrySetDataList
        (
          DA,
          "Links",
          () => imports.
          Where(x => x.IsLinked).
          OfType<Types.ImportInstance>().
          FromSource(source).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
