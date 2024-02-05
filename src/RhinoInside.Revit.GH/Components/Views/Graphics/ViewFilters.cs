using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Views
{
  using System.Collections.Generic;
  using System.Linq;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.11")]
  public class ViewFilters : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("61812ADE-D693-405C-A450-687CB7A0BDF7");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "VF";

    public ViewFilters() : base
    (
      name: "View Filters",
      nickname: "Filters",
      description: "View Get-Set Filters",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to access filters"
      ),
      ParamDefinition.Create<Parameters.FilterElement>
      (
        name: "Filters",
        nickname: "F",
        description:  "Set of document stored view filters to apply to the view",
        optional: true,
        access: GH_ParamAccess.list,
        relevance: ParamRelevance.Primary
      )
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>
      (
        name: "View",
        nickname: "V",
        description: "View to access filters",
        relevance: ParamRelevance.Primary
      ),
      ParamDefinition.Create<Parameters.FilterElement>
      (
        name: "Filters",
        nickname: "F",
        description:  "Set of document stored view filters applied to the view",
        access: GH_ParamAccess.list,
        relevance: ParamRelevance.Primary
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (Params.GetDataList(DA, "Filters", out IList<Types.FilterElement> filters))
      {
        StartTransaction(view.Document);

        var viewFilters = view.Value.GetFilters().AsReadOnlyElementIdSet();
        var filtersToAdd = new HashSet<ARDB.ElementId>(filters.Count);
        
        foreach (var filter in filters.OfType<Types.FilterElement>())
        {
          if (!filter.Document.IsEquivalent(view.Document)) continue;

          filtersToAdd.Add(filter.Id);
          if (!viewFilters.Contains(filter.Id))
            view.Value.AddFilter(filter.Id);
        }

        foreach (var filterId in viewFilters)
        {
          if (!filtersToAdd.Contains(filterId))
            view.Value.RemoveFilter(filterId);
        }
      }

      Params.TrySetDataList(DA, "Filters", () => view.Value.GetFilters().Select(x => Types.FilterElement.FromElement(view.Document.GetElement(x))));
    }
  }
}
