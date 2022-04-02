using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.2.1")]
  public class QueryViews : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("DF691659-B75B-4455-AF5F-8A5DE485FA05");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "V";
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.View));

    public QueryViews() : base
    (
      name: "Query Views",
      nickname: "Views",
      description: "Get all document views",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewDiscipline>>("Discipline", "D", "View discipline", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewFamily>>("View Family", "VF", "View family", optional: true),
      ParamDefinition.Create<Param_String>("View Name", "VN", "View name", optional: true),
      ParamDefinition.Create<Param_Boolean>("Is Dependent", "D", "View is dependent", defaultValue: false, optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.View>("View Dependency", "VD", "View depedency", optional: true, relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Is Template", "IT", "View is template", defaultValue: false, optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.View>("View Template", "T", "View template", optional: true, relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Is Assembly", "IA", "View is assembly", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.AssemblyInstance>("Associated Assembly", "AA", "The assembly the view is associated with", optional: true, relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Is Printable", "IP", "View is printable", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", optional: true, relevance: ParamRelevance.Secondary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>("Views", "V", "Views list", GH_ParamAccess.list)
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Input<IGH_Param>("Family") is IGH_Param family)
        family.Name = "View Family";

      if (Params.Input<IGH_Param>("Name") is IGH_Param name)
        name.Name = "View Name";

      if (Params.Input<IGH_Param>("Template") is IGH_Param template)
        template.Name = "View Template";

      if (Params.Input<IGH_Param>("Assembly") is IGH_Param assembly)
        assembly.Name = "Associated Assembly";

      base.AddedToDocument(document);
    }


    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Discipline", out ARDB.ViewDiscipline? viewDiscipline)) return;
      if (!Params.TryGetData(DA, "View Family", out ARDB.ViewFamily? viewFamily)) return;
      if (!Params.TryGetData(DA, "View Name", out string viewName)) return;
      if (!Params.TryGetData(DA, "Is Dependent", out bool? isDependent)) return;
      if (!Params.TryGetData(DA, "View Dependency", out Types.View viewDependency)) return;
      if (!Params.TryGetData(DA, "Is Template", out bool? isTemplate)) return;
      if (!Params.TryGetData(DA, "View Template", out Types.View viewTemplate)) return;
      if (!Params.TryGetData(DA, "Is Assembly", out bool? isAssembly)) return;
      if (!Params.TryGetData(DA, "Associated Assembly", out Types.AssemblyInstance assembly)) return;
      if (!Params.TryGetData(DA, "Is Printable", out bool? isPrintable)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var viewsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          viewsCollector = viewsCollector.WherePasses(filter);

        if (viewDiscipline.HasValue && TryGetFilterIntegerParam(ARDB.BuiltInParameter.VIEW_DISCIPLINE, (int) viewDiscipline, out var viewDisciplineFilter))
          viewsCollector = viewsCollector.WherePasses(viewDisciplineFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.VIEW_NAME, ref viewName, out var viewNameFilter))
          viewsCollector = viewsCollector.WherePasses(viewNameFilter);

        if (viewTemplate is object && TryGetFilterElementIdParam(ARDB.BuiltInParameter.VIEW_TEMPLATE, viewTemplate.Id, out var templateFilter))
          viewsCollector = viewsCollector.WherePasses(templateFilter);

        if (assembly is object && TryGetFilterElementIdParam(ARDB.BuiltInParameter.VIEW_ASSOCIATED_ASSEMBLY_INSTANCE_ID, assembly.Id, out var assemblyFilter))
          viewsCollector = viewsCollector.WherePasses(assemblyFilter);

        var views = collector.Cast<ARDB.View>();

        if (viewDependency is object)
          views = views.Where(x => x.GetPrimaryViewId() == viewDependency.Id);

        if (isDependent.HasValue)
          views = views.Where(x => (x.GetPrimaryViewId() != ARDB.ElementId.InvalidElementId) == isDependent.Value);

        if (isTemplate.HasValue)
          views = views.Where(x => x.IsTemplate == isTemplate.Value);

        if (isPrintable.HasValue)
          views = views.Where(x => x.CanBePrinted == isPrintable.Value);

        if (isAssembly.HasValue)
          views = views.Where(x => x.IsAssemblyView == isAssembly.Value);

        if (viewFamily.HasValue)
          views = views.Where(x => x.GetViewFamily() == viewFamily);
        else
          views = views.Where(x => x.GetViewFamily() != ARDB.ViewFamily.Invalid);

        if (viewName is object)
          views = views.Where(x => x.Name.IsSymbolNameLike(viewName));

        DA.SetDataList
        (
          "Views",
          views.
          Select(Types.View.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
