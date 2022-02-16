using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DesignOptions
{
  public class QueryDesignOptionSets : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("B31E7605-87F6-421B-84C5-8B00BFD0BC88");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "Q";

    protected override ARDB.ElementFilter ElementFilter => Types.DesignOptionSet.IsValidElementFilter;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var hasInputDocument = Params.Input<Parameters.Document>("Document") is object;
      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.DesignOptions);
      Menu_AppendItem
      (
        menu, $"Open Design Options…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        !hasInputDocument && activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryDesignOptionSets() : base
    (
      name: "Query Design Option Sets",
      nickname: "Option Sets",
      description: "Get all document design options",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Name", "N", "Design Option name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Design Option Sets", "V", "Design Option Sets list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var optionsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          optionsCollector = optionsCollector.WherePasses(filter);

        if (name is object && TryGetFilterStringParam(ARDB.BuiltInParameter.OPTION_SET_NAME, ref name, out var nameFilter))
          optionsCollector = optionsCollector.WherePasses(nameFilter);

        var options = collector.Cast<ARDB.Element>();

        if (name is object)
          options = options.Where(x => x.get_Parameter(ARDB.BuiltInParameter.OPTION_SET_NAME).AsString().IsSymbolNameLike(name));

        DA.SetDataList
        (
          "Design Option Sets",
          options.
          Select(x => new Types.DesignOptionSet(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }

  public class QueryDesignOptions : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("6804582B-E6FD-4825-AA71-B59346F149CD");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "Q";

    static readonly ARDB.ElementFilter elementFilter = new ARDB.ElementClassFilter(typeof(ARDB.DesignOption));
    protected override ARDB.ElementFilter ElementFilter => elementFilter;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var hasInputDocument = Params.Input<Parameters.Document>("Document") is object;
      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.DesignOptions);
      Menu_AppendItem
      (
        menu, $"Open Design Options…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        !hasInputDocument && activeApp.CanPostCommand(commandId), false
      );
    }
    #endregion

    public QueryDesignOptions() : base
    (
      name: "Query Design Options",
      nickname: "Design Options",
      description: "Get all document design options",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Element>("Design Option Set", "DOS", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Design Option name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Primary", "P", "Design Option is primary", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Design Options", "V", "Design Options list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Design Option Set", out Types.DesignOptionSet set)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Primary", out bool? primary)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      if (!(set?.Document is null || doc.Equals(set.Document)))
        throw new System.ArgumentException("Wrong Document.", "Design Option Set");

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var optionsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          optionsCollector = optionsCollector.WherePasses(filter);

        if (set is object && TryGetFilterElementIdParam(ARDB.BuiltInParameter.OPTION_SET_ID, set.Id, out var optionSetFilter))
          optionsCollector = optionsCollector.WherePasses(optionSetFilter);

        if (name is object && TryGetFilterStringParam(ARDB.BuiltInParameter.OPTION_NAME, ref name, out var nameFilter))
          optionsCollector = optionsCollector.WherePasses(nameFilter);

        var options = collector.Cast<ARDB.DesignOption>();

        if (primary.HasValue)
          options = options.Where(x => x.IsPrimary == primary.Value);

        if (name is object)
          options = options.Where(x => x.get_Parameter(ARDB.BuiltInParameter.OPTION_NAME).AsString().IsSymbolNameLike(name));

        if
        (
          (set is null || set.Id == ARDB.ElementId.InvalidElementId) &&
          (name is null || name == "Main Model") &&
          primary != false
        )
        {
          options = Enumerable.Repeat(default(ARDB.DesignOption), 1).Concat(options);
        }

        DA.SetDataList
        (
          "Design Options",
          options.
          Select(x => new Types.DesignOption(x)).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
