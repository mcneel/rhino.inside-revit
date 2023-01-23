using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.12")]
  public class WorksetVisibilityOverrides : TransactionalChainComponent
  {
    public override Guid ComponentGuid => new Guid("B062C96E-D3AA-4CC1-B5ED-0AEDFD67417B");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => string.Empty;

    public WorksetVisibilityOverrides() : base
    (
      name: "Workset Visibility Overrides",
      nickname: "WV-Overrides",
      description: "Get-Set workset visibility overrides on the specified View",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query filter graphics overrides",
        }
      ),
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Workset",
          NickName = "W",
          Description = "Workset to access graphics overrides",
          Access = GH_ParamAccess.list
        }
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.WorksetVisibility>()
        {
          Name = "Visibility",
          NickName = "V",
          Description = "Workset visibility state",
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
        new Parameters.View()
        {
          Name = "View",
          NickName = "V",
          Description = "View to query workset graphic overrides",
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Workset()
        {
          Name = "Workset",
          NickName = "W",
          Description = "Workset to access graphic overrides state",
          Access = GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Parameters.Param_Enum<Types.WorksetVisibility>()
        {
          Name = "Visibility",
          NickName = "V",
          Description = "Workset visibility state",
          Access = GH_ParamAccess.list,
        }, ParamRelevance.Primary
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);

      if (!Params.GetDataList(DA, "Workset", out IList<Types.Workset> worksets)) return;
      else Params.TrySetDataList(DA, "Workset", () => worksets);

      if (Params.GetDataList(DA, "Visibility", out IList<ARDB.WorksetVisibility?> visibility) && visibility.Count > 0)
      {
        if (view.Value.AreGraphicsOverridesAllowed())
        {
          var worksetsToDisable = new HashSet<ARDB.WorksetId>(worksets.Count);
          var worksetsToEnable = new HashSet<ARDB.WorksetId>(worksets.Count);

          StartTransaction(view.Document);

          foreach (var pair in worksets.ZipOrLast(visibility, (Workset, Visibility) => (Workset, Visibility)))
          {
            if (!pair.Visibility.HasValue) continue;
            if (!view.Document.IsEquivalent(pair.Workset?.Document)) continue;
            if (pair.Workset?.IsValid != true) continue;

            view.Value.SetWorksetVisibility(pair.Workset.Id, pair.Visibility.Value);
          }
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Graphics Overrides are not allowed on View '{view.Value.Title}'");
      }

      Params.TrySetDataList
      (
        DA, "Visibility", () => worksets.Select
        (
          x => view.Document.IsEquivalent(x?.Document) && x.Id is ARDB.WorksetId worksetId ?
               view.Value.GetWorksetVisibility(worksetId) :
               default(ARDB.WorksetVisibility?)
        )
      );
    }
  }
}
