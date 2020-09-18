using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components
{
  public class PinUnPinElement: TransactionalComponent
  {
    public override Guid ComponentGuid => new Guid("CC205221-1583-47D1-A715-226C39C3FB34");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;
    protected override string IconTag => "PIN";

    public PinUnPinElement() : base
    (
      name: "Element Pin",
      nickname: "ElemPin",
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
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Pin",
          NickName = "P",
          Description = "New state for Element Pin",
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
        new Parameters.Element()
        {
          Name = "Element",
          NickName = "E",
          Description = "Element to access Pin",
          Access = GH_ParamAccess.item
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Pin",
          NickName = "P",
          Description = "State for Element Pin",
          Access = GH_ParamAccess.item
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.Element);
      if (!DA.GetData("Element", ref element))
        return;

      var _Pin_ = Params.IndexOfInputParam("Pin");
      if (_Pin_ >= 0 && Params.Input[_Pin_].DataType != GH_ParamData.@void)
      {
        bool pinned = false;
        if (!DA.GetData(_Pin_, ref pinned))
          return;

        var doc = element.Document;
        using (var transaction = NewTransaction(doc))
        {
          transaction.Start();
          element.APIElement.Pinned = pinned;
          transaction.Commit();
        }
      }

      DA.SetData("Element", element);
      DA.SetData("Pin", element.APIElement.Pinned);
    }
  }
}
