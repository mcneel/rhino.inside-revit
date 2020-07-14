using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurtainGridJoinCondition_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("9C2D116D-516C-4825-BB6B-67111B9100B1");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public CurtainGridJoinCondition_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Curtain Grid Join Condition";
      NickName = "CGJC";
      Description = "Picker for curtain grid join condition options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Not Defined", ((int) DBX.CurtainGridJoinCondition.NotDefined).ToString()));
      ListItems.Add(new GH_ValueListItem("Vertical Grid Continuous", ((int) DBX.CurtainGridJoinCondition.VerticalGridContinuous).ToString()));
      ListItems.Add(new GH_ValueListItem("Horizontal Grid Continuous", ((int) DBX.CurtainGridJoinCondition.HorizontalGridContinuous).ToString()));
      ListItems.Add(new GH_ValueListItem("Border & Vertical Grid Continuous", ((int) DBX.CurtainGridJoinCondition.BorderAndVerticalGridContinuous).ToString()));
      ListItems.Add(new GH_ValueListItem("Border & Horizontal Grid Continuous", ((int) DBX.CurtainGridJoinCondition.BorderAndHorizontalGridContinuous).ToString()));
    }
  }
}
