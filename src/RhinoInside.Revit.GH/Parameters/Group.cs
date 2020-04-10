using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Group : GraphicalElementT<Types.Group, DB.Group>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("2674B9FF-E463-426B-8A8C-CCB5A7F4C84E");

    public Group() : base("Group", "Group", "Represents a Revit document group element.", "Params", "Revit") { }

    protected override Types.Group PreferredCast(object data) => data is DB.Group group ? new Types.Group(group) : null;
  }
}
