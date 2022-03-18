using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.LinePatternElements
{
  public class LinePatternDashes : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("851E51FE-8B35-4F32-BABD-0FB3A1AFEAF4");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

    public LinePatternDashes() : base
    (
      name: "Line Pattern Dashes",
      nickname: "Dashes",
      description: "Get-Set accessor to line patern dashes, spaces and dots.",
      category: "Revit",
      subCategory: "Object Styles"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.LinePatternElement>("Line Pattern", "LP"),
      ParamDefinition.Create<Param_Number>("Dashes", "D", access: GH_ParamAccess.list, optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.LinePatternElement>("Line Pattern", "LP"),
      ParamDefinition.Create<Param_Number>("Dashes", "D", access: GH_ParamAccess.list, relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Line Pattern", out Types.LinePatternElement pattern, x => x.IsValid)) return;

      if (Params.GetDataList(DA, "Dashes", out IList<double> dashes))
      {
        StartTransaction(pattern.Document);

        pattern.Dashes = dashes;
      }

      DA.SetData("Line Pattern", pattern);
      Params.TrySetDataList(DA, "Dashes", () => pattern.Dashes);
    }
  }
}
