using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
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
        new GH_ValueListItem("None", ((int) ARDB.OpeningWrappingCondition.None).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Exterior", ((int) ARDB.OpeningWrappingCondition.Exterior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Interior", ((int) ARDB.OpeningWrappingCondition.Interior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Exterior and Interior", ((int) ARDB.OpeningWrappingCondition.ExteriorAndInterior).ToString())
        );
    }
  }
}
