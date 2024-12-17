using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
#if REVIT_2023
  using ARDB_AnalyticalElement = ARDB.Structure.AnalyticalElement;
#else
  using ARDB_AnalyticalElement = ARDB.Structure.AnalyticalModel;
#endif

  [ComponentVersion(introduced: "1.27")]
  public class QueryAnalyticalElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("1D518EBF-D75D-4D9C-B962-9907352DF89A");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

    private ARDB.ElementFilter _ElementFilter = new ARDB.ElementClassFilter(typeof(ARDB_AnalyticalElement));
    protected override ARDB.ElementFilter ElementFilter => _ElementFilter;

    public QueryAnalyticalElements() : base
    (
      name: "Query Analytical Elements",
      nickname: "A-Elements",
      description: "Get all document analytical elements",
      category: "Revit",
      subCategory: "Structure"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.AnalyticalStructuralRole>>
      (
        name: "Structural Role",
        nickname: "SR",
        optional: true,
#if REVIT_2023
        relevance: ParamRelevance.Primary
#else
        relevance: ParamRelevance.Occasional
#endif
      ),
      ParamDefinition.Create<Parameters.Param_Enum<Types.AnalyzeAs>>
      (
        name: "Analyze As",
        nickname: "AS",
        optional: true,
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.ElementFilter>
      (
        name: "Filter",
        nickname: "F",
        description: "Additional Filter.",
        optional: true,
        relevance: ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.AnalyticalElement>("Analytical Elements", "AE", "Analytical elements list", GH_ParamAccess.list),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Structural Role", out ARDB.Structure.AnalyticalStructuralRole? structuralRole)) return;
      if (!Params.TryGetData(DA, "Analyze As", out ARDB.Structure.AnalyzeAs? analyzeAs)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var elementsCollector = collector.WherePasses(ElementFilter);

        if (filter is object)
          elementsCollector = elementsCollector.WherePasses(filter);

#if REVIT_2023
        if (structuralRole is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.ANALYTICAL_ELEMENT_STRUCTURAL_ROLE, (int) structuralRole);
#endif
        if (analyzeAs is object)
          elementsCollector = elementsCollector.WhereParameterEqualsTo(ARDB.BuiltInParameter.STRUCTURAL_ANALYZES_AS, (int) analyzeAs);

        Params.TrySetDataList(DA, "Analytical Elements",   () => collector.Select(Types.AnalyticalElement.FromElement).TakeWhileIsNotEscapeKeyDown(this));
      }
    }
  }
}
