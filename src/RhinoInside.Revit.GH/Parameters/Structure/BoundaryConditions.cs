using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.27")]
  public class BoundaryConditions : GraphicalElement<Types.BoundaryConditions, ARDB.Structure.BoundaryConditions>
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("DEE191F4-9D73-46C7-9BD0-EBBBD5B8D4A6");

    public BoundaryConditions() : base("Boundary Conditions", "Boundary Conditions", "Contains a collection of Revit Boundary Conditions", "Params", "Revit Elements") { }

  }
}
