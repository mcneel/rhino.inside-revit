using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Worksets
{
  public class WorksetActive : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("AA467C94-D400-4F4A-80BF-DEFB309A4C52");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "A";

    public WorksetActive() : base
    (
      name: "Active Workset",
      nickname: "AWorkset",
      description: "Gets the active workset",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Workset>("Active Workset", "W", "Active workset", optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Workset>("Active Workset", "W", "Active workset", relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      if (doc.Value.GetWorksetTable() is DB.WorksetTable table)
      {
        if (Params.GetData(DA, "Active Workset", out Types.Workset active))
        {
          StartTransaction(doc.Value);
          table.SetActiveWorksetId(active.Id);
        }

        DA.SetData("Active Workset", new Types.Workset(doc.Value, table.GetActiveWorksetId()));
      }
    }
  }
}
