using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurtainMullionPosition_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("3DF76236-A44B-4BDB-89D0-7C2D6024962D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public CurtainMullionPosition_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Curtain Mullion Position";
      NickName = "CMP";
      Description = "Picker for curtain mullion position options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Parallel to Ground", ((int) DBX.CurtainMullionPosition.ParallelToGround).ToString()));
      ListItems.Add(new GH_ValueListItem("Perpendicular to Face", ((int) DBX.CurtainMullionPosition.PerpendicularToFace).ToString()));
    }
  }
}
