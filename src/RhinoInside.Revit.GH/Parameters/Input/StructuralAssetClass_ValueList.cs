using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
#if REVIT_2018
  public class StructuralAssetClass_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("6f5d09c7-797f-4ffe-afae-b9c0ddc5905f");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public StructuralAssetClass_ValueList()
    {
      Category = "Revit";
      SubCategory = "Material";
      Name = "Physical Asset Class";
      NickName = "Physical Class";
      Description = "Picker for physical material class options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Generic", ((int) ARDB.StructuralAssetClass.Generic).ToString()));
      ListItems.Add(new GH_ValueListItem("Basic", ((int) ARDB.StructuralAssetClass.Basic).ToString()));
      ListItems.Add(new GH_ValueListItem("Concrete", ((int) ARDB.StructuralAssetClass.Concrete).ToString()));
      ListItems.Add(new GH_ValueListItem("Gas", ((int) ARDB.StructuralAssetClass.Gas).ToString()));
      ListItems.Add(new GH_ValueListItem("Liquid", ((int) ARDB.StructuralAssetClass.Liquid).ToString()));
      ListItems.Add(new GH_ValueListItem("Metal", ((int) ARDB.StructuralAssetClass.Metal).ToString()));
      ListItems.Add(new GH_ValueListItem("Plastic", ((int) ARDB.StructuralAssetClass.Plastic).ToString()));
      ListItems.Add(new GH_ValueListItem("Wood", ((int) ARDB.StructuralAssetClass.Wood).ToString()));
    }
  }
#endif
}
