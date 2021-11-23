using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Worksets
{
  [ComponentVersion(introduced: "1.2")]
  public class WorksetIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("C33CD128-9BEE-4B0C-BB0E-3FFBFCB3C41E");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "WS";

    public WorksetIdentity() : base
    (
      name: "Workset Identity",
      nickname: "Workset",
      description: "Workset properties. Get-Set accessor to workset information.",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Workset",
          NickName = "W",
          Description = "Workset to access identity information",
        }
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Workset name",
          Optional = true
        },
        ParamRelevance.Occasional
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
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
        new Parameters.Param_Enum<Types.WorksetKind>
        {
          Name = "Kind",
          NickName = "K",
          Description = "Workset kind",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Name",
          NickName = "N",
          Description = "Workset name",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_String()
        {
          Name = "Owner",
          NickName = "O",
          Description = "User name of the workset owner",
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Open",
          NickName = "O",
          Description = "Whether the workset is open."
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Editable",
          NickName = "E",
          Description = "Whether the workset is editable."
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Visible",
          NickName = "V",
          Description = "Whether the workset is visible by default."
        },
        ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Default",
          NickName = "D",
          Description = "Whether the workset is the default one."
        },
        ParamRelevance.Occasional
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Workset", out Types.Workset workset)) return;

      if (Params.GetData(DA, "Name", out string name))
      {
        if (workset.Value.Name != name)
        {
          StartTransaction(workset.Document);
          ARDB.WorksetTable.RenameWorkset(workset.Document, workset.Id, name);
        }
      }

      DA.SetData("Workset", workset);
      Params.TrySetData(DA, "Kind", () => workset.Value.Kind);
      Params.TrySetData(DA, "Name", () => workset.Value.Name);
      Params.TrySetData(DA, "Owner", () => workset.Value.Owner);
      Params.TrySetData(DA, "Open", () => workset.Value.IsOpen);
      Params.TrySetData(DA, "Editable", () => workset.Value.IsEditable);
      Params.TrySetData(DA, "Visible", () => workset.Value.IsVisibleByDefault);
      Params.TrySetData(DA, "Default", () => workset.Value.IsDefaultWorkset);
    }
  }
}
