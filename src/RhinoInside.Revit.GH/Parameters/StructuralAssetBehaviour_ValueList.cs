using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
#if REVIT_2019
  public class StructuralAssetBehaviour_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("c907b51e-eea7-4ecf-a110-79ef1b7069ec");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public StructuralAssetBehaviour_ValueList()
    {
      Category = "Revit";
      SubCategory = "Material";
      Name = "Physical/Thermal Asset Behaviour";
      NickName = "PTMB";
      Description = "Picker material behaviour options of physical or thermal assets";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Isotropic", ((int) DB.StructuralBehavior.Isotropic).ToString()));
      ListItems.Add(new GH_ValueListItem("Orthotropic", ((int) DB.StructuralBehavior.Orthotropic).ToString()));
      ListItems.Add(new GH_ValueListItem("Transverse Isotropic", ((int) DB.StructuralBehavior.TransverseIsotropic).ToString()));
    }
  }
#endif
}
