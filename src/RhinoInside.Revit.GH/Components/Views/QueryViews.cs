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
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewDiscipline>>("Discipline", "D", "View discipline", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.Param_Enum<Types.ViewFamily>>("Family", "T", "View family", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_String>("Name", "N", "View name", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.View>("Template", "T", "Views template", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Is Template", "IT", "View is template", false, GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Is Printable", "IP", "View is printable", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Param_Boolean>("Is Assembly", "IA", "View is assembly", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.AssemblyInstance>("Assembly", "A", "Assembly the view belongs to", GH_ParamAccess.item, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>("Views", "V", "Views list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var viewDiscipline = default(ARDB.ViewDiscipline);
      var _Discipline_ = Params.IndexOfInputParam("Discipline");
      bool nofilterDiscipline = (!DA.GetData(_Discipline_, ref viewDiscipline) && Params.Input[_Discipline_].Sources.Count == 0);

      var viewFamily = ARDB.ViewFamily.Invalid;
      DA.GetData("Family", ref viewFamily);

      string name = null;
      DA.GetData("Name", ref name);

      var Template = default(ARDB.View);
      var _Template_ = Params.IndexOfInputParam("Template");
      bool nofilterTemplate = (!DA.GetData(_Template_, ref Template) && Params.Input[_Template_].DataType == GH_ParamData.@void);

      bool IsTemplate = false;
      var _IsTemplate_ = Params.IndexOfInputParam("Is Template");
      bool nofilterIsTemplate = (!DA.GetData(_IsTemplate_, ref IsTemplate) && Params.Input[_IsTemplate_].DataType == GH_ParamData.@void);

      bool IsPrintable = false;
      var _IsPrintable_ = Params.IndexOfInputParam("Is Printable");
      bool nofilterIsPrintable = (!DA.GetData(_IsPrintable_, ref IsPrintable) && Params.Input[_IsPrintable_].DataType == GH_ParamData.@void);

      bool IsAssembly = false;
      var _IsAssembly_ = Params.IndexOfInputParam("Is Assembly");
      bool nofilterIsAssembly = (!DA.GetData(_IsAssembly_, ref IsAssembly) && Params.Input[_IsAssembly_].DataType == GH_ParamData.@void);

      var Assembly = default(Types.AssemblyInstance);
      var _Assembly_ = Params.IndexOfInputParam("Assembly");
      bool noFilterAssembly = (!DA.GetData(_Assembly_, ref Assembly) && Params.Input[_Assembly_].DataType == GH_ParamData.@void);

      ARDB.ElementFilter filter = null;
      DA.GetData("Filter", ref filter);

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var viewsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          viewsCollector = viewsCollector.WherePasses(filter);

        if (!nofilterDiscipline && TryGetFilterIntegerParam(ARDB.BuiltInParameter.VIEW_DISCIPLINE, (int) viewDiscipline, out var viewDisciplineFilter))
          viewsCollector = viewsCollector.WherePasses(viewDisciplineFilter);

        if (TryGetFilterStringParam(ARDB.BuiltInParameter.VIEW_NAME, ref name, out var viewNameFilter))
          viewsCollector = viewsCollector.WherePasses(viewNameFilter);

        if (!nofilterTemplate && TryGetFilterElementIdParam(ARDB.BuiltInParameter.VIEW_TEMPLATE, Template?.Id ?? ARDB.ElementId.InvalidElementId, out var templateFilter))
          viewsCollector = viewsCollector.WherePasses(templateFilter);

        if (!noFilterAssembly && TryGetFilterElementIdParam(ARDB.BuiltInParameter.VIEW_ASSOCIATED_ASSEMBLY_INSTANCE_ID, Assembly?.Id ?? ARDB.ElementId.InvalidElementId, out var assemblyFilter))
          viewsCollector = viewsCollector.WherePasses(assemblyFilter);

        var views = collector.Cast<ARDB.View>();

        if (!nofilterIsTemplate)
          views = views.Where((x) => x.IsTemplate == IsTemplate);

        if (!nofilterIsPrintable)
          views = views.Where((x) => x.CanBePrinted == IsPrintable);

        if (!nofilterIsAssembly)
          views = views.Where((x) => x.IsAssemblyView == IsAssembly);

        if (viewFamily != ARDB.ViewFamily.Invalid)
          views = views.Where(x => x.GetViewFamily() == viewFamily);
        else
          views = views.Where(x => x.GetViewFamily() != viewFamily);

        if (name is object)
          views = views.Where(x => x.Name.IsSymbolNameLike(name));

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
