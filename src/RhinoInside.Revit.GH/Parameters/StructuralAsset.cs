using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class StructuralAsset : Element<Types.StructuralAssetElement, ARDB.PropertySetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("dde6da63-87bc-4250-9455-5233bfad8683");

    public StructuralAsset() : base
    (
      name: "Physical Asset",
      nickname: "Physical",
      description: "Contains a collection of Revit structural asset elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }
  }
}
