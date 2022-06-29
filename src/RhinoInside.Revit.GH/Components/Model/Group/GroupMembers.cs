using System;
using System.Linq;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.ModelElements
{
  [ComponentVersion(introduced: "1.0", updated: "1.9")]
  public class GroupMembers : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("7C7D3739-7609-4F7F-BAB5-1E3648508891");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public GroupMembers() : base
    (
      name: "Group Members",
      nickname: "Group",
      description: "Get group members list",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.Group>("Group", "G", "Group to query", GH_ParamAccess.item)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Members", "M", "Group members", GH_ParamAccess.list)
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("Elements") is IGH_Param elements)
        elements.Name = "Members";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Group", out Types.Group group, x => x.IsValid)) return;
      DA.SetDataList("Members", group.Value.GetMemberIds().Select(x => Types.GraphicalElement.FromElementId(group.Document, x)));
    }
  }
}
