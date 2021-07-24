using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class SpatialElement : Element<Types.SpatialElement, DB.SpatialElement>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("8774ACF3-7B77-474F-B12B-03D4CBBC3C15");

    public SpatialElement() : base
    (
      name: "Spatial Element",
      nickname: "Spatial Element",
      description: "Contains a collection of Revit spatial elements",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}
