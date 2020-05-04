using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurtainGridMullion : ElementIdWithoutPreviewParam<Types.CurtainGridMullion, object>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("6D845CBD-1962-4912-80C1-F47FE99AD54A");

    public CurtainGridMullion() : base(
      name: "CurtainGridMullion",
      nickname: "CurtainGridMullion",
      description: "Represents a Revit CurtainGridMullion element.",
      category: "Params",
      subcategory: "Revit"
      ) { }

    protected override Types.CurtainGridMullion PreferredCast(object data) => Types.CurtainGridMullion.FromValue(data as DB.Mullion) as Types.CurtainGridMullion;
  }
}
