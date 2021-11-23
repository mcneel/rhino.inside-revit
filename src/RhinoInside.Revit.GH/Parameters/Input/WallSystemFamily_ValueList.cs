using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class WallSystemFamily_ValueList: GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("15545E80-87AB-4510-925D-D52E2CE5D839");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public WallSystemFamily_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Wall System Family";
      NickName = "WSF";
      Description = "Picker for builtin Wall system families";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("Basic", ((int) ARDB.WallKind.Basic).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Curtain", ((int) ARDB.WallKind.Curtain).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Stacked", ((int) ARDB.WallKind.Stacked).ToString())
        );
    }
  }
}
