using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Elements
{
  public class FlipUnflipElement : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("4CADC9AA-27D9-4804-87AC-477203862AFA");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "F";

    public FlipUnflipElement()
    : base
    (
      name: "Flip Element",
      nickname: "FlipElem",
      description: "Flips or Unflips elements from Revit document",
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
          Description = "Element to access Flipping state",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hand",
          NickName = "H",
          Description = "New state for Element Hand flipping",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Facing",
          NickName = "F",
          Description = "New state for Element Facing flipping",
          Optional = true
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Work Plane",
          NickName = "W",
          Description = "New state for Element Work Plane flipping",
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
          Description = "Element to access Flipping state",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hand",
          NickName = "H",
          Description = "State for Element Hand flipping",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Facing",
          NickName = "F",
          Description = "State for Element Face flipping",
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Work Plane",
          NickName = "W",
          Description = "State for Element Work Plane flipping",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.GraphicalElement element)) return;
      else DA.SetData("Element", element);

      if
      (
        Params.GetData(DA, "Hand", out bool? hand) |
        Params.GetData(DA, "Facing", out bool? facing) |
        Params.GetData(DA, "Work Plane", out bool? workplane)
      )
      {
        UpdateElement
        (
          element.Value, () =>
          {
            element.HandFlipped = hand;
            element.FacingFlipped = facing;
            element.WorkPlaneFlipped = workplane;
          }
        );
      }

      DA.SetData("Facing", element.FacingFlipped);
      DA.SetData("Hand", element.HandFlipped);
      DA.SetData("Work Plane", element.WorkPlaneFlipped);
    }
  }
}
