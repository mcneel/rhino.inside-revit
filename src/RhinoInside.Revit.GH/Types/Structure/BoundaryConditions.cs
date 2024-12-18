using System;
using System.Collections;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Boundary Conditions")]
  public class BoundaryConditions : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.Structure.BoundaryConditions);
    public new ARDB.Structure.BoundaryConditions Value => base.Value as ARDB.Structure.BoundaryConditions;

    public BoundaryConditions() { }
    public BoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }

  [Kernel.Attributes.Name("Area Boundary Conditions")]
  public class AreaBoundaryConditions : BoundaryConditions
  {
    protected override Type ValueType => typeof(AreaBoundaryConditions);
    public AreaBoundaryConditions() { }
    public AreaBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }

  [Kernel.Attributes.Name("Line Boundary Conditions")]
  public class LineBoundaryConditions : BoundaryConditions
  {
    protected override Type ValueType => typeof(PointBoundaryConditions);
    public LineBoundaryConditions() { }
    public LineBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }

  [Kernel.Attributes.Name("Point Boundary Conditions")]
  public class PointBoundaryConditions : BoundaryConditions
  {
    protected override Type ValueType => typeof(PointBoundaryConditions);
    public PointBoundaryConditions() { }
    public PointBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }
}

