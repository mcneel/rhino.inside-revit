using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using Autodesk.Revit.DB;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.27")]
  public class DimensionText : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("7B229F5E-850B-486A-8B27-DF2FB17B45F1");
    public override GH_Exposure Exposure => GH_Exposure.quinary;
    protected override string IconTag => string.Empty;

    public DimensionText() : base
    (
      name: "Dimension Text",
      nickname: "D-Text",
      description: string.Empty,
      category: "Revit",
      subCategory: "Annotate"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Dimension()
        {
          Name = "Dimension",
          NickName = "D",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Above",
          NickName = "A",
          Optional = true,
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Value",
          NickName = "V",
          Optional = true,
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Below",
          NickName = "B",
          Optional = true,
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Prefix",
          NickName = "P",
          Optional = true,
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Sufix",
          NickName = "S",
          Optional = true,
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Dimension()
        {
          Name = "Dimension",
          NickName = "A",
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Number()
        {
          Name = "Measure",
          NickName = "M",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Above",
          NickName = "A",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Value",
          NickName = "V",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Below",
          NickName = "B",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Prefix",
          NickName = "P",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Sufix",
          NickName = "S",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),

    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Dimension", out Types.Dimension dim, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Dimension", () => dim);

      Params.TryGetDataList(DA, "Above", out IList<string> above);
      Params.TryGetDataList(DA, "Value", out IList<string> value);
      Params.TryGetDataList(DA, "Below", out IList<string> below);
      Params.TryGetDataList(DA, "Prefix", out IList<string> prefix);
      Params.TryGetDataList(DA, "Sufix", out IList<string> suffix);

      if (above is object || value is object || below is object || prefix is object || suffix is object)
      {
        StartTransaction(dim.Document);
        if (dim.Value.NumberOfSegments == 0)
        {
          if (above?.Count > 0) dim.Value.Above = above?.Last() ?? string.Empty;
          if (value?.Count > 0) dim.Value.ValueOverride = value?.Last() ?? string.Empty;
          if (below?.Count > 0) dim.Value.Below = below?.Last() ?? string.Empty;
          if (prefix?.Count > 0) dim.Value.Prefix = prefix?.Last() ?? string.Empty;
          if (suffix?.Count > 0) dim.Value.Suffix = suffix?.Last() ?? string.Empty;
        }
        else
        {
          using (var segments = dim.Value.Segments)
          {
            var empty = Array.Empty<string>();
            foreach (var (d, a, v, b, p, s) in segments.Cast<DimensionSegment>().ZipOrLast(above ?? empty, value ?? empty, below ?? empty, prefix ?? empty, suffix ?? empty))
            {
              if (a is object) d.Above = a;
              if (v is object) d.ValueOverride = v;
              if (b is object) d.Below = b;
              if (p is object) d.Prefix = p;
              if (s is object) d.Suffix = s;
            }
          }
        }
      }

      var factor = dim.Value.DimensionType.StyleType == DimensionStyleType.Angular ? 1.0 : Revit.ModelUnits;
      if (dim.Value.NumberOfSegments == 0)
      {
        Params.TrySetDataList(DA, "Measure", () => new double?[] { dim.Value.Value.HasValue ? dim.Value.Value.Value * factor : default(double?) });
        Params.TrySetDataList(DA, "Above", () => new string[] { dim.Value.Above });
        Params.TrySetDataList(DA, "Value", () => new string[] { dim.Value.ValueOverride });
        Params.TrySetDataList(DA, "Below", () => new string[] { dim.Value.Below });
        Params.TrySetDataList(DA, "Prefix", () => new string[] { dim.Value.Prefix });
        Params.TrySetDataList(DA, "Sufix", () => new string[] { dim.Value.Suffix });
      }
      else
      {
        using (var segments = dim.Value.Segments)
        {
          Params.TrySetDataList(DA, "Measure", () => segments.Cast<DimensionSegment>().Select(x => x.Value.HasValue ? x.Value.Value * factor : default(double?)));
          Params.TrySetDataList(DA, "Above", () => segments.Cast<DimensionSegment>().Select(x => x.Above));
          Params.TrySetDataList(DA, "Value", () => segments.Cast<DimensionSegment>().Select(x => x.ValueOverride));
          Params.TrySetDataList(DA, "Below", () => segments.Cast<DimensionSegment>().Select(x => x.Below));
          Params.TrySetDataList(DA, "Prefix", () => segments.Cast<DimensionSegment>().Select(x => x.Prefix));
          Params.TrySetDataList(DA, "Sufix", () => segments.Cast<DimensionSegment>().Select(x => x.Suffix));
        }
      }
    }
  }
}
