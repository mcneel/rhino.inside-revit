using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ThermalAsset : Element<Types.ThermalAssetElement, ARDB.PropertySetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    public override Guid ComponentGuid => new Guid("b0a6689a-f2cd-4360-980c-d61a1f0c0453");

    public ThermalAsset() : base
    (
      name: "Thermal Asset",
      nickname: "Thermal",
      description: "Contains a collection of Revit thermal assets elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }
  }
}
