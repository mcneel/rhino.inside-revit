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

      newOverrides.Value.SetSurfaceForegroundPatternVisible(surfaceForegroundPatternVisible ?? overrides?.Value.IsSurfaceForegroundPatternVisible() ?? true);
      surfaceForegroundPattern = doc.GetNamesakeElement(surfaceForegroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetSurfaceForegroundPatternId(surfaceForegroundPattern?.Id ?? overrides?.Value.SurfaceForegroundPatternId() ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetSurfaceForegroundPatternColor(surfaceForegroundPatternColor?.ToColor() ?? overrides?.Value.SurfaceForegroundPatternColor() ?? ARDB.Color.InvalidColorValue);

      newOverrides.Value.SetSurfaceBackgroundPatternVisible(surfaceBackgroundPatternVisible ?? overrides?.Value.IsSurfaceBackgroundPatternVisible() ?? true);
      surfaceBackgroundPattern = doc.GetNamesakeElement(surfaceBackgroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetSurfaceBackgroundPatternId(surfaceBackgroundPattern?.Id ?? overrides?.Value.SurfaceBackgroundPatternId() ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetSurfaceBackgroundPatternColor(surfaceBackgroundPatternColor?.ToColor() ?? overrides?.Value.SurfaceBackgroundPatternColor() ?? ARDB.Color.InvalidColorValue);

      newOverrides.Value.SetSurfaceTransparency(surfaceTransparency.HasValue ? Rhino.RhinoMath.Clamp((int) Math.Round(surfaceTransparency.Value * 100), 0, 100) : overrides?.Value.Transparency ?? 0);

      cutLinePattern = doc.GetNamesakeElement(cutLinePattern) as Types.LinePatternElement;
      newOverrides.Value.SetCutLinePatternId(cutLinePattern?.Id ?? overrides?.Value.CutLinePatternId ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetCutLineColor(cutLineColor?.ToColor() ?? overrides?.Value.CutLineColor ?? ARDB.Color.InvalidColorValue);
      newOverrides.Value.SetCutLineWeight(cutLineWeight ?? overrides?.Value.CutLineWeight ?? ARDB.OverrideGraphicSettings.InvalidPenNumber);

      newOverrides.Value.SetCutForegroundPatternVisible(cutForegroundPatternVisible ?? overrides?.Value.IsCutForegroundPatternVisible() ?? true);
      cutForegroundPattern = doc.GetNamesakeElement(cutForegroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetCutForegroundPatternId(cutForegroundPattern?.Id ?? overrides?.Value.CutForegroundPatternId() ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetCutForegroundPatternColor(cutForegroundPatternColor?.ToColor() ?? overrides?.Value.CutForegroundPatternColor() ?? ARDB.Color.InvalidColorValue);

      newOverrides.Value.SetCutBackgroundPatternVisible(cutBackgroundPatternVisible ?? overrides?.Value.IsCutBackgroundPatternVisible() ?? true);
      cutBackgroundPattern = doc.GetNamesakeElement(cutBackgroundPattern) as Types.FillPatternElement;
      newOverrides.Value.SetCutBackgroundPatternId(cutBackgroundPattern?.Id ?? overrides?.Value.CutBackgroundPatternId() ?? ElementIdExtension.InvalidElementId);
      newOverrides.Value.SetCutBackgroundPatternColor(cutBackgroundPatternColor?.ToColor() ?? overrides?.Value.CutBackgroundPatternColor() ?? ARDB.Color.InvalidColorValue);
      #endregion

      #region Set
      Params.TrySetData(DA, "Document", () => newOverrides.Document);
      Params.TrySetData(DA, "Overrides", () => newOverrides);

      Params.TrySetData(DA, "Detail Level", () => newOverrides.Value.DetailLevel);
      Params.TrySetData(DA, "Halftone", () => newOverrides.Value.Halftone);

      Params.TrySetData(DA, "Projection Lines : Pattern", () => Types.LinePatternElement.FromElementId(newOverrides.Document, newOverrides.Value.ProjectionLinePatternId));
      Params.TrySetData(DA, "Projection Lines : Color", () => newOverrides.Value.ProjectionLineColor.ToColor());
      Params.TrySetData(DA, "Projection Lines : Weight", () => newOverrides.Value.ProjectionLineWeight);

      Params.TrySetData(DA, "Surface Patterns : Foreground Visible", () => newOverrides.Value.IsSurfaceForegroundPatternVisible());
      Params.TrySetData(DA, "Surface Patterns : Foreground Pattern", () => Types.FillPatternElement.FromElementId(newOverrides.Document, newOverrides.Value.SurfaceForegroundPatternId()));
      Params.TrySetData(DA, "Surface Patterns : Foreground Color", () => newOverrides.Value.SurfaceForegroundPatternColor().ToColor());

      Params.TrySetData(DA, "Surface Patterns : Background Visible", () => newOverrides.Value.IsSurfaceBackgroundPatternVisible());
      Params.TrySetData(DA, "Surface Patterns : Background Pattern", () => Types.FillPatternElement.FromElementId(newOverrides.Document, newOverrides.Value.SurfaceBackgroundPatternId()));
      Params.TrySetData(DA, "Surface Patterns : Background Color", () => newOverrides.Value.SurfaceBackgroundPatternColor().ToColor());

      Params.TrySetData(DA, "Surface : Transparency", () => newOverrides.Value.Transparency / 100.0);

      Params.TrySetData(DA, "Cut Lines : Pattern", () => Types.LinePatternElement.FromElementId(newOverrides.Document, newOverrides.Value.CutLinePatternId));
      Params.TrySetData(DA, "Cut Lines : Color", () => newOverrides.Value.CutLineColor.ToColor());
      Params.TrySetData(DA, "Cut Lines : Weight", () => newOverrides.Value.CutLineWeight);

      Params.TrySetData(DA, "Cut Patterns : Foreground Visible", () => newOverrides.Value.IsCutForegroundPatternVisible());
      Params.TrySetData(DA, "Cut Patterns : Foreground Pattern", () => Types.FillPatternElement.FromElementId(newOverrides.Document, newOverrides.Value.CutForegroundPatternId()));
      Params.TrySetData(DA, "Cut Patterns : Foreground Color", () => newOverrides.Value.CutForegroundPatternColor().ToColor());

      Params.TrySetData(DA, "Cut Patterns : Background Visible", () => newOverrides.Value.IsCutBackgroundPatternVisible());
      Params.TrySetData(DA, "Cut Patterns : Background Pattern", () => Types.FillPatternElement.FromElementId(newOverrides.Document, newOverrides.Value.CutBackgroundPatternId()));
      Params.TrySetData(DA, "Cut Patterns : Background Color", () => newOverrides.Value.CutBackgroundPatternColor().ToColor());
      #endregion
    }
  }
}
