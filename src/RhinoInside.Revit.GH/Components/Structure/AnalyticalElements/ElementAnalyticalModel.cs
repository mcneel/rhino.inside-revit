using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.36")]
  public class ElementAnalyticalModel : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("{DBC7A253-ACA8-407F-803F-0BA3B6EC5677}");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => string.Empty;

    public ElementAnalyticalModel() : base
    (
      name: "Element Analytical Model",
      nickname: "E-Analytical",
      description: "Get-Set access to the related analytical model of provided model element",
      category: "Revit",
      subCategory: "Structure"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _ModelElement_,
          NickName = "ME",
          Description = "Model element",
        }
      ),
#if !REVIT_2023
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Enable",
          NickName = "E",
          Description = "Enable Analytical Element",
          Optional = true,
        },ParamRelevance.Primary
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
          NickName = "AE",
          Description = $"Output {_AnalyticalElement_}",
        }
      ),
#if !REVIT_2023
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Enable",
          NickName = "E",
          Description = "Enable Analytical Element",
          Optional = true,
        },ParamRelevance.Primary
      ),
#endif
    };

    const string _ModelElement_ = "Model Element";
    const string _AnalyticalElement_ = "Analytical Element";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.GetData(DA, _ModelElement_, out Types.GraphicalElement element, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Enable", out bool? enable)) return;

      if (!element.IsPhysicalElement)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input element is not a model element.");
        return;
      }

      if (enable is object)
      {
        if (!element.Structural)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input model element is not structural.");
          return;
        }

        StartTransaction(element.Document);
        element.EnableAnalyticalModel = enable.Value;
      }

      Params.TrySetData(DA, "Enable", () => element.EnableAnalyticalModel);
      Params.TrySetData(DA, _AnalyticalElement_, () => element.AnalyticalElement);
    }
  }
}
