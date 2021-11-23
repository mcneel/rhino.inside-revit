using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace RhinoInside.Revit.GH.Parameters.Input
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
      
      ListItems.Add(new GH_ValueListItem("Do Not Wrap", ((int) External.DB.WallWrapping.DoNotWrap).ToString()));
      ListItems.Add(new GH_ValueListItem("Exterior", ((int) External.DB.WallWrapping.Exterior).ToString()));
      ListItems.Add(new GH_ValueListItem("Interior", ((int) External.DB.WallWrapping.Interior).ToString()));
      ListItems.Add(new GH_ValueListItem("Both", ((int) External.DB.WallWrapping.Both).ToString()));
    }
  }
}
