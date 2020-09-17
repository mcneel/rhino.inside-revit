using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ThermalMaterialType_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("9d9d0211-4598-4f50-921e-ad1208944e7c");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public ThermalMaterialType_ValueList()
    {
      Category = "Revit";
      SubCategory = "Material";
      Name = "Thermal Material Type";
      NickName = "TMT";
      Description = "Picker for thermal material type options";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Gas", ((int) DB.ThermalMaterialType.Gas).ToString()));
      ListItems.Add(new GH_ValueListItem("Liquid", ((int) DB.ThermalMaterialType.Liquid).ToString()));
      ListItems.Add(new GH_ValueListItem("Solid", ((int) DB.ThermalMaterialType.Solid).ToString()));
    }
  }
}
