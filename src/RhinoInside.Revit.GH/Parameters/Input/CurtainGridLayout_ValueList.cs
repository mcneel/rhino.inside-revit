using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class CurtainGridLayout_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("CD3E68B2-2BD5-4CDB-80B7-F8CB5A313FEF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public CurtainGridLayout_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Curtain Grid Layout";
      NickName = "CGL";
      Description = "Picker for curtain grid layout options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("None", ((int) DBX.CurtainGridLayout.None).ToString()));
      ListItems.Add(new GH_ValueListItem("Fixed Distance", ((int) DBX.CurtainGridLayout.FixedDistance).ToString()));
      ListItems.Add(new GH_ValueListItem("Fixed Number", ((int) DBX.CurtainGridLayout.FixedNumber).ToString()));
      ListItems.Add(new GH_ValueListItem("Maximum Spacing", ((int) DBX.CurtainGridLayout.MaximumSpacing).ToString()));
      ListItems.Add(new GH_ValueListItem("Minimum Spacing", ((int) DBX.CurtainGridLayout.MinimumSpacing).ToString()));
    }
  }
}
