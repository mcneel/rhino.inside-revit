using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Filters
{
  public class SelectionElements : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("E90F2139-FA13-4EE2-BFD3-6642FA9053AB");
    public override GH_Exposure Exposure => GH_Exposure.septenary;

    public SelectionElements() : base
    (
      name: "Selection Elements",
      nickname: "Selection",
      description: "Selection Elements list.",
      category: "Revit",
      subCategory: "Filter"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Element>("Selection Filter", "S"),
      ParamDefinition.Create<Parameters.Element>("Elements", "E", access: GH_ParamAccess.list, optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Selection Filter", "S"),
      ParamDefinition.Create<Parameters.Element>("Elements", "E", access: GH_ParamAccess.list, relevance: ParamRelevance.Primary)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Selection Filter", out DB.SelectionFilterElement selection, x => x.IsValid())) return;

      if (Params.GetDataList(DA, "Elements", out IList<Types.Element> elements))
      {
        StartTransaction(selection.Document);

        var elementIds = elements?.Where(x => selection.Document.IsEquivalent(x.Document)).Select(x => x.Id).ToList();
        selection.SetElementIds(elementIds);
      }

      DA.SetData("Selection Filter", selection);
      Params.TrySetDataList(DA, "Elements", () => selection.GetElementIds().Select(x => Types.Element.FromElementId(selection.Document, x)));
    }
  }
}
