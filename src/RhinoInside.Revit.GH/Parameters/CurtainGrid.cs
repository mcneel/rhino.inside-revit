using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CurtainGrid : ParamWithPreview<Types.CurtainGrid>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("7519D945-1539-47DB-BFF2-AE848B08C5C3");
    public CurtainGrid() : base("Curtain Grid", "CGrid", "Represents a Revit curtain grid.", "Params", "Revit Primitives") { }
    protected override Types.CurtainGrid PreferredCast(object data) => base.PreferredCast(data);
  }
}
