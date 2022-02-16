using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class PinUnPinElement: TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("CC205221-1583-47D1-A715-226C39C3FB34");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "PIN";

    public PinUnPinElement() : base
    (
      name: "Pin Element",
      nickname: "PinElem",
      description: "Pins or Unpins elements from Revit document",
      category: "Revit",
      subCategory: "Element"
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
          Description = "Element to access Pin",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Pinned",
          NickName = "P",
          Description = "New state for Element Pin",
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
          Description = "Element to access Pin",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Pinned",
          NickName = "P",
          Description = "State for Element Pin",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;
      else DA.SetData("Element", element);

      if (Params.GetData(DA, "Pinned", out bool? pinned))
        UpdateElement(element.Value, () => element.Pinned = pinned);

      DA.SetData("Pinned", element.Pinned);
    }
  }
}
