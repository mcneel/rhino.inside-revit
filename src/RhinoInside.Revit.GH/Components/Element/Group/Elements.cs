using System;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class GroupElements : Component
  {
    public override Guid ComponentGuid => new Guid("7C7D3739-7609-4F7F-BAB5-1E3648508891");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public GroupElements() : base
    (
      name: "Group Elements",
      nickname: "Group",
      description: "Get group elements list",
      category: "Revit",
      subCategory: "Model"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Group(), "Group", "G", "Group to query", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Elements", "E", "Group Elements", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var group = default(DB.Group);
      if (!DA.GetData("Group", ref group))
        return;

      DA.SetDataList("Elements", group.GetMemberIds().Select(x => Types.Element.FromElementId(group.Document, x)));
    }
  }
}
