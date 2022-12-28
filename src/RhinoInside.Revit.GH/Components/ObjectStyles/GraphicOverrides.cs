using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Convert.Geometry;
  using Convert.System.Drawing;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.11")]
  public class GraphicSettings : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("BFD4A970-CE90-47D3-B196-103E0DDCE977");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "GS";

    public GraphicSettings() : base
    (
      name: "Graphic Settings",
      nickname: "G-Settings",
      description: "Get-Set element graphics overrides on the specified View",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Halftone", NickName = "H", Description = "Element Halftone state", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LinePatternElement() { Name = "Pattern : Projection Lines", NickName = "PLP", Description = "Element projection line pattern", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour() { Name = "Color : Projection Lines", NickName = "CPL", Description = "Element projection line color", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer() { Name = "Weight : Projection Lines", NickName = "WPL", Description = "Element projection line weight", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Foreground Visible : Surface Patterns", NickName = "FVSP", Description = "Element foreground surface patterns visibility", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Foreground Pattern : Surface Patterns", NickName = "FPSP", Description = "Element foreground surface pattern", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour() { Name = "Foreground Color : Surface Patterns", NickName = "FCSP", Description = "Element foreground surface color", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Background Visible : Surface Patterns", NickName = "BVSP", Description = "Element background surface patterns visibility", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Background Pattern : Surface Patterns", NickName = "BPSP", Description = "Element background surface pattern", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour() { Name = "Background Color : Surface Patterns", NickName = "BCSP", Description = "Element background surface color", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number() { Name = "Transparency : Surface", NickName = "TS", Description = "Element surface transparency [0.0 .. 1.0]", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LinePatternElement() { Name = "Pattern : Cut Lines", NickName = "PCL", Description = "Element cut line pattern", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour() { Name = "Color : Cut Lines", NickName = "CCL", Description = "Element cut line color", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer() { Name = "Weight : Cut Lines", NickName = "WCL", Description = "Element cut line weight", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Foreground Visible : Cut Patterns", NickName = "FVCP", Description = "Element foreground cut patterns visibility", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Foreground Pattern : Cut Patterns", NickName = "FPCP", Description = "Element foreground cut pattern", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour() { Name = "Foreground Color : Cut Patterns", NickName = "FCCP", Description = "Element foreground cut color", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Background Visible : Cut Patterns", NickName = "BVCP", Description = "Element background cut patterns visibility", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Background Pattern : Cut Patterns", NickName = "BPCP", Description = "Element background cut pattern", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour() { Name = "Background Color : Cut Patterns", NickName = "BCCP", Description = "Element background cut color", Optional = true }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Settings",
          NickName = "S",
          Description = "Graphic Settings",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc))
        return;

      if (!Params.TryGetData(DA, "Halftone", out bool? halftone)) return;

      if (!Params.TryGetData(DA, "Pattern : Projection Lines", out Types.LinePatternElement projectionLinePattern)) return;
      if (!Params.TryGetData(DA, "Color : Projection Lines", out System.Drawing.Color? projectionLineColor)) return;
      if (!Params.TryGetData(DA, "Weight : Projection Lines", out int? projectionLineWeight)) return;

      if (!Params.TryGetData(DA, "Foreground Visible : Surface Patterns", out bool? surfaceForegroundPatternVisible)) return;
      if (!Params.TryGetData(DA, "Foreground Pattern : Surface Patterns", out Types.FillPatternElement surfaceForegroundPattern)) return;
      if (!Params.TryGetData(DA, "Foreground Color : Surface Patterns", out System.Drawing.Color? surfaceForegroundPatternColor)) return;

      if (!Params.TryGetData(DA, "Background Visible : Surface Patterns", out bool? surfaceBackgroundPatternVisible)) return;
      if (!Params.TryGetData(DA, "Background Pattern : Surface Patterns", out Types.FillPatternElement surfaceBackgroundPattern)) return;
      if (!Params.TryGetData(DA, "Background Color : Surface Patterns", out System.Drawing.Color? surfaceBackgroundPatternColor)) return;

      if (!Params.TryGetData(DA, "Transarency : Surface", out double? surfaceTransparency)) return;

      if (!Params.TryGetData(DA, "Pattern : Cut Lines", out Types.LinePatternElement cutLinePattern)) return;
      if (!Params.TryGetData(DA, "Color : Cut Lines", out System.Drawing.Color? cutLineColor)) return;
      if (!Params.TryGetData(DA, "Weight : Cut Lines", out int? cutLineWeight)) return;

      if (!Params.TryGetData(DA, "Foreground Visible : Cut Patterns", out bool? cutForegroundPatternVisible)) return;
      if (!Params.TryGetData(DA, "Foreground Pattern : Cut Patterns", out Types.FillPatternElement cutForegroundPattern)) return;
      if (!Params.TryGetData(DA, "Foreground Color : Cut Patterns", out System.Drawing.Color? cutForegroundPatternColor)) return;

      if (!Params.TryGetData(DA, "Background Visible : Cut Patterns", out bool? cutBackgroundPatternVisible)) return;
      if (!Params.TryGetData(DA, "Background Pattern : Cut Patterns", out Types.FillPatternElement cutBackgroundPattern)) return;
      if (!Params.TryGetData(DA, "Background Color : Cut Patterns", out System.Drawing.Color? cutBackgroundPatternColor)) return;

      Params.TrySetData
      (
        DA, "Settings", () =>
        {
          var settings = new ARDB.OverrideGraphicSettings();

          settings.SetHalftone(halftone.GetValueOrDefault(false));

          settings.SetProjectionLinePatternId(projectionLinePattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetProjectionLineColor(projectionLineColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());
          settings.SetProjectionLineWeight(projectionLineWeight.GetValueOrDefault(ARDB.OverrideGraphicSettings.InvalidPenNumber));

#if REVIT_2019
          settings.SetSurfaceForegroundPatternVisible(surfaceForegroundPatternVisible.GetValueOrDefault(true));
          settings.SetSurfaceForegroundPatternId(surfaceForegroundPattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetSurfaceForegroundPatternColor(surfaceForegroundPatternColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());

          settings.SetSurfaceBackgroundPatternVisible(surfaceBackgroundPatternVisible.GetValueOrDefault(true));
          settings.SetSurfaceBackgroundPatternId(surfaceBackgroundPattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetSurfaceBackgroundPatternColor(surfaceBackgroundPatternColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());
#else
          settings.SetProjectionFillPatternVisible(surfaceForegroundPatternVisible.GetValueOrDefault(true));
          settings.SetProjectionFillPatternId(surfaceForegroundPattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetProjectionFillColor(surfaceForegroundPatternColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());
#endif

          settings.SetSurfaceTransparency((int) Rhino.RhinoMath.Clamp(surfaceTransparency.GetValueOrDefault(0.0) * 100, 0, 100));

          settings.SetCutLinePatternId(cutLinePattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetCutLineColor(cutLineColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());
          settings.SetCutLineWeight(cutLineWeight.GetValueOrDefault(ARDB.OverrideGraphicSettings.InvalidPenNumber));

#if REVIT_2019
          settings.SetCutForegroundPatternVisible(cutForegroundPatternVisible.GetValueOrDefault(true));
          settings.SetCutForegroundPatternId(cutForegroundPattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetCutForegroundPatternColor(cutForegroundPatternColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());

          settings.SetCutBackgroundPatternVisible(cutBackgroundPatternVisible.GetValueOrDefault(true));
          settings.SetCutBackgroundPatternId(cutBackgroundPattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetCutBackgroundPatternColor(cutBackgroundPatternColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());
#else
          settings.SetCutFillPatternVisible(cutForegroundPatternVisible.GetValueOrDefault(true));
          settings.SetCutFillPatternId(cutForegroundPattern?.Id ?? ElementIdExtension.InvalidElementId);
          settings.SetCutFillColor(cutForegroundPatternColor.GetValueOrDefault(System.Drawing.Color.Empty).ToColor());
#endif

          return new Types.OverrideGraphicSettings(doc.Value, settings);
        }
      );
    }
  }

  [ComponentVersion(introduced: "1.11")]
  public class CategoryGraphicOverrides : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("D296F72F-E6A8-46E0-9337-A43BA79836E6");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "O";

    public CategoryGraphicOverrides() : base
    (
      name: "Category Graphic Overrides",
      nickname: "CG-Overrides",
      description: "Get-Set category graphics overrides on the specified View",
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
          Description = "View to query element visibility",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Category to access graphics overrides state",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element Hidden state",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Settings",
          NickName = "S",
          Description = "Element graphic settings",
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
          Description = "View to query element visibility",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Category to access graphics overrides state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element Hidden state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Settings",
          NickName = "S",
          Description = "Element graphic settings state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.GetDataList(DA, "Category", out IList<Types.Category> categories)) return;
      else Params.TrySetDataList(DA, "Category", () => categories);

      if (Params.GetDataList(DA, "Hidden", out IList<bool?> hidden) && hidden.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          var categoriesToHide = new HashSet<ARDB.ElementId>(categories.Count);
          var categoriesToUnhide = new HashSet<ARDB.ElementId>(categories.Count);

          foreach (var pair in categories.ZipOrLast(hidden, (Category, Hidden) => (Category, Hidden)))
          {
            if (!pair.Hidden.HasValue) continue;
            if (!view.Document.IsEquivalent(pair.Category?.Document)) continue;
            if (pair.Category?.IsValid != true) continue;
            if (!view.Value.CanCategoryBeHidden(pair.Category.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Category '{pair.Category.Nomen}' can not be hidden on view '{view.Value.Title}'.");
              continue;
            }

            if (pair.Hidden.Value)
            {
              categoriesToUnhide.Remove(pair.Category.Id);
              categoriesToHide.Add(pair.Category.Id);
            }
            else
            {
              categoriesToHide.Remove(pair.Category.Id);
              categoriesToUnhide.Add(pair.Category.Id);
            }
          }

          StartTransaction(view.Document);

          foreach(var categoryId in categoriesToHide)
            view.Value.SetCategoryHidden(categoryId, true);

          foreach (var categoryId in categoriesToUnhide)
            view.Value.SetCategoryHidden(categoryId, false);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Hidden", () => categories.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && view.Value is ARDB.View viewValue && x.Id is ARDB.ElementId categoryId ?
               viewValue.GetCategoryHidden(categoryId) :
               default
        )
      );

      if (Params.GetDataList(DA, "Settings", out IList<Types.OverrideGraphicSettings> settings) && settings.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          StartTransaction(view.Document);

          foreach (var pair in categories.ZipOrLast(settings, (Category, Settings) => (Category, Settings)))
          {
            if (pair.Settings?.Value is null) continue;
            if (!view.Document.IsEquivalent(pair.Category?.Document)) continue;
            if (pair.Category?.IsValid != true) continue;
            if (!view.Value.CanCategoryBeHidden(pair.Category.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed for Category '{pair.Category.Nomen}' on view '{view.Value.Title}'.");
              continue;
            }

            view.Value.SetCategoryOverrides(pair.Category.Id, pair.Settings.Value);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Settings", () => categories.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x?.Id is ARDB.ElementId categoryId &&
               view.Value.GetCategoryOverrides(categoryId) is ARDB.OverrideGraphicSettings overrideSettings ?
               new Types.OverrideGraphicSettings(x.Document, overrideSettings) :
               default
        )
      );
    }
  }

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
          Name = "Settings",
          NickName = "S",
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
          Description = "View to query filter graphics overrides",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FilterElement()
        {
          Name = "Filter",
          NickName = "F",
          Description = "Filter to access graphics overrides state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
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
          Name = "Settings",
          NickName = "S",
          Description = "Filter graphic overrides state",
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
            if (!view.Value.GetFilters().Contains(pair.Filter.Id))
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
               default
        )
      );

      if (Params.GetDataList(DA, "Settings", out IList<Types.OverrideGraphicSettings> settings) && settings.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          StartTransaction(view.Document);

          foreach (var pair in filters.ZipOrLast(settings, (Filter, Settings) => (Filter, Settings)))
          {
            if (pair.Settings?.Value is null) continue;
            if (!view.Document.IsEquivalent(pair.Filter?.Document)) continue;
            if (pair.Filter?.IsValid != true) continue;
            if (!view.Value.GetFilters().Contains(pair.Filter.Id))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Filter '{pair.Filter.Nomen}' is not applied to view '{view.Value.Title}'.");
              continue;
            }

            // Reset filter visibility here to force Revit redraw using this new settings.
            var visibility = view.Value.GetFilterVisibility(pair.Filter.Id);
            if (visibility) view.Value.SetFilterVisibility(pair.Filter.Id, false);
            view.Value.SetFilterOverrides(pair.Filter.Id, pair.Settings.Value);
            if (visibility) view.Value.SetFilterVisibility(pair.Filter.Id, true);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Settings", () => filters.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x?.Id is ARDB.ElementId filterId && view.Value.GetFilters().Contains(filterId) &&
               view.Value.GetFilterOverrides(filterId) is ARDB.OverrideGraphicSettings overrideSettings ?
               new Types.OverrideGraphicSettings(x.Document, overrideSettings) :
               default
        )
      );
    }
  }

  [ComponentVersion(introduced: "1.11")]
  public class ElementGraphicOverrides : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("6F5E3619-4299-4FB5-8CAC-2C172A149142");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "O";

    public ElementGraphicOverrides() : base
    (
      name: "Element Graphic Overrides",
      nickname: "EG-Overrides",
      description: "Get-Set element graphics overrides on the specified View",
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
          Description = "View to query element graphics overrides",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access graphics overrides",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element hidden state",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Settings",
          NickName = "S",
          Description = "Element graphic overrides",
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
          Description = "View to query element visibility",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access graphics overrides state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hidden",
          NickName = "H",
          Description = "Element hidden state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings()
        {
          Name = "Settings",
          NickName = "S",
          Description = "Element graphic overrides",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.GetDataList(DA, "Element", out IList<Types.GraphicalElement> elements)) return;
      else Params.TrySetDataList(DA, "Element", () => elements);

      if (Params.GetDataList(DA, "Hidden", out IList<bool?> hidden) && hidden.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          var elementsToHide = new HashSet<ARDB.ElementId>(elements.Count);
          var elementsToUnhide = new HashSet<ARDB.ElementId>(elements.Count);

          foreach (var pair in elements.ZipOrLast(hidden, (Element, Hidden) => (Element, Hidden)))
          {
            if (!pair.Hidden.HasValue) continue;
            if (!view.Document.IsEquivalent(pair.Element?.Document)) continue;
            if (pair.Element?.IsValid != true) continue;
            if (!pair.Element.Value.CanBeHidden(view.Value))
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Category '{pair.Element.Nomen}' can not be hidden on view '{view.Value.Title}'.");
              continue;
            }

            if (pair.Hidden.Value)
            {
              elementsToUnhide.Remove(pair.Element.Id);
              elementsToHide.Add(pair.Element.Id);
            }
            else
            {
              elementsToHide.Remove(pair.Element.Id);
              elementsToUnhide.Add(pair.Element.Id);
            }
          }

          StartTransaction(view.Document);

          if (elementsToHide.Count > 0) view.Value.HideElements(elementsToHide);
          if (elementsToUnhide.Count > 0) view.Value.UnhideElements(elementsToUnhide);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Hidden", () => elements.Select
        (
          x => view.Document.IsEquivalent(x?.Document) ?
               x?.Value?.IsHidden(view.Value) :
               default
        )
      );

      if (Params.GetDataList(DA, "Settings", out IList<Types.OverrideGraphicSettings> settings) && settings.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {

          StartTransaction(view.Document);

          foreach (var pair in elements.ZipOrLast(settings, (Element, Settings) => (Element, Settings)))
          {
            if (pair.Settings?.Value is null) continue;
            if (!view.Document.IsEquivalent(pair.Element?.Document)) continue;
            if (pair.Element?.IsValid != true) continue;
            if (!pair.Element.Value.CanBeHidden(view.Value)) continue;

            view.Value.SetElementOverrides(pair.Element.Id, pair.Settings.Value);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Settings", () => elements.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x?.Id is ARDB.ElementId elementId &&
               view.Value.GetElementOverrides(elementId) is ARDB.OverrideGraphicSettings overrideSettings ?
               new Types.OverrideGraphicSettings(x.Document, overrideSettings) :
               default
        )
      );
    }
  }
}
