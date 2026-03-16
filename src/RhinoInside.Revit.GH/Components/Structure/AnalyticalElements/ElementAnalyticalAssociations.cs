using System;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Structure
{
  [ComponentVersion(introduced: "1.36")]
  public class ElementAnalyticalAssociations : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("{DBC7A253-ACA8-407F-803F-0BA3B6EC5677}");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override string IconTag => string.Empty;

    public ElementAnalyticalAssociations() : base
    (
      name: "Element Analytical Associations",
      nickname: "E-Associations",
      description: "Get the related analytical and model elements of the provided element",
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
          Name = "Element",
          NickName = "E",
          Description = "Element to query",
        }
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.AnalyticalElement()
        {
          Name = _AnalyticalElements_,
          NickName = "AE",
          Description = "Associated analytical elements",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.GraphicalElement()
        {
          Name = _PhysicalElements_,
          NickName = "ME",
          Description = "Associated model elements",
        }
      ),
    };

    const string _AnalyticalElements_ = "Analytical Elements";
    const string _PhysicalElements_ = "Model Elements";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element, x => x.IsValid)) return;

      if (element.IsPhysicalElement)
      {
        var analyticalElements = element.AnalyticalElements;
        Params.TrySetDataList(DA, _AnalyticalElements_, () => analyticalElements);
        Params.TrySetDataList(DA, _PhysicalElements_, () => analyticalElements.FirstOrDefault()?.PhysicalElements);
      }
      else if (element is Types.AnalyticalElement analyticalElement)
      {
        var physicalElements = analyticalElement.PhysicalElements;
        Params.TrySetDataList(DA, _AnalyticalElements_, () => physicalElements.FirstOrDefault()?.AnalyticalElements);
        Params.TrySetDataList(DA, _PhysicalElements_, () => physicalElements);
      }
      else
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input element is not a model nor an analytical element.");
      }
    }
  }
}
