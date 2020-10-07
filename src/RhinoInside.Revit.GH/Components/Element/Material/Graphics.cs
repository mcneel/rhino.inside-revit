using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Extensions;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class MaterialGraphics : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("8C5CD6FB-4F48-4F35-B0B8-42B5A3636B5C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "G";

    public MaterialGraphics()
    : base
    (
      "Material Graphics",
      "Graphics",
      "Material Graphics Data.",
      "Revit",
      "Material"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Material>("Material", "M"),

      ParamDefinition.Create<Param_Boolean>("Use Render Appearance", "R", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Color", "C", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Number>("Smoothness", "S", optional: true, relevance: ParamVisibility.Voluntary),

      ParamDefinition.Create<Parameters.FillPatternElement>("Surface Foreground Pattern", "SFP", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Surface Foreground Color", "SFC", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Parameters.FillPatternElement>("Surface Background Pattern", "SBP", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Surface Background Color", "SBC", optional: true, relevance: ParamVisibility.Default),

      ParamDefinition.Create<Parameters.FillPatternElement>("Cut Foreground Pattern", "SFP", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Cut Foreground Color", "SFC", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Parameters.FillPatternElement>("Cut Background Pattern", "SBP", optional: true, relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Cut Background Color", "SBC", optional: true, relevance: ParamVisibility.Default),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Material>("Material", "M"),

      ParamDefinition.Create<Param_Boolean>("Use Render Appearance", "R", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Color", "C", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Number>("Smoothness", "S", relevance: ParamVisibility.Voluntary),

      ParamDefinition.Create<Parameters.FillPatternElement>("Surface Foreground Pattern", "SFP", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Surface Foreground Color", "SFC", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Parameters.FillPatternElement>("Surface Background Pattern", "SBP", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Surface Background Color", "SBC", relevance: ParamVisibility.Default),

      ParamDefinition.Create<Parameters.FillPatternElement>("Cut Foreground Pattern", "SFP", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Cut Foreground Color", "SFC", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Parameters.FillPatternElement>("Cut Background Pattern", "SBP", relevance: ParamVisibility.Default),
      ParamDefinition.Create<Param_Colour>("Cut Background Color", "SBC", relevance: ParamVisibility.Default),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!DA.TryGetData(Params.Input, "Material", out Types.Material material))
        return;

      bool update = false;
      update |= DA.TryGetData(Params.Input, "Use Render Appearance", out bool appearance);
      update |= DA.TryGetData(Params.Input, "Color", out System.Drawing.Color color);
      update |= DA.TryGetData(Params.Input, "Smoothness", out double smootness);

      update |= DA.TryGetData(Params.Input, "Surface Foreground Pattern", out Types.FillPatternElement sfp);
      update |= DA.TryGetData(Params.Input, "Surface Foreground Color", out System.Drawing.Color sfc);
      update |= DA.TryGetData(Params.Input, "Surface Background Pattern", out Types.FillPatternElement sbp);
      update |= DA.TryGetData(Params.Input, "Surface Background Color", out System.Drawing.Color sbc);

      update |= DA.TryGetData(Params.Input, "Cut Foreground Pattern", out Types.FillPatternElement cfp);
      update |= DA.TryGetData(Params.Input, "Cut Foreground Color", out System.Drawing.Color cfc);
      update |= DA.TryGetData(Params.Input, "Cut Background Pattern", out Types.FillPatternElement cbp);
      update |= DA.TryGetData(Params.Input, "Cut Background Color", out System.Drawing.Color cbc);

      if (update)
      {
        StartTransaction(material.Document);
        material.UseRenderAppearanceForShading = appearance;
        material.Color = color;
        material.Smoothness = smootness;

        material.SurfaceForegroundPattern = sfp;
        material.SurfaceForegroundPatternColor = sfc;
        material.SurfaceBackgroundPattern = sbp;
        material.SurfaceBackgroundPatternColor = sbc;

        material.CutForegroundPattern = cfp;
        material.CutForegroundPatternColor = cfc;
        material.CutBackgroundPattern = cbp;
        material.CutBackgroundPatternColor = cbc;
      }

      DA.TrySetData(Params.Output, "Material", () => material);

      DA.TrySetData(Params.Output, "Use Render Appearance", () => material.UseRenderAppearanceForShading);
      DA.TrySetData(Params.Output, "Color", () => material.Color);
      DA.TrySetData(Params.Output, "Smoothness", () => material.Smoothness);

      DA.TrySetData(Params.Output, "Surface Foreground Pattern", () => material.SurfaceForegroundPattern);
      DA.TrySetData(Params.Output, "Surface Foreground Color", () => material.SurfaceForegroundPatternColor);
      DA.TrySetData(Params.Output, "Surface Background Pattern", () => material.SurfaceBackgroundPattern);
      DA.TrySetData(Params.Output, "Surface Background Color", () => material.SurfaceBackgroundPatternColor);

      DA.TrySetData(Params.Output, "Cut Foreground Pattern", () => material.CutForegroundPattern);
      DA.TrySetData(Params.Output, "Cut Foreground Color", () => material.CutForegroundPatternColor);
      DA.TrySetData(Params.Output, "Cut Background Pattern", () => material.CutBackgroundPattern);
      DA.TrySetData(Params.Output, "Cut Background Color", () => material.CutBackgroundPatternColor);
    }
  }
}

