using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.ParameterElements
{
  public class ParameterFormula : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("21F9F9C6-E5C2-4D38-820A-99AC4517F62F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "fx";

    public ParameterFormula() : base
    (
      name: "Parameter Formula",
      nickname: "Formula",
      description: "Parameter formula property. Get-Set accessor to Parameter formula properties",
      category: "Revit",
      subCategory: "Parameter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Formula",
          NickName = "F",
          Description = "Parameter formula value",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Reporting",
          NickName = "R",
          Description = "Parameter reporting value",
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
        new Parameters.ParameterKey()
        {
          Name = "Parameter",
          NickName = "P"
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Formula",
          NickName = "F",
          Description = "Parameter formula value",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Reporting",
          NickName = "R",
          Description = "Parameter reporting value",
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // Input
      if (!Parameters.ParameterKey.GetDocumentParameter(this, DA, "Parameter", out var key)) return;
      if (!Params.TryGetData(DA, "Formula", out string formula)) return;
      if (!Params.TryGetData(DA, "Reporting", out bool? reporting)) return;

      if (reporting is object || formula is object)
      {
        StartTransaction(key.Document);
        key.IsReporting = reporting;
        key.Formula = formula;
      }

      DA.SetData("Parameter", key);
      Params.TrySetData(DA, "Formula", () => key.Formula);
      Params.TrySetData(DA, "Reporting", () => key.IsReporting);
    }
  }
}
