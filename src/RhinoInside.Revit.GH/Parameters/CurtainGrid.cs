using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurtainGrid : ParamWithPreview<Types.CurtainGrid>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("7519D945-1539-47DB-BFF2-AE848B08C5C3");
    public CurtainGrid() : base("Curtain Grid", "CGrid", "Contains a collection of Revit curtain grids", "Params", "Revit Primitives") { }
  }
}
