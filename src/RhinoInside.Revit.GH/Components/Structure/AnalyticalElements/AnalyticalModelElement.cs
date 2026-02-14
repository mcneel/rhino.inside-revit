using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.36")]
  public class AnalyticalModelElement : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("{6784ABA1-9109-45E7-8DBE-ABF48D48AD23}");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => string.Empty;

    public AnalyticalModelElement() : base
    (
      name: "Analytical Model Element",
      nickname: "A-Element",
      description: "Get-Set association between analytical and model elements",
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
          Description = $"Output {_AnalyticalElement_}",
        }
      ),
#if REVIT_2023
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _ModelElement_,
          NickName = "ME",
          Description = "Model element",
          Optional = true
        }, ParamRelevance.Primary
      ),
#endif
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
#if REVIT_2023
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = _AnalyticalElement_,
          NickName = "AE",
          Description = $"Output {_AnalyticalElement_}",
        }, ParamRelevance.Primary
      ),
#endif
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _ModelElement_,
          NickName = "ME",
          Description = "Model element",
        }
      ),
    };

    const string _AnalyticalElement_ = "Analytical Element";
    const string _ModelElement_ = "Model Element";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.GetData(DA, _AnalyticalElement_, out Types.AnalyticalElement analytical, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, _ModelElement_, out Types.GraphicalElement element, x => x.IsValid)) return;

      if (element is object)
      {
        if (!element.IsPhysicalElement)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input element is not a model element.");
          return;
        }

        if (!element.Structural)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input model element is not structural.");
          return;
        }

        StartTransaction(element.Document);
        analytical.PhysicalElement = element;
      }

      Params.TrySetData(DA, _AnalyticalElement_, () => analytical);
      Params.TrySetData(DA, _ModelElement_, () => analytical.PhysicalElement);
    }
  }
}

