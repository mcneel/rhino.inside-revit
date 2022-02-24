using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Worksets
{
  [ComponentVersion(introduced: "1.2", updated: "1.6")]
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
        new Parameters.Param_Enum<Types.ModelUpdatesStatus>
        {
          Name = "Status",
          NickName = "S",
          Description = "Element status in the central model."
        },
        ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element)) return;
      else DA.SetData("Element", element);

      if (Params.GetData(DA, "Workset", out Types.Workset workset))
      {
        StartTransaction(element.Document);
        element.WorksetId = workset.Id;
      }

      Params.TrySetData(DA, "Workset", () => new Types.Workset(element.Document, element.WorksetId));
      Params.TrySetData(DA, "Status", () => ARDB.WorksharingUtils.GetModelUpdatesStatus(element.Document, element.Id));
    }
  }


  [ComponentVersion(introduced: "1.6")]
  public class ElementOwnership : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F68F96EC-977A-4103-94EE-932B8108193C");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public ElementOwnership() : base
    (
      name: "Element Ownership",
      nickname: "Ownership",
      description: "Element ownership status.",
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
          Description = "Element to access ownership status.",
        }
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
          Description = "Element to access ownership status.",
        }, ParamRelevance.Occasional
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.CheckoutStatus>
        {
          Name = "Ownership",
          NickName = "OS",
          Description = "Indicates the ownership status of an element."
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Creator",
          NickName = "C",
          Description = "The user name of the user who created the element.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Changed by",
          NickName = "CB",
          Description = "The user name of the most recent user who saved a user change of this element to the central model.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Owner",
          NickName = "O",
          Description = "The current owner of the element or empty string if no one owns the element.",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Requesters",
          NickName = "R",
          Description = "The ordered list of unique user names of users who have outstanding editing requests for the specified element.",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element)) return;
      else Params.TrySetData(DA, "Element", () => element);

      var _Ownership_ = Params.IndexOfOutputParam("Ownership");
      var _Creator_ = Params.IndexOfOutputParam("Creator");
      var _ChangedBy_ = Params.IndexOfOutputParam("Changed By");
      var _Owner_ = Params.IndexOfOutputParam("Owner");
      var _Requesters_ = Params.IndexOfOutputParam("Requesters");

      //if (element.Document.IsWorkshared)
      {
        if (_Ownership_ >= 0)
        {
          var ownership = default(ARDB.CheckoutStatus);
          if (_Owner_ >= 0)
          {
            ownership = ARDB.WorksharingUtils.GetCheckoutStatus(element.Document, element.Id, out var owner);
            DA.SetData(_Owner_, owner);
          }
          else ownership = ARDB.WorksharingUtils.GetCheckoutStatus(element.Document, element.Id);

          if (_Ownership_ >= 0)
            DA.SetData(_Ownership_, ownership);
        }

        if (_Creator_ >= 0 || _Owner_ >= 0 || _ChangedBy_ >= 0 || _Requesters_ >= 0)
        {
          using (var info = ARDB.WorksharingUtils.GetWorksharingTooltipInfo(element.Document, element.Id))
          {
            if (info is object)
            {
              Params.TrySetData(DA, "Creator", () => info.Creator);
              Params.TrySetData(DA, "Changed By", () => info.LastChangedBy);
              Params.TrySetData(DA, "Owner", () => info.Owner);
              Params.TrySetDataList(DA, "Requesters", () => info.GetRequesters());
            }
          }
        }
      }
    }
  }
}
