using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.14")]
  public class WallFoundation : GraphicalElement<Types.WallFoundation, ARDB.WallFoundation>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("CA456FEA-7C01-452E-BB76-73D749EF5B49");

    public WallFoundation() : base("Wall Foundation", "Wall Foundation", "Contains a collection of Revit wall foundation elements", "Params", "Revit Elements") { }
  }
}
