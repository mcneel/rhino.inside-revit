using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ThermalAsset : Element<Types.ThermalAssetElement, DB.PropertySetElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("b0a6689a-f2cd-4360-980c-d61a1f0c0453");

    public ThermalAsset() : base(
      "Thermal Asset",
      "Thermal",
      "Represents a Revit Thermal Asset",
      "Params",
      "Revit Primitives"
      )
    { }
  }
}
