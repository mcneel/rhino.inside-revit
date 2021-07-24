using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurtainGrid : ParamWithPreview<Types.CurtainGrid>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("7519D945-1539-47DB-BFF2-AE848B08C5C3");
    public CurtainGrid() : base
    (
      name: "Curtain Grid",
      nickname: "CGrid",
      description: "Contains a collection of Revit curtain grids",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }
  }

  public class CurtainCell : Param<Types.DataObject<DB.CurtainCell>>
  {
    public override Guid ComponentGuid => new Guid("F25FAC7B-B338-4E12-A974-F2238E3B83C2");
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public CurtainCell() : base
    (
      name: "Curtain Cell",
      nickname: "CCell",
      description: "Contains a collection of Revit curtain grid cells",
      category: "Params",
      subcategory: "Revit Primitives"
    )
    { }
  }
}
