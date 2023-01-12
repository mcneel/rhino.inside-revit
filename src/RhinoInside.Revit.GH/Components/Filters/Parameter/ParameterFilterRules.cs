using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.12")]
  public class ParameterFilterRules : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("E4E08F99-1A83-4219-8AFD-A02D30CD154D");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public ParameterFilterRules() : base
    (
      name: "Rule-based Filter Definiton",
      nickname: "RuleDef",
      description: "Get-Set accessor for Rule-based Filter definition.",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.FilterElement>("Rule-based Filter", "R"),
      ParamDefinition.Create<Parameters.Category>("Categories", "C", access: GH_ParamAccess.list, optional: true, relevance: ParamRelevance.Primary),
#if REVIT_2019
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", access: GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Primary)
#else
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", access: GH_ParamAccess.item, optional: true, relevance: ParamRelevance.Occasional)
#endif
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.FilterElement>("Rule-based Filter", "R"),
      ParamDefinition.Create<Parameters.Category>("Categories", "C", access: GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", access: GH_ParamAccess.item, relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Rule-based Filter", out ARDB.ParameterFilterElement ruleFilter, x => x.IsValid())) return;
      else DA.SetData("Rule-based Filter", ruleFilter);

      if (Params.GetDataList(DA, "Catgories", out IList<Types.Category> categories))
      {
        StartTransaction(ruleFilter.Document);

        var categoryIds = categories?.Where(x => ruleFilter.Document.IsEquivalent(x.Document)).Select(x => x.Id).ToList() as ICollection<ARDB.ElementId>;

        var inputCategoryIds = categoryIds;
        categoryIds = ARDB.ParameterFilterUtilities.RemoveUnfilterableCategories(inputCategoryIds);
        if (categoryIds.Count != inputCategoryIds.Count)
        {
          if (FailureProcessingMode != ARDB.FailureProcessingResult.ProceedWithCommit)
            throw new Exceptions.RuntimeErrorException("Input 'Categories' parameter contains unfilterable categories.");
          else foreach (var id in inputCategoryIds.Where(x => !categoryIds.Contains(x)))
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Unfilterable category '{ruleFilter.Document.GetCategory(id)?.Name}' was not applied.");
        }

        ruleFilter.SetCategories(categoryIds);
      } 

      if (Params.GetData(DA, "Filter", out Types.ElementFilter filter, x => x.IsValid))
      {
        var filterValue = filter.Value;
        if (!ruleFilter.ElementFilterIsAcceptableForParameterFilterElement(filterValue))
          throw new Exceptions.RuntimeErrorException
          (
#if REVIT_2019
            $"The input 'Filter' is not acceptable for use by a Rule-based Filter.\r" +
            "Only Parameter Filters or Logical combinations of these are accepted."
#else
              "Parameter Filter is partially supported before Revit 2019."
#endif
          );

        StartTransaction(ruleFilter.Document);
        ruleFilter.SetElementFilter(filterValue);
      }

      Params.TrySetDataList(DA, "Categories", () => ruleFilter.GetCategories().Select(x => Types.Element.FromElementId(ruleFilter.Document, x)));
      Params.TrySetData(DA, "Filter", () => ruleFilter.GetElementFilter());
    }
  }
}
