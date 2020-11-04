using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class WallWrapping_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("141F0DA4-659E-47E4-8033-3B61057F860B");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public WallWrapping_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Wall Wrapping";
      NickName = "WR";
      Description = "Picker for builtin Wall wrapping options";

      ListItems.Clear();
      
      ListItems.Add(new GH_ValueListItem("Do Not Wrap", ((int) DBX.WallWrapping.DoNotWrap).ToString()));
      ListItems.Add(new GH_ValueListItem("Exterior", ((int) DBX.WallWrapping.Exterior).ToString()));
      ListItems.Add(new GH_ValueListItem("Interior", ((int) DBX.WallWrapping.Interior).ToString()));
      ListItems.Add(new GH_ValueListItem("Both", ((int) DBX.WallWrapping.Both).ToString()));
    }
  }
}
