using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace RhinoInside.Revit.GH.Components.Element.Sheet
{
  [ComponentVersion(since: "1.2", updated: "1.2.1")]
  public class SheetIdentity : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("CADF5FBB-9DEA-4B9F-8214-9897CEC0E54A");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public SheetIdentity() : base
    (
      name: "Sheet Identity",
      nickname: "Identity",
      description: "Sheet Identity Data.",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.ViewSheet>("Sheet", "S"),

      ParamDefinition.Create<Param_String>("Sheet Number", "NUM", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Sheet Name", "N", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Sheet Issue Date", "ID", optional: true, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Appears In Sheet List", "AISL", optional: true, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ViewSheet>("Sheet", "S", relevance: ParamRelevance.Occasional),

      ParamDefinition.Create<Param_Boolean>("Placeholder", "PH", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Sheet Number", "NUM", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Sheet Name", "N", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Sheet Issue Date", "ID", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Appears In Sheet List", "AISL", relevance: ParamRelevance.Primary),
  };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sheet", out Types.ViewSheet sheet, x => x.IsValid))
        return;

      bool update = false;
      update |= Params.GetData(DA, "Sheet Number", out string number);
      update |= Params.GetData(DA, "Sheet Name", out string name);
      update |= Params.GetData(DA, "Sheet Issue Date", out string date);
      update |= Params.GetData(DA, "Appears In Sheet List", out bool? scheduled);

      if (update)
      {
        StartTransaction(sheet.Document);
        sheet.SheetNumber = number;
        sheet.Name = name;
        sheet.SheetIssueDate = date;
        sheet.SheetScheduled = scheduled;
      }

      Params.TrySetData(DA, "Sheet", () => sheet);
      Params.TrySetData(DA, "Placeholder", () => sheet.IsPlaceholder);
      Params.TrySetData(DA, "Sheet Number", () => sheet.SheetNumber);
      Params.TrySetData(DA, "Sheet Name", () => sheet.Name);
      Params.TrySetData(DA, "Sheet Issue Date", () => sheet.SheetIssueDate);
      Params.TrySetData(DA, "Appears In Sheet List", () => sheet.SheetScheduled);
    }
  }
}
