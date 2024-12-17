using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.27"), ComponentRevitAPIVersion(min: "2023.0")]
  public class AnalyticalElementStructuralRole : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("6844CF5E-8015-457E-AC7E-0E58C6B80A82");
#if REVIT_2023
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
#else
    public override GH_Exposure Exposure => GH_Exposure.hidden;
#endif
    public AnalyticalElementStructuralRole() : base
    (
      name: "Element Structural Role",
      nickname: "AE-Role",
      description: "Given an analytical element from the Revit document, this component sets its structural role",
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
          Name = "Analytical Element",
          NickName = "AE",
          Description = "Analytical element to set the structural role",
        }
      ),
#if REVIT_2023
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = "Structural Role",
          NickName = "SR",
          Description = "Structural Role to apply to the analytical element",
          Optional = true,
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyzeAs>
        {
          Name = "Analyze As",
          NickName = "AS",
          Description = "Structural analysis function to apply to the analytical element",
          Optional = true,
        }, ParamRelevance.Primary
      ),
#endif
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = _AnalyticalElement_,
          NickName = _AnalyticalElement_.Substring(0, 1),
          Description = $"Output {_AnalyticalElement_}",
        }
      ),
#if REVIT_2023
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyticalStructuralRole>
        {
          Name = "Structural Role",
          NickName = "SR",
          Description = "Structural Role applied to the analytical element",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.AnalyzeAs>
        {
          Name = "Analyze As",
          NickName = "AS",
          Description = "Structural analysis function to applied to the analytical element",
        }, ParamRelevance.Primary
      ),
#endif
    };

    const string _AnalyticalElement_ = "Analytical Element";
    const string _StructuralRole_ = "Structural Role";
    const string _AnalyzeAs_ = "Analyze As";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.GetData(DA, _AnalyticalElement_, out Types.AnalyticalElement element)) return;
      else Params.TrySetData(DA, _AnalyticalElement_, () => element);

#if REVIT_2023
      if (!Params.TryGetData(DA, _StructuralRole_, out Types.AnalyticalStructuralRole structuralRole)) return;
      if (!Params.TryGetData(DA, _AnalyzeAs_, out Types.AnalyzeAs analyzeAs)) return;

      if (structuralRole is object)
      {
        StartTransaction(element.Document);
        element.Value.StructuralRole = structuralRole.Value;
      }

      if (analyzeAs is object)
      {
        StartTransaction(element.Document);
        element.Value.AnalyzeAs = analyzeAs.Value;
      }

      Params.TrySetData(DA, _StructuralRole_, () => element.Value.StructuralRole);
      Params.TrySetData(DA, _AnalyzeAs_, () => element.Value.AnalyzeAs);
#endif
    }
  }
}

