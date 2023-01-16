using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Grasshopper.Kernel.Parameters;

  [ComponentVersion(introduced: "1.12")]
  public class WorksetGlobalVisibility : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("2922AF4A-7252-4CDC-90D9-788E4576F500");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => string.Empty;

    public WorksetGlobalVisibility() : base
    (
      name: "Workset Global Visibility",
      nickname: "WG-Visibility",
      description: "Get-Set workset global visibility",
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
          Description = "Workset to access visibility status",
          Access = GH_ParamAccess.list,
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean()
        {
          Name = "Visible",
          NickName = "V",
          Description = "Workset visibility status",
          Access = GH_ParamAccess.list,
          Optional = true
        }, ParamRelevance.Primary
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
          Description = "Workset to access visibility status",
          Access = GH_ParamAccess.list,
        }
      ),
      new ParamDefinition
      (
        new Param_Boolean
        {
          Name = "Visible",
          NickName = "V",
          Description = "Workset visibility status",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetDataList(DA, "Workset", out IList<Types.Workset> worksets)) return;
      else DA.SetDataList("Workset", worksets);

      var visibilitySettings = worksets.Select(x => x.Document).OfType<ARDB.Document>().Distinct().ToDictionary(x => x, ARDB.WorksetDefaultVisibilitySettings.GetWorksetDefaultVisibilitySettings);

      if (Params.GetDataList(DA, "Visible", out IList<bool?> visibles) && visibles.Count > 0)
      {
        foreach (var set in worksets.ZipOrLast(visibles, (Workset, Visibile) => (Workset, Visibile)).GroupBy(x => x.Workset.Document))
        {
          StartTransaction(set.Key);
          var globalSettings = visibilitySettings[set.Key];

          foreach (var (workset, visible) in set.Where(x => x.Visibile.HasValue))
            globalSettings.SetWorksetVisibility(workset.Id, visible.Value);
        }
      }

      Params.TrySetDataList
      (
        DA, "Visible", () => worksets.Select(x => (x.Document is object ? visibilitySettings[x.Document] : null)?.IsWorksetVisible(x.Id))
      );
    }
  }
}
