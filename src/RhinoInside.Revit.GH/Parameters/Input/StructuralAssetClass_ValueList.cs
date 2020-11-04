using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
#if REVIT_2019
  public class StructuralAssetClass_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("6f5d09c7-797f-4ffe-afae-b9c0ddc5905f");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public StructuralAssetClass_ValueList()
    {
      Category = "Revit";
      SubCategory = "Material";
      Name = "Physical Asset Type";
      NickName = "PAMT";
      Description = "Picker for physical material type options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Generic", ((int) DB.StructuralAssetClass.Generic).ToString()));
      ListItems.Add(new GH_ValueListItem("Basic", ((int) DB.StructuralAssetClass.Basic).ToString()));
      ListItems.Add(new GH_ValueListItem("Concrete", ((int) DB.StructuralAssetClass.Concrete).ToString()));
      ListItems.Add(new GH_ValueListItem("Gas", ((int) DB.StructuralAssetClass.Gas).ToString()));
      ListItems.Add(new GH_ValueListItem("Liquid", ((int) DB.StructuralAssetClass.Liquid).ToString()));
      ListItems.Add(new GH_ValueListItem("Metal", ((int) DB.StructuralAssetClass.Metal).ToString()));
      ListItems.Add(new GH_ValueListItem("Plastic", ((int) DB.StructuralAssetClass.Plastic).ToString()));
      ListItems.Add(new GH_ValueListItem("Wood", ((int) DB.StructuralAssetClass.Wood).ToString()));
    }
  }
#endif
}
