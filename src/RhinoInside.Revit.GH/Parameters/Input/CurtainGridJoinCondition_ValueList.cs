using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace RhinoInside.Revit.GH.Parameters.Input
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

      ListItems.Add(new GH_ValueListItem("Not Defined", ((int) External.DB.CurtainGridJoinCondition.NotDefined).ToString()));
      ListItems.Add(new GH_ValueListItem("Vertical Grid Continuous", ((int) External.DB.CurtainGridJoinCondition.VerticalGridContinuous).ToString()));
      ListItems.Add(new GH_ValueListItem("Horizontal Grid Continuous", ((int) External.DB.CurtainGridJoinCondition.HorizontalGridContinuous).ToString()));
      ListItems.Add(new GH_ValueListItem("Border & Vertical Grid Continuous", ((int) External.DB.CurtainGridJoinCondition.BorderAndVerticalGridContinuous).ToString()));
      ListItems.Add(new GH_ValueListItem("Border & Horizontal Grid Continuous", ((int) External.DB.CurtainGridJoinCondition.BorderAndHorizontalGridContinuous).ToString()));
    }
  }
}
