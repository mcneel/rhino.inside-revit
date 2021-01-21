using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class StructuralAsset : Element<Types.StructuralAssetElement, DB.PropertySetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid =>
      new Guid("dde6da63-87bc-4250-9455-5233bfad8683");

    public StructuralAsset() : base(
      "Physical Asset",
      "Physical",
      "Represents a Revit Physical (Structural) Asset",
      "Params",
      "Revit Primitives"
      )
    { }
  }
}
