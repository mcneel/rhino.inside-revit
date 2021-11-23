using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
#if REVIT_2018
  public class ThermalMaterialType_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("9d9d0211-4598-4f50-921e-ad1208944e7c");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public ThermalMaterialType_ValueList()
    {
      Category = "Revit";
      SubCategory = "Material";
      Name = "Thermal Asset Class";
      NickName = "Thermal Class";
      Description = "Picker for thermal material class options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Gas", ((int) ARDB.ThermalMaterialType.Gas).ToString()));
      ListItems.Add(new GH_ValueListItem("Liquid", ((int) ARDB.ThermalMaterialType.Liquid).ToString()));
      ListItems.Add(new GH_ValueListItem("Solid", ((int) ARDB.ThermalMaterialType.Solid).ToString()));
    }
  }
#endif
}
