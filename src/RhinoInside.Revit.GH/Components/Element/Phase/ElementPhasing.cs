using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementPropertyPhasing : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("BE455CF8-5650-4890-9CC6-EEF506850FAA");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "P";

    public ElementPropertyPhasing()
    : base
    (
      "Element Phasing",
      "Phasing",
      "Element Phasing Properties. Get-Set accessor to Element phasing properties.",
      "Revit",
      "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Phase",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase Created",
          NickName = "PC",
          Description = "Phase where element is constructed",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase Demolished",
          NickName = "PD",
          Description = "Phase where element is demolished",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamRelevance.Primary
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Type",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase Created",
          NickName = "PC",
          Description = "Phase where element is constructed",
          Access = GH_ParamAccess.item,
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Phase Demolished",
          NickName = "PD",
          Description = "Phase where element is demolished",
          Access = GH_ParamAccess.item,
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.Element);
      if (!DA.GetData("Element", ref element))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Phase Created", out Types.Phase createdPhase);
      update |= Params.GetData(DA, "Phase Demolished", out Types.Phase demolishedPhase);

      if (update)
      {
        StartTransaction(element.Document);
        element.CreatedPhase = createdPhase;
        element.DemolishedPhase = demolishedPhase;
      }

      Params.TrySetData(DA, "Element", () => element);
      Params.TrySetData(DA, "Phase Created", () => element.CreatedPhase);
      Params.TrySetData(DA, "Phase Demolished", () => element.DemolishedPhase);
    }
  }
}
