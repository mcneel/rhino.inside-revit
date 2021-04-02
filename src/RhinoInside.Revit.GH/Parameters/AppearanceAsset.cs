using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class AppearanceAsset : Element<Types.AppearanceAssetElement, DB.AppearanceAssetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("941b2ee3-5423-4fee-9df6-27c77fdb53c9");

    public AppearanceAsset() : base
    (
      "Appearance Asset",
      "Appearance",
      "Contains a collection of Revit appearance asset elements",
      "Params",
      "Revit Primitives"
    )
    { }
  }
}
