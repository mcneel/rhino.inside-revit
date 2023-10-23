using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Sheets
{
  [ComponentVersion(introduced: "1.11")]
  public class SheetRevisions : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("2120C0FB-FA7A-4C3C-A873-515F351AE964");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public SheetRevisions() : base
    (
      name: "Sheet Revisions",
      nickname: "Revisions",
      description: "Sheet Revisions.",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.ViewSheet>("Sheet", "S"),
      ParamDefinition.Create<Parameters.Revision>("Revisions on Sheet", "ROS", "Revisions that are additionally included in the sheet's revision schedules", optional: true, relevance: ParamRelevance.Secondary, access: GH_ParamAccess.list),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.ViewSheet>("Sheet", "S", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Revision>("Current Revision", "CR", relevance: ParamRelevance.Tertiary),
      ParamDefinition.Create<Parameters.Revision>("Revisions on Sheet", "ROS", "Revisions that are additionally included in the sheet's revision schedules", relevance: ParamRelevance.Secondary, access: GH_ParamAccess.list),
      ParamDefinition.Create<Parameters.Revision>("Scheduled Revisions", "SR", "Ordered list of Revisions which participate in the Sheet's revision schedules.", access: GH_ParamAccess.list),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Sheet", out Types.ViewSheet sheet, x => x.IsValid)) return;
      else Params.TrySetData(DA, "Sheet", () => sheet);

      bool update = false;
      update |= Params.GetDataList(DA, "Revisions on Sheet", out IList<Types.Revision> revisions);

      if (update)
      {
        StartTransaction(sheet.Document);
        sheet.Value.SetAdditionalRevisionIds(revisions.OfType<Types.Revision>().Select(x => x.Id).ToArray());
        sheet.Document.Regenerate();
      }

      Params.TrySetData(DA, "Current Revision", () => Types.Element.FromElementId(sheet.Document, sheet.Value.GetCurrentRevision()) as Types.Revision);
      Params.TrySetDataList(DA, "Revisions on Sheet", () => sheet.Value.GetAdditionalRevisionIds().Select(x => Types.Element.FromElementId(sheet.Document, x)));
      Params.TrySetDataList(DA, "Scheduled Revisions", () => sheet.Value.GetAllRevisionIds().Select(x => Types.Element.FromElementId(sheet.Document, x)));
    }
  }
}
