using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Boundary Conditions")]
  public class BoundaryConditions : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Structure.BoundaryConditions);
    public new ARDB.Structure.BoundaryConditions Value => base.Value as ARDB.Structure.BoundaryConditions;

    public BoundaryConditions() { }
    public BoundaryConditions(ARDB.Structure.BoundaryConditions beamSystem) : base(beamSystem) { }
  }
}

