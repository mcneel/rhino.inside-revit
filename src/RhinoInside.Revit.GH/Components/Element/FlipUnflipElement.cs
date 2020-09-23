using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
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
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Facing",
          NickName = "F",
          Description = "New state for Element Facing flipping",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hand",
          NickName = "H",
          Description = "New state for Element Hand flipping",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Default
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Work Plane",
          NickName = "W",
          Description = "New state for Element Work Plane flipping",
          Access = GH_ParamAccess.item,
          Optional = true
        },
        ParamVisibility.Default
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
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Facing",
          NickName = "F",
          Description = "State for Element Face flipping",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Hand",
          NickName = "H",
          Description = "State for Element Hand flipping",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Work Plane",
          NickName = "W",
          Description = "State for Element Work Plane flipping",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      Types.GraphicalElement element = default;
      if (!DA.GetData("Element", ref element))
        return;

      bool? facing = default;
      {
        var _Facing_ = Params.IndexOfInputParam("Facing");
        if (_Facing_ >= 0 && Params.Input[_Facing_].DataType != GH_ParamData.@void)
        {
          bool flipped = false;
          if (DA.GetData(_Facing_, ref flipped))
            facing = flipped;
        }
        if (facing.HasValue && !element.CanFlipFacing)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Facing can not be flipped for this element. {{{element.Id.IntegerValue}}}");
          return;
        }
      }

      bool? hand = default;
      {
        var _Hand_ = Params.IndexOfInputParam("Hand");
        if (_Hand_ >= 0 && Params.Input[_Hand_].DataType != GH_ParamData.@void)
        {
          bool flipped = false;
          if (DA.GetData(_Hand_, ref flipped))
            hand = flipped;
        }
        if (hand.HasValue && !element.CanFlipFacing)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Hand can not be flipped for this element. {{{element.Id.IntegerValue}}}");
          return;
        }
      }

      bool? workplane = default;
      {
        var _WorkPlane_ = Params.IndexOfInputParam("Work Plane");
        if (_WorkPlane_ >= 0 && Params.Input[_WorkPlane_].DataType != GH_ParamData.@void)
        {
          bool flipped = false;
          if (DA.GetData(_WorkPlane_, ref flipped))
            workplane = flipped;
        }
        if (workplane.HasValue && !element.CanFlipWorkPlane)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Work Plane can not be flipped for this element. {{{element.Id.IntegerValue}}}");
          return;
        }
      }

      if (facing.HasValue || hand.HasValue || workplane.HasValue)
      {
        StartTransaction(element.Document);
        {
          element.FacingFlipped = facing;
          element.HandFlipped = hand;
          element.WorkPlaneFlipped = workplane;

          if (element is IGH_PreviewMeshData preview)
            preview.DestroyPreviewMeshes();
        }
      }

      DA.SetData("Element", element);
      DA.SetData("Facing", element.FacingFlipped);
      DA.SetData("Hand", element.HandFlipped);
      DA.SetData("Work Plane", element.WorkPlaneFlipped);
    }
  }
}
