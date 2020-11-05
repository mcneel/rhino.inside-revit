using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class WallLocationLine_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("A4EB9313-A38B-4794-B13B-133D3D4C872D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public WallLocationLine_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Wall Location Line";
      NickName = "WLL";
      Description = "Picker for builtin Wall location line options";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("Wall Centerline", ((int) DB.WallLocationLine.WallCenterline).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Core Centerline", ((int) DB.WallLocationLine.CoreCenterline).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Finish (Exterior Face)", ((int) DB.WallLocationLine.FinishFaceExterior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Finish (Interior Face)", ((int) DB.WallLocationLine.FinishFaceInterior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Core (Exterior Face)", ((int) DB.WallLocationLine.CoreExterior).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Core (Interior Face)", ((int) DB.WallLocationLine.CoreInterior).ToString())
        );
    }
  }
}
