using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class EndCapCondition_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("8D73D533-9EF9-4923-A025-B040110AD9DD");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public EndCapCondition_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "End Cap Condition";
      NickName = "ECC";
      Description = "Picker for end cap condition of a wall compound structure ";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("Exterior", ((int) DB.EndCapCondition.Exterior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Interior", ((int) DB.EndCapCondition.Interior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("NoEndCap", ((int) DB.EndCapCondition.NoEndCap).ToString())
        );
    }
  }
}
