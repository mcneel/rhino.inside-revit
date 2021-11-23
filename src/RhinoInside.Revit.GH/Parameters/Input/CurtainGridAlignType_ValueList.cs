using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class CurtainGridAlignType_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("3CBC9E4C-7D94-4AD3-ACEC-AB0D2611C847");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public CurtainGridAlignType_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Curtain Grid Align Type";
      NickName = "CGAT";
      Description = "Picker for curtain grid align type options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("No Justify", ((int) ARDB.CurtainGridAlignType.NoJustify).ToString()));
      ListItems.Add(new GH_ValueListItem("Beginning", ((int) ARDB.CurtainGridAlignType.Beginning).ToString()));
      ListItems.Add(new GH_ValueListItem("Center", ((int) ARDB.CurtainGridAlignType.Center).ToString()));
      ListItems.Add(new GH_ValueListItem("End", ((int) ARDB.CurtainGridAlignType.End).ToString()));
    }
  }
}
