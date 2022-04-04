using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  [ComponentVersion(introduced: "1.7")]
  public class SpatialElementBoundaryLocation_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("53FDAB6F-B2F5-447D-BBB2-623B05866569");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public SpatialElementBoundaryLocation_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Spatial Element Boundary Location Line";
      NickName = "BLL";
      Description = "Picker for spatial element boundary location line options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Finish", ((int) ARDB.SpatialElementBoundaryLocation.Finish).ToString()));
      ListItems.Add(new GH_ValueListItem("Center", ((int) ARDB.SpatialElementBoundaryLocation.Center).ToString()));
      ListItems.Add(new GH_ValueListItem("Core Boundary", ((int) ARDB.SpatialElementBoundaryLocation.CoreBoundary).ToString()));
      ListItems.Add(new GH_ValueListItem("Core Center", ((int) ARDB.SpatialElementBoundaryLocation.CoreCenter).ToString()));
    }
  }
}
