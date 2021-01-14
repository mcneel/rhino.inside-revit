using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


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
        new GH_ValueListItem("Basic", ((int) DB.WallKind.Basic).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Curtain", ((int) DB.WallKind.Curtain).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Stacked", ((int) DB.WallKind.Stacked).ToString())
        );
    }
  }
}
