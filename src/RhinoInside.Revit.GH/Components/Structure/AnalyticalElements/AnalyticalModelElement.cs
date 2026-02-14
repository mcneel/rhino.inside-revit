using System;
using System.Collections.Generic;
using System.Linq;
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
          Name = _ModelElements_,
          NickName = "ME",
          Description = "Model elements associated",
          Access = GH_ParamAccess.list,
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
          Name = _ModelElements_,
          NickName = "ME",
          Description = "Model elements associated",
          Access = GH_ParamAccess.list
        }
      ),
    };

    const string _AnalyticalElement_ = "Analytical Element";
    const string _ModelElements_ = "Model Elements";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.GetData(DA, _AnalyticalElement_, out Types.AnalyticalElement analytical, x => x.IsValid)) return;
      if (!Params.TryGetDataList(DA, _ModelElements_, out IList<Types.GraphicalElement> elements)) return;

      if (elements is object)
      {
        foreach (var element in elements)
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
        }

        StartTransaction(analytical.Document);
        analytical.PhysicalElements = elements.ToArray();
      }

      Params.TrySetData(DA, _AnalyticalElement_, () => analytical);
      Params.TrySetDataList(DA, _ModelElements_, () => analytical.PhysicalElements);
    }
  }
}

