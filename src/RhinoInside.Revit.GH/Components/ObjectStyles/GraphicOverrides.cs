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
  public class GraphicOverrides : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("BFD4A970-CE90-47D3-B196-103E0DDCE977");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => "GO";

    public GraphicOverrides() : base
    (
      name: "Graphic Overrides",
      nickname: "G-Overrides",
      description: "Get-Set element graphics overrides on the specified View",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document() { Optional = true }, ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings() { Name = "Overrides", NickName = "O", Description = "Graphic Overrides", Optional = true}, ParamRelevance.Primary
      ),
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
        new Parameters.Color() { Name = "Color : Projection Lines", NickName = "CPL", Description = "Element projection line color", Optional = true }, ParamRelevance.Primary
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
        new Parameters.Color() { Name = "Foreground Color : Surface Patterns", NickName = "FCSP", Description = "Element foreground surface color", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Background Visible : Surface Patterns", NickName = "BVSP", Description = "Element background surface patterns visibility", Optional = true }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Background Pattern : Surface Patterns", NickName = "BPSP", Description = "Element background surface pattern", Optional = true }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Color() { Name = "Background Color : Surface Patterns", NickName = "BCSP", Description = "Element background surface color", Optional = true }, ParamRelevance.Secondary
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
        new Parameters.Color() { Name = "Color : Cut Lines", NickName = "CCL", Description = "Element cut line color", Optional = true }, ParamRelevance.Primary
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
        new Parameters.Color() { Name = "Foreground Color : Cut Patterns", NickName = "FCCP", Description = "Element foreground cut color", Optional = true }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Background Visible : Cut Patterns", NickName = "BVCP", Description = "Element background cut patterns visibility", Optional = true }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Background Pattern : Cut Patterns", NickName = "BPCP", Description = "Element background cut pattern", Optional = true }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Color() { Name = "Background Color : Cut Patterns", NickName = "BCCP", Description = "Element background cut color", Optional = true }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ViewDetailLevel>() { Name = "Detail Level", NickName = "DL", Description = "Element Detail Level", Optional = true }, ParamRelevance.Tertiary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      new ParamDefinition
      (
        new Parameters.OverrideGraphicSettings() { Name = "Overrides", NickName = "O", Description = "Graphic Overrides", }
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Halftone", NickName = "H", Description = "Element Halftone state" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LinePatternElement() { Name = "Projection Lines : Pattern", NickName = "PLP", Description = "Element projection line pattern" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Color() { Name = "Projection Lines : Color", NickName = "PLC", Description = "Element projection line color" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer() { Name = "Projection Lines : Weight", NickName = "PLW", Description = "Element projection line weight" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Surface Patterns : Foreground Visible", NickName = "SPFV", Description = "Element foreground surface patterns visibility" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Surface Patterns : Foreground Pattern", NickName = "SPFP", Description = "Element foreground surface pattern" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour() { Name = "Surface Patterns : Foreground Color", NickName = "SPFC", Description = "Element foreground surface color" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Surface Patterns : Background Visible", NickName = "SPBV", Description = "Element background surface patterns visibility" }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Surface Patterns : Background Pattern", NickName = "SPBP", Description = "Element background surface pattern" }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Color() { Name = "Surface Patterns : Background Color", NickName = "SPBC", Description = "Element background surface color" }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Number() { Name = "Surface : Transparency", NickName = "ST", Description = "Element surface transparency [0.0 .. 1.0]" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.LinePatternElement() { Name = "Cut Lines : Pattern", NickName = "CLP", Description = "Element cut line pattern" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Color() { Name = "Cut Lines : Color", NickName = "CLC", Description = "Element cut line color" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer() { Name = "Cut Lines : Weight", NickName = "CLW", Description = "Element cut line weight" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Cut Patterns : Foreground Visible", NickName = "CPFV", Description = "Element foreground cut patterns visibility" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Cut Patterns : Foreground Pattern", NickName = "CPFP", Description = "Element foreground cut pattern" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Color() { Name = "Cut Patterns : Foreground Color", NickName = "CPFC", Description = "Element foreground cut color" }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean() { Name = "Cut Patterns : Background Visible", NickName = "CPBV", Description = "Element background cut patterns visibility" }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.FillPatternElement() { Name = "Cut Patterns : Background Pattern", NickName = "CPBP", Description = "Element background cut pattern" }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Color() { Name = "Cut Patterns : Background Color", NickName = "CPBC", Description = "Element background cut color" }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.ViewDetailLevel>() { Name = "Detail Level", NickName = "DL", Description = "Element Detail Level" }, ParamRelevance.Tertiary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      #region Get
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Overrides", out Types.OverrideGraphicSettings overrides)) return;

      if (!Params.TryGetData(DA, "Detail Level", out ARDB.ViewDetailLevel? detailLevel)) return;
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

      if (!Params.TryGetData(DA, "Transparency : Surface", out double? surfaceTransparency)) return;

      if (!Params.TryGetData(DA, "Pattern : Cut Lines", out Types.LinePatternElement cutLinePattern)) return;
      if (!Params.TryGetData(DA, "Color : Cut Lines", out System.Drawing.Color? cutLineColor)) return;
      if (!Params.TryGetData(DA, "Weight : Cut Lines", out int? cutLineWeight)) return;

      if (!Params.TryGetData(DA, "Foreground Visible : Cut Patterns", out bool? cutForegroundPatternVisible)) return;
      if (!Params.TryGetData(DA, "Foreground Pattern : Cut Patterns", out Types.FillPatternElement cutForegroundPattern)) return;
      if (!Params.TryGetData(DA, "Foreground Color : Cut Patterns", out System.Drawing.Color? cutForegroundPatternColor)) return;

      if (!Params.TryGetData(DA, "Background Visible : Cut Patterns", out bool? cutBackgroundPatternVisible)) return;
      if (!Params.TryGetData(DA, "Background Pattern : Cut Patterns", out Types.FillPatternElement cutBackgroundPattern)) return;
      if (!Params.TryGetData(DA, "Background Color : Cut Patterns", out System.Drawing.Color? cutBackgroundPatternColor)) return;
      #endregion

      #region Merge
      var newOverrides = new Types.OverrideGraphicSettings(doc.Value);

      newOverrides.Value.SetDetailLevel(detailLevel ?? overrides?.Value.DetailLevel ?? default);
      newOverrides.Value.SetHalftone(halftone ?? overrides?.Value.Halftone ?? default);

      newOverrides.Value.SetProjectionLinePatternId(doc.GetNamesakeElement(projectionLinePattern)?.Id ?? overrides?.Value.ProjectionLinePatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetProjectionLineColor(projectionLineColor?.ToColor() ?? overrides?.Value.ProjectionLineColor ?? ARDB.Color.InvalidColorValue);
      newOverrides.Value.SetProjectionLineWeight(projectionLineWeight ?? overrides?.Value.ProjectionLineWeight ?? ARDB.OverrideGraphicSettings.InvalidPenNumber);

#if REVIT_2019
      newOverrides.Value.SetSurfaceForegroundPatternVisible(surfaceForegroundPatternVisible ?? overrides?.Value.IsSurfaceForegroundPatternVisible ?? true);
      surfaceForegroundPattern = doc.GetNamesakeElement(surfaceForegroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetSurfaceForegroundPatternId(surfaceForegroundPattern?.Id ?? overrides?.Value.SurfaceForegroundPatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetSurfaceForegroundPatternColor(surfaceForegroundPatternColor?.ToColor() ?? overrides?.Value.SurfaceForegroundPatternColor ?? ARDB.Color.InvalidColorValue);

      newOverrides.Value.SetSurfaceBackgroundPatternVisible(surfaceBackgroundPatternVisible ?? overrides?.Value.IsSurfaceBackgroundPatternVisible ?? true);
      surfaceBackgroundPattern = doc.GetNamesakeElement(surfaceBackgroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetSurfaceBackgroundPatternId(surfaceBackgroundPattern?.Id ?? overrides?.Value.SurfaceBackgroundPatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetSurfaceBackgroundPatternColor(surfaceBackgroundPatternColor?.ToColor() ?? overrides?.Value.SurfaceBackgroundPatternColor ?? ARDB.Color.InvalidColorValue);
#else
      newOverrides.Value.SetProjectionFillPatternVisible(surfaceForegroundPatternVisible ?? overrides?.Value.IsProjectionFillPatternVisible ?? true);
      surfaceForegroundPattern = doc.GetNamesakeElement(surfaceForegroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetProjectionFillPatternId(surfaceForegroundPattern?.Id ?? overrides?.Value.ProjectionFillPatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetProjectionFillColor(surfaceForegroundPatternColor?.ToColor() ?? overrides?.Value.ProjectionFillColor ?? ARDB.Color.InvalidColorValue);
#endif

      newOverrides.Value.SetSurfaceTransparency(surfaceTransparency.HasValue ? Rhino.RhinoMath.Clamp((int) Math.Round(surfaceTransparency.Value * 100), 0, 100) : overrides?.Value.Transparency ?? 0);

      cutLinePattern = doc.GetNamesakeElement(cutLinePattern) as Types.LinePatternElement;
      newOverrides.Value.SetCutLinePatternId(cutLinePattern?.Id ?? overrides?.Value.CutLinePatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetCutLineColor(cutLineColor?.ToColor() ?? overrides?.Value.CutLineColor ?? ARDB.Color.InvalidColorValue);
      newOverrides.Value.SetCutLineWeight(cutLineWeight ?? overrides?.Value.CutLineWeight ?? ARDB.OverrideGraphicSettings.InvalidPenNumber);

#if REVIT_2019
      newOverrides.Value.SetCutForegroundPatternVisible(cutForegroundPatternVisible ?? overrides?.Value.IsCutForegroundPatternVisible ?? true);
      cutForegroundPattern = doc.GetNamesakeElement(cutForegroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetCutForegroundPatternId(cutForegroundPattern?.Id ?? overrides?.Value.CutForegroundPatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetCutForegroundPatternColor(cutForegroundPatternColor?.ToColor() ?? overrides?.Value.CutForegroundPatternColor ?? ARDB.Color.InvalidColorValue);

      newOverrides.Value.SetCutBackgroundPatternVisible(cutBackgroundPatternVisible ?? overrides?.Value.IsCutBackgroundPatternVisible ?? true);
      cutBackgroundPattern = doc.GetNamesakeElement(cutBackgroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetCutBackgroundPatternId(cutBackgroundPattern?.Id ?? overrides?.Value.CutBackgroundPatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetCutBackgroundPatternColor(cutBackgroundPatternColor?.ToColor() ?? overrides?.Value.CutBackgroundPatternColor ?? ARDB.Color.InvalidColorValue);
#else
      newOverrides.Value.SetCutFillPatternVisible(cutForegroundPatternVisible ?? overrides?.Value.IsCutFillPatternVisible ?? true);
      cutForegroundPattern = doc.GetNamesakeElement(cutForegroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetCutFillPatternId(cutForegroundPattern?.Id ?? overrides?.Value.CutFillPatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetCutFillColor(cutForegroundPatternColor?.ToColor() ?? overrides?.Value.CutFillColor ?? ARDB.Color.InvalidColorValue);
#endif
      #endregion

      #region Set
      Params.TrySetData(DA, "Document", () => newOverrides.Document);
      Params.TrySetData(DA, "Overrides", () => newOverrides);

      Params.TrySetData(DA, "Detail Level", () => newOverrides.Value.DetailLevel);
      Params.TrySetData(DA, "Halftone", () => newOverrides.Value.Halftone);

      Params.TrySetData(DA, "Projection Lines : Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.ProjectionLinePatternId));
      Params.TrySetData(DA, "Projection Lines : Color", () => newOverrides.Value.ProjectionLineColor.ToColor());
      Params.TrySetData(DA, "Projection Lines : Weight", () => newOverrides.Value.ProjectionLineWeight);

#if REVIT_2019
      Params.TrySetData(DA, "Surface Patterns : Foreground Visible", () => newOverrides.Value.IsSurfaceForegroundPatternVisible);
      Params.TrySetData(DA, "Surface Patterns : Foreground Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.SurfaceForegroundPatternId));
      Params.TrySetData(DA, "Surface Patterns : Foreground Color", () => newOverrides.Value.SurfaceForegroundPatternColor.ToColor());

      Params.TrySetData(DA, "Surface Patterns : Background Visible", () => newOverrides.Value.IsSurfaceBackgroundPatternVisible);
      Params.TrySetData(DA, "Surface Patterns : Background Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.SurfaceBackgroundPatternId));
      Params.TrySetData(DA, "Surface Patterns : Background Color", () => newOverrides.Value.SurfaceBackgroundPatternColor.ToColor());
#else
      Params.TrySetData(DA, "Surface Patterns : Foreground Visible", () => newOverrides.Value.IsProjectionFillPatternVisible);
      Params.TrySetData(DA, "Surface Patterns : Foreground Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.ProjectionFillPatternId));
      Params.TrySetData(DA, "Surface Patterns : Foreground Color", () => newOverrides.Value.ProjectionFillColor.ToColor());
#endif

      Params.TrySetData(DA, "Surface : Transparency", () => newOverrides.Value.Transparency / 100.0);

      Params.TrySetData(DA, "Cut Lines : Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.CutLinePatternId));
      Params.TrySetData(DA, "Cut Lines : Color", () => newOverrides.Value.CutLineColor.ToColor());
      Params.TrySetData(DA, "Cut Lines : Weight", () => newOverrides.Value.CutLineWeight);

#if REVIT_2019
      Params.TrySetData(DA, "Cut Patterns : Foreground Visible", () => newOverrides.Value.IsCutForegroundPatternVisible);
      Params.TrySetData(DA, "Cut Patterns : Foreground Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.CutForegroundPatternId));
      Params.TrySetData(DA, "Cut Patterns : Foreground Color", () => newOverrides.Value.CutForegroundPatternColor.ToColor());

      Params.TrySetData(DA, "Cut Patterns : Background Visible", () => newOverrides.Value.IsCutBackgroundPatternVisible);
      Params.TrySetData(DA, "Cut Patterns : Background Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.CutBackgroundPatternId));
      Params.TrySetData(DA, "Cut Patterns : Background Color", () => newOverrides.Value.CutBackgroundPatternColor.ToColor());
#else
      Params.TrySetData(DA, "Cut Patterns : Foreground Visible", () => newOverrides.Value.IsCutFillPatternVisible);
      Params.TrySetData(DA, "Cut Patterns : Foreground Pattern", () => Types.Element.FromElementId(newOverrides.Document, newOverrides.Value.CutFillPatternId));
      Params.TrySetData(DA, "Cut Patterns : Foreground Color", () => newOverrides.Value.CutFillColor.ToColor());
#endif
      #endregion
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
          Description = "View to query category graphics overrides",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Category to access graphics overrides",
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
          Name = "Overrides",
          NickName = "O",
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
          Description = "View to query category graphic overrides",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Category()
        {
          Name = "Category",
          NickName = "C",
          Description = "Category to access graphic overrides",
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
          Name = "Overrides",
          NickName = "O",
          Description = "Element graphic overrides",
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
          x => view.Document.IsEquivalent(x?.Document) && x.Id is ARDB.ElementId categoryId ?
               view.Value.GetCategoryHidden(categoryId) :
               default(bool?)
        )
      );

      if (Params.GetDataList(DA, "Overrides", out IList<Types.OverrideGraphicSettings> settings) && settings.Count > 0)
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
        DA, "Overrides", () => categories.Select
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
               default(bool?)
        )
      );

      if (Params.GetDataList(DA, "Overrides", out IList<Types.OverrideGraphicSettings> settings) && settings.Count > 0)
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
          Description = "View to query element graphic overrides",
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access graphic overrides",
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
          Name = "Overrides",
          NickName = "O",
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
          Description = "View to query element graphic overrides",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access graphics overrides",
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
          Name = "Overrides",
          NickName = "O",
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
               default(bool?)
        )
      );

      if (Params.GetDataList(DA, "Overrides", out IList<Types.OverrideGraphicSettings> settings) && settings.Count > 0)
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
        DA, "Overrides", () => elements.Select
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
