using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Worksets
{
  [ComponentVersion(since: "1.2")]
  public class ElementWorkset : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("B441BA8C-429E-4F92-90DC-97DA3F14EB85");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "WS";

    public ElementWorkset() : base
    (
      name: "Element Workset",
      nickname: "Workset",
      description: "Element Workset properties. Get-Set accessor to element workset information.",
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
          Description = "Element to access workset information",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Workset",
          NickName = "W",
          Description = "Workset at which the Element belongs to",
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
          Description = "Element to access workset information",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Workset",
          NickName = "W",
          Description = "Workset at which the Element belongs to",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Edited by",
          NickName = "E",
          Description = "Workset at which the Element belongs to",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Owner",
          NickName = "O",
          Description = "User that owns the element",
        },
        ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.CheckoutStatus>
        {
          Name = "Status",
          NickName = "S",
          Description = "Checkout Status"
        },
        ParamRelevance.Occasional
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element)) return;

      if (Params.GetData(DA, "Workset", out Types.Workset workset))
      {
        StartTransaction(element.Document);
        element.WorksetId = workset.Id;
      }

      DA.SetData("Element", element);
      Params.TrySetData(DA, "Workset", () => new Types.Workset(element.Document, element.WorksetId));
      Params.TrySetData(DA, "Edited by", () => element.Value.get_Parameter(DB.BuiltInParameter.EDITED_BY)?.AsString());

      var _Owner_ = Params.IndexOfOutputParam("Owner");
      var _Status_ = Params.IndexOfOutputParam("Status");

      if (_Owner_ >= 0 || _Status_ >= 0)
      {
        var checkoutStatus = default(DB.CheckoutStatus);
        if (_Owner_ >= 0)
        {
          checkoutStatus = DB.WorksharingUtils.GetCheckoutStatus(element.Document, element.Id, out var owner);
          DA.SetData(_Owner_, owner);
        }
        else checkoutStatus = DB.WorksharingUtils.GetCheckoutStatus(element.Document, element.Id);

        if (_Status_ >= 0)
          DA.SetData(_Status_, checkoutStatus);
      }
    }
  }
}
