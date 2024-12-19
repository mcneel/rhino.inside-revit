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
    public BoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }

  [Kernel.Attributes.Name("Point Boundary Conditions")]
  public class PointBoundaryConditions : GeometricElement
  {
    protected override Type ValueType => typeof(PointBoundaryConditions);

    public PointBoundaryConditions() { }
    public PointBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }

  [Kernel.Attributes.Name("Line Boundary Conditions")]
  public class LineBoundaryConditions : GeometricElement
  {
    protected override Type ValueType => typeof(LineBoundaryConditions);

    public LineBoundaryConditions() { }
    public LineBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }

  [Kernel.Attributes.Name("Area Boundary Conditions")]
  public class AreaBoundaryConditions : GeometricElement
  {
    protected override Type ValueType => typeof(LineBoundaryConditions);

    public AreaBoundaryConditions() { }
    public AreaBoundaryConditions(ARDB.Structure.BoundaryConditions boundaryConditions) : base(boundaryConditions) { }
  }
}

