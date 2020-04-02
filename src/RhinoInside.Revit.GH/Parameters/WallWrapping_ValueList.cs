using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


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
      // Revit API does not have an enum for this (eirannejad: 2020-04-02)
      ListItems.Add(new GH_ValueListItem("Do Not Wrap", "0"));
      ListItems.Add(new GH_ValueListItem("Exterior", "1"));
      ListItems.Add(new GH_ValueListItem("Interior", "2"));
      ListItems.Add(new GH_ValueListItem("Both", "3"));
    }
  }
}
