using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurtainGridLine : ElementIdWithoutPreviewParam<Types.CurtainGridLine, object>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("A2DD571E-729C-4F69-BD34-2769583D329B");

    public CurtainGridLine() : base(
      name: "CurtainGridLine",
      nickname: "CurtainGridLine",
      description: "Represents a Revit CurtainGridLine element.",
      category: "Params",
      subcategory: "Revit"
      ) { }

    protected override Types.CurtainGridLine PreferredCast(object data) => Types.CurtainGridLine.FromValue(data as DB.CurtainGridLine) as Types.CurtainGridLine;
  }
}
