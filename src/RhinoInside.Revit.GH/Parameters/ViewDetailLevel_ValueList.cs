using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Parameters
{
  public class ViewDetailLevel_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("B078E48A-C56F-4D51-A886-04E537445019");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public ViewDetailLevel_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Detail Level";
      NickName = "DL";
      Description = "Picker for level of detail";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("Coarse", ((int) DB.ViewDetailLevel.Coarse).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Medium", ((int) DB.ViewDetailLevel.Medium).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Fine", ((int) DB.ViewDetailLevel.Fine).ToString())
        );
    }
  }
}
