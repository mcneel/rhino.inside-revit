using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class SpatialElement_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("7FA5A410-23AD-4AC7-9E78-E96852DC8C97");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public SpatialElement_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Spatial Element Categories";
      NickName = "SE";
      Description = "Picker for kind of spatial elements";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Areas", ((int) ARDB.BuiltInCategory.OST_Areas).ToString()));
      ListItems.Add(new GH_ValueListItem("Rooms", ((int) ARDB.BuiltInCategory.OST_Rooms).ToString()));
      ListItems.Add(new GH_ValueListItem("Spaces", ((int) ARDB.BuiltInCategory.OST_MEPSpaces).ToString()));
    }
  }
}
