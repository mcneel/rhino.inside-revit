using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Phasing
{
  [ComponentVersion(since: "1.2")]
  public class GraphicalElementPhasing : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("3BA4524A-D4E5-4392-88B3-17A0CF651B09");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "P";

    public GraphicalElementPhasing() : base
    (
      name: "Element Phasing",
      nickname: "Phasing",
      description: "Element Phasing properties. Get-Set accessor to element phasing information.",
      category: "Revit",
      subCategory: "Element"
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
          Description = "Element to access phasing information",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Created",
          NickName = "C",
          Description = "Phase at which the Element was created",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Demolished",
          NickName = "D",
          Description = "Phase at which the Element was demolished",
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
        new Parameters.GraphicalElement()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access phasing information",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Created",
          NickName = "C",
          Description = "Phase at which the Element was created",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Phase()
        {
          Name = "Demolished",
          NickName = "D",
          Description = "Phase at which the Element was demolished",
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element)) return;

      if (Params.GetData(DA, "Created", out Types.Phase created))
      {
        StartTransaction(element.Document);
        element.CreatedPhase = created;
      }

      if (Params.GetData(DA, "Demolished", out Types.Phase demolished))
      {
        StartTransaction(element.Document);
        element.DemolishedPhase = demolished;
      }

      DA.SetData("Element", element);
      Params.TrySetData(DA, "Created", () => element.CreatedPhase);
      Params.TrySetData(DA, "Demolished", () => element.DemolishedPhase);
    }
  }
}
