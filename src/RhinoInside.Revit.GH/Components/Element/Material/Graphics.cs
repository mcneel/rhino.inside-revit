using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Materials
{
  [ComponentVersion(introduced: "1.0", updated: "1.11")]
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

      ParamDefinition.Create<Param_Boolean>("Use Render Appearance", "URA", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Color>("Color", "C", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Transparency", "T", "Valid value range is [0.0 .. 1.0]", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Shininess", "SH", "Valid value range is [0.0 .. 1.0]", optional: true, relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Number>("Smoothness", "SM", "Valid value range is [0.0 .. 1.0]", optional: true, relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Parameters.FillPatternElement>("Foreground Pattern : Surface Patterns", "FPSP", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Color>("Foreground Color : Surface Patterns", "FCSP", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.FillPatternElement>("Background Pattern : Surface Patterns", "BPSP", optional: true, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.Color>("Background Color : Surface Patterns", "BCSP", optional: true, relevance: ParamRelevance.Secondary),

      ParamDefinition.Create<Parameters.FillPatternElement>("Foreground Pattern : Cut Patterns", "FPCP", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Color>("Foreground Color : Cut Patterns", "FCCP", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.FillPatternElement>("Background Pattern : Cut Patterns", "BPCP", optional: true, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.Color>("Background Color : Cut Patterns", "BCCP", optional: true, relevance: ParamRelevance.Secondary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Material>("Material", "M"),

      ParamDefinition.Create<Param_Boolean>("Use Render Appearance", "R", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Color>("Color", "C", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Transparency", "T", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Number>("Shininess", "SH", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Number>("Smoothness", "SM", relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Parameters.FillPatternElement>("Surface Patterns : Foreground Pattern", "SPFP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Color>("Surface Patterns : Foreground Color", "SPFC", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.FillPatternElement>("Surface Patterns : Background Pattern", "SPBP", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.Color>("Surface Patterns : Background Color", "SPBC", relevance: ParamRelevance.Secondary),

      ParamDefinition.Create<Parameters.FillPatternElement>("Cut Patterns : Foreground Pattern", "CPFP", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Color>("Cut Patterns : Foreground Color", "CPFC", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.FillPatternElement>("Cut Patterns : Background Pattern", "CPBP", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.Color>("Cut Patterns : Background Color", "CPBC", relevance: ParamRelevance.Secondary),
    };

    static readonly (string Key, string Name, string NickName) [] ObsoleteInputs = new (string Key, string Name, string NickName)[]
    {
      // V1.11
      ("Surface Foreground Pattern", "Foreground Pattern : Surface Patterns", "FPSP"),
      ("Surface Foreground Color", "Foreground Color : Surface Patterns", "FCSP"),
      ("Surface Background Pattern", "Background Pattern : Surface Patterns", "BPSP"),
      ("Surface Background Color", "Background Color : Surface Patterns", "BCSP"),
      ("Cut Foreground Pattern", "Foreground Pattern : Cut Patterns", "FPCP"),
      ("Cut Foreground Color", "Foreground Color : Cut Patterns", "FCCP"),
      ("Cut Background Pattern", "Background Pattern : Cut Patterns", "BPCP"),
      ("Cut Background Color", "Background Color : Cut Patterns", "BCCP"),
    };

    static readonly (string Key, string Name, string NickName)[] ObsoleteOutputs = new (string Key, string Name, string NickName)[]
    {
      // V1.11
      ("Surface Foreground Pattern", "Surface Patterns : Foreground Pattern", "SPFP"),
      ("Surface Foreground Color", "Surface Patterns : Foreground Color", "SPFC"),
      ("Surface Background Pattern", "Surface Patterns : Background Pattern", "SPBP"),
      ("Surface Background Color", "Surface Patterns : Background Color", "SPBC"),
      ("Cut Foreground Pattern", "Cut Patterns : Foreground Pattern", "FPCP"),
      ("Cut Foreground Color", "Cut Patterns : Foreground Color", "CPFC"),
      ("Cut Background Pattern", "Cut Patterns : Background Pattern", "CPBP"),
      ("Cut Background Color", "Cut Patterns : Background Color", "CPBC"),
    };

    public override void AddedToDocument(GH_Document document)
    {
      foreach (var input in ObsoleteInputs)
      {
        if (Params.Input<IGH_Param>(input.Key) is IGH_Param param)
        {
          param.Name = input.Name; param.NickName = input.NickName;
        }
      }

      foreach (var output in ObsoleteOutputs)
      {
        if (Params.Output<IGH_Param>(output.Key) is IGH_Param param)
        {
          param.Name = output.Name; param.NickName = output.NickName;
        }
      }

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Material", out Types.Material material, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Use Render Appearance", out bool? appearance);
      update |= Params.GetData(DA, "Color", out System.Drawing.Color? color);
      update |= Params.GetData(DA, "Transparency", out double? transparency);
      update |= Params.GetData(DA, "Shininess", out double? shininess);
      update |= Params.GetData(DA, "Smoothness", out double? smoothness);

      update |= Params.GetData(DA, "Surface Foreground Pattern", out Types.FillPatternElement sfp);
      update |= Params.GetData(DA, "Surface Foreground Color", out System.Drawing.Color? sfc);
      update |= Params.GetData(DA, "Surface Background Pattern", out Types.FillPatternElement sbp);
      update |= Params.GetData(DA, "Surface Background Color", out System.Drawing.Color? sbc);

      update |= Params.GetData(DA, "Cut Foreground Pattern", out Types.FillPatternElement cfp);
      update |= Params.GetData(DA, "Cut Foreground Color", out System.Drawing.Color? cfc);
      update |= Params.GetData(DA, "Cut Background Pattern", out Types.FillPatternElement cbp);
      update |= Params.GetData(DA, "Cut Background Color", out System.Drawing.Color? cbc);

      if (update)
      {
        StartTransaction(material.Document);
        material.UseRenderAppearanceForShading = appearance;
        material.Color = color;
        material.Transparency = transparency;
        material.Shininess = shininess;
        material.Smoothness = smoothness;

        material.SurfaceForegroundPattern = sfp;
        material.SurfaceForegroundPatternColor = sfc;
        material.SurfaceBackgroundPattern = sbp;
        material.SurfaceBackgroundPatternColor = sbc;

        material.CutForegroundPattern = cfp;
        material.CutForegroundPatternColor = cfc;
        material.CutBackgroundPattern = cbp;
        material.CutBackgroundPatternColor = cbc;
      }

      Params.TrySetData(DA, "Material", () => material);

      Params.TrySetData(DA, "Use Render Appearance", () => material.UseRenderAppearanceForShading);
      Params.TrySetData(DA, "Color", () => material.Color);
      Params.TrySetData(DA, "Transparency", () => material.Transparency);
      Params.TrySetData(DA, "Shininess", () => material.Shininess);
      Params.TrySetData(DA, "Smoothness", () => material.Smoothness);

      Params.TrySetData(DA, "Surface Foreground Pattern", () => material.SurfaceForegroundPattern);
      Params.TrySetData(DA, "Surface Foreground Color", () => material.SurfaceForegroundPatternColor);
      Params.TrySetData(DA, "Surface Background Pattern", () => material.SurfaceBackgroundPattern);
      Params.TrySetData(DA, "Surface Background Color", () => material.SurfaceBackgroundPatternColor);

      Params.TrySetData(DA, "Cut Foreground Pattern", () => material.CutForegroundPattern);
      Params.TrySetData(DA, "Cut Foreground Color", () => material.CutForegroundPatternColor);
      Params.TrySetData(DA, "Cut Background Pattern", () => material.CutBackgroundPattern);
      Params.TrySetData(DA, "Cut Background Color", () => material.CutBackgroundPatternColor);
    }
  }
}

