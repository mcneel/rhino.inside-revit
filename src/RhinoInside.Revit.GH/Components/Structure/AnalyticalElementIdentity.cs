using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.27")]
  public class AnalyticalElementIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("6844CF5E-8015-457E-AC7E-0E58C6B80A82");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => "ID";

    public AnalyticalElementIdentity() : base
    (
      name: "Analytical Element Identity",
      nickname: "AE-Identity",
      description: "Analytical Element Data.",
      category: "Revit",
      subCategory: "Structure"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = _AnalyticalElement_,
          NickName = "AE",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = _StructuralRole_,
          NickName = "SR",
          Description = "Structural Role to apply to the analytical element",
          Optional = true,
        },
#if REVIT_2023
        ParamRelevance.Primary
#else
        ParamRelevance.Occasional
#endif
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyzeAs>
        {
          Name = _AnalyzeAs_,
          NickName = "AS",
          Description = "Structural analysis function to apply to the analytical element",
          Optional = true,
        }, ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = _AnalyticalElement_,
          NickName = "AE",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = _StructuralRole_,
          NickName = "SR",
          Description = "Structural Role applied to the analytical element",
        },
#if REVIT_2023
        ParamRelevance.Primary
#else
        ParamRelevance.Occasional
#endif
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyzeAs>
        {
          Name = _AnalyzeAs_,
          NickName = "AS",
          Description = "Structural analysis function to applied to the analytical element",
        }, ParamRelevance.Primary
      ),
    };

    const string _AnalyticalElement_ = "Analytical Element";
    const string _StructuralRole_ = "Structural Role";
    const string _AnalyzeAs_ = "Analyze As";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.GetData(DA, _AnalyticalElement_, out Types.AnalyticalElement element)) return;
      else Params.TrySetData(DA, _AnalyticalElement_, () => element);

      if (!Params.TryGetData(DA, _StructuralRole_, out Types.AnalyticalStructuralRole structuralRole)) return;
      if (!Params.TryGetData(DA, _AnalyzeAs_, out Types.AnalyzeAs analyzeAs)) return;

      if (structuralRole is object)
      {
        StartTransaction(element.Document);
        element.StructuralRole = structuralRole.Value;
      }
      Params.TrySetData(DA, _StructuralRole_, () => element.StructuralRole);

      if (analyzeAs is object)
      {
        StartTransaction(element.Document);
        element.AnalyzeAs = analyzeAs.Value;
      }
      Params.TrySetData(DA, _AnalyzeAs_, () => element.AnalyzeAs);
    }
  }
}

