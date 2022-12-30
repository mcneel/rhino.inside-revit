using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.11")]
  public class FilterGraphicOverrides : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("1A137425-C54D-465F-A2EE-79B9772E0C3D");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "O";

    public FilterGraphicOverrides() : base
    (
      name: "Filter Graphic Overrides",
      nickname: "FG-Overrides",
      description: "Get-Set filter graphics overrides on the specified View",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query filter graphics overrides",
        }
      ),
      new ParamDefinition
      (
        new Parameters.FilterElement()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Filter to access graphics overrides",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Enabled",
          NickName = "E",
          Description = "Filter enabled state",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Filter hidden state",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Overrides",
          NickName = "O",
          Description = "Filter graphic overrides",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query filter graphic overrides",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FilterElement()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Filter to access graphic overrides state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Enabled",
          NickName = "E",
          Description = "Filter enabled state",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Filter hidden state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Overrides",
          NickName = "O",
          Description = "Filter graphic overrides",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.GetDataList(DA, "Filter", out IList<Types.FilterElement> filters)) return;
      else Params.TrySetDataList(DA, "Filter", () => filters);
#if REVIT_2021
      if (Params.GetDataList(DA, "Enabled", out IList<bool?> enabled) && enabled.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          var filtersToDisable = new HashSet<ARDB.ElementId>(filters.Count);
          var filtersToEnable = new HashSet<ARDB.ElementId>(filters.Count);

          foreach (var pair in filters.ZipOrLast(enabled, (Filter, Enabled) => (Filter, Enabled)))
          {
            if (!pair.Enabled.HasValue) continue;
            if (!view.Document.IsEquivalent(pair.Filter?.Document)) continue;
            if (pair.Filter?.IsValid != true) continue;
            if (!view.Value.GetFilters().Contains(pair.Filter.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Filter '{pair.Filter.Nomen}' is not applied to view '{view.Value.Title}'.");
              continue;
            }

            if (pair.Enabled.Value)
            {
              filtersToEnable.Remove(pair.Filter.Id);
              filtersToDisable.Add(pair.Filter.Id);
            }
            else
            {
              filtersToDisable.Remove(pair.Filter.Id);
              filtersToEnable.Add(pair.Filter.Id);
            }
          }

          StartTransaction(view.Document);

          foreach (var filterId in filtersToDisable)
            view.Value.SetIsFilterEnabled(filterId, false);

          foreach (var filterId in filtersToEnable)
            view.Value.SetIsFilterEnabled(filterId, true);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Enabled", () => filters.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x.Id is ARDB.ElementId filterId && view.Value.GetFilters().Contains(filterId) ?
               view.Value.GetIsFilterEnabled(filterId) :
               default(bool?)
        )
      );
#endif

      if (Params.GetDataList(DA, "Hidden", out IList<bool?> hidden) && hidden.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          var filtersToHide = new HashSet<ARDB.ElementId>(filters.Count);
          var filtersToUnhide = new HashSet<ARDB.ElementId>(filters.Count);

          foreach (var pair in filters.ZipOrLast(hidden, (Filter, Hidden) => (Filter, Hidden)))
          {
            if (!pair.Hidden.HasValue) continue;
            if (!view.Document.IsEquivalent(pair.Filter?.Document)) continue;
            if (pair.Filter?.IsValid != true) continue;
            if (!view.Value.IsFilterApplied(pair.Filter.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Filter '{pair.Filter.Nomen}' is not applied to view '{view.Value.Title}'.");
              continue;
            }

            if (pair.Hidden.Value)
            {
              filtersToUnhide.Remove(pair.Filter.Id);
              filtersToHide.Add(pair.Filter.Id);
            }
            else
            {
              filtersToHide.Remove(pair.Filter.Id);
              filtersToUnhide.Add(pair.Filter.Id);
            }
          }

          StartTransaction(view.Document);

          foreach (var categoryId in filtersToHide)
            view.Value.SetFilterVisibility(categoryId, false);

          foreach (var categoryId in filtersToUnhide)
            view.Value.SetFilterVisibility(categoryId, true);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Hidden", () => filters.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x.Id is ARDB.ElementId filterId && view.Value.GetFilters().Contains(filterId) ?
               !view.Value.GetFilterVisibility(filterId) :
               default(bool?)
        )
      );

      if (Params.GetDataList(DA, "Overrides", out IList<Types.OverrideGraphicSettings> overrides) && overrides.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          StartTransaction(view.Document);

          foreach (var pair in filters.ZipOrLast(overrides, (Filter, Overrides) => (Filter, Overrides)))
          {
            if (pair.Overrides?.Value is null) continue;
            if (!view.Document.IsEquivalent(pair.Filter?.Document)) continue;
            if (pair.Filter?.IsValid != true) continue;
            if (!view.Value.IsFilterApplied(pair.Filter.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Filter '{pair.Filter.Nomen}' is not applied to view '{view.Value.Title}'.");
              continue;
            }

            var settings = pair.Overrides.Document.IsEquivalent(view.Document) ? pair.Overrides :
                           new Types.OverrideGraphicSettings(view.Document, pair.Overrides);

            // Reset filter visibility here to force Revit redraw using this new settings.
            var visibility = view.Value.GetFilterVisibility(pair.Filter.Id);
            if (visibility) view.Value.SetFilterVisibility(pair.Filter.Id, false);
            view.Value.SetFilterOverrides(pair.Filter.Id, settings.Value);
            if (visibility) view.Value.SetFilterVisibility(pair.Filter.Id, true);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Overrides", () => filters.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x?.Id is ARDB.ElementId filterId && view.Value.GetFilters().Contains(filterId) &&
               view.Value.GetFilterOverrides(filterId) is ARDB.OverrideGraphicSettings overrideSettings ?
               new Types.OverrideGraphicSettings(x.Document, overrideSettings) :
               default
        )
      );
    }
  }
}
