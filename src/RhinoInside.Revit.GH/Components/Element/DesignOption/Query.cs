using System;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.DesignOption
{
  public class QueryDesignOptionSets : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("B31E7605-87F6-421B-84C5-8B00BFD0BC88");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "Q";

    static readonly DB.ElementFilter elementFilter = new DB.ElementParameterFilter
    (
      new DB.FilterStringRule
      (
        new DB.ParameterValueProvider(new DB.ElementId(DB.BuiltInParameter.OPTION_SET_NAME)),
        new DB.FilterStringEquals(),
        string.Empty,
        true
      ),
      true
    );
    protected override DB.ElementFilter ElementFilter => elementFilter;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.DesignOptions);
      Menu_AppendItem
      (
        menu, $"Open Design Options…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
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
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Param_String>("Name", "N", "Design Option name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamVisibility.Voluntary),
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
      if (!Params.TryGetData(DA, "Filter", out DB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var optionsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          optionsCollector = optionsCollector.WherePasses(filter);

        if (name is object && TryGetFilterStringParam(DB.BuiltInParameter.OPTION_SET_NAME, ref name, out var nameFilter))
          optionsCollector = optionsCollector.WherePasses(nameFilter);

        var options = collector.Cast<DB.Element>();

        if (name is object)
          options = options.Where(x => x.get_Parameter(DB.BuiltInParameter.OPTION_SET_NAME).AsString().IsSymbolNameLike(name));

        DA.SetDataList("Design Option Sets", options.Select(x => new Types.DesignOptionSet(x)));
      }
    }
  }

  public class QueryDesignOptions : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("6804582B-E6FD-4825-AA71-B59346F149CD");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "Q";

    static readonly DB.ElementFilter elementFilter = new DB.ElementClassFilter(typeof(DB.DesignOption));
    protected override DB.ElementFilter ElementFilter => elementFilter;

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.DesignOptions);
      Menu_AppendItem
      (
        menu, $"Open Design Options…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
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
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.Create<Parameters.Element>("Design Option Set", "DOS", string.Empty, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "Design Option name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Primary", "P", "Design Option is primary", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true, relevance: ParamVisibility.Voluntary),
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

      if (!Params.TryGetData(DA, "Design Option Set", out Types.DesignOptionSet set, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Name", out string name)) return;
      if (!Params.TryGetData(DA, "Primary", out bool? primary)) return;
      if (!Params.TryGetData(DA, "Filter", out DB.ElementFilter filter, x => x.IsValidObject)) return;

      if (!(set?.Document is null || doc.Equals(set.Document)))
        throw new System.ArgumentException("Wrong Document.", "Design Option Set");

      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var optionsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          optionsCollector = optionsCollector.WherePasses(filter);

        if (set is object && TryGetFilterElementIdParam(DB.BuiltInParameter.OPTION_SET_ID, set.Id, out var optionSetFilter))
          optionsCollector = optionsCollector.WherePasses(optionSetFilter);

        if (name is object && TryGetFilterStringParam(DB.BuiltInParameter.OPTION_NAME, ref name, out var nameFilter))
          optionsCollector = optionsCollector.WherePasses(nameFilter);

        var options = collector.Cast<DB.DesignOption>();

        if (primary.HasValue)
          options = options.Where(x => x.IsPrimary == primary.Value);

        if (name is object)
          options = options.Where(x => x.get_Parameter(DB.BuiltInParameter.OPTION_NAME).AsString().IsSymbolNameLike(name));

        if
        (
          (set is null || set.Id == DB.ElementId.InvalidElementId) &&
          (name is null || name == "Main Model") &&
          primary != false
        )
        {
          options = Enumerable.Repeat(default(DB.DesignOption), 1).Concat(options);
        }

        DA.SetDataList("Design Options", options.Select(x => new Types.DesignOption(x)));
      }
    }
  }
}
