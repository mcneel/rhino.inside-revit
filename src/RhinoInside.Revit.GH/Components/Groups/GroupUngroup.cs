using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.GH.ElementTracking;

namespace RhinoInside.Revit.GH.Components.Groups
{
  //[ComponentVersion(introduced: "1.11")]
  class GroupUngroup : ElementTrackerComponent
  {
    public override Guid ComponentGuid => new Guid("7AE513C5-A9AB-4315-901C-B3BC4C30442C");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;

    public GroupUngroup() : base
    (
      name: "Group Ungroup",
      nickname: "Ungroup",
      description: "Ungroup a group and get its members list",
      category: "Revit",
      subCategory: "Type"
    )
    {
      TrackingMode = TrackingMode.Supersede;
    }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Group>("Group", "G", "Group to query", GH_ParamAccess.item)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>(_Members_, "M", "Group members", GH_ParamAccess.list)
    };

    const string _Members_ = "Members";

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Group", out Types.Group group, x => x.IsValid)) return;

      StartTransaction(group.Document);

      var members = group.Value.UngroupMembers().Select(x => Types.GraphicalElement.FromElementId(group.Document, x)).ToArray();

      for (int i = 0; i < members.Length; ++i)
        Params.WriteTrackedElement(_Members_, group.Document, members[i]);

      DA.SetDataList(_Members_, members);
    }
  }
}
