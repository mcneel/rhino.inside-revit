using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Parameters
{
  public class OpeningWrappingCondition_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("55B31952-FAE4-41A1-92B4-B59E80F8223C");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public OpeningWrappingCondition_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Opening Wrapping Condition";
      NickName = "OWC";
      Description = "Picker for compound structure layers wrapping at openings setting ";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("None", ((int) DB.OpeningWrappingCondition.None).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Exterior", ((int) DB.OpeningWrappingCondition.Exterior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Interior", ((int) DB.OpeningWrappingCondition.Interior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Exterior and Interior", ((int) DB.OpeningWrappingCondition.ExteriorAndInterior).ToString())
        );
    }
  }
}
