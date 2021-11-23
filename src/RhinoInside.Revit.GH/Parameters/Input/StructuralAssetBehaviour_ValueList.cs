using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Input
{
#if REVIT_2018
  public class StructuralAssetBehaviour_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("c907b51e-eea7-4ecf-a110-79ef1b7069ec");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    public StructuralAssetBehaviour_ValueList()
    {
      Category = "Revit";
      SubCategory = "Material";
      Name = "Physical/Thermal Asset Behaviour";
      NickName = "Behaviour";
      Description = "Picker material behaviour options of physical or thermal assets";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Isotropic", ((int) ARDB.StructuralBehavior.Isotropic).ToString()));
      ListItems.Add(new GH_ValueListItem("Orthotropic", ((int) ARDB.StructuralBehavior.Orthotropic).ToString()));
      ListItems.Add(new GH_ValueListItem("Transverse Isotropic", ((int) ARDB.StructuralBehavior.TransverseIsotropic).ToString()));
    }
  }
#endif
}
