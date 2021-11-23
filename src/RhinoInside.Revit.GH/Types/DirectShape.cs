using System;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Direct Shape")]
  public class DirectShape : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.DirectShape);
    public new ARDB.DirectShape Value => base.Value as ARDB.DirectShape;

    public DirectShape() { }
    public DirectShape(ARDB.DirectShape value) : base(value) { }
  }

  [Kernel.Attributes.Name("Direct Shape Type")]
  public class DirectShapeType : ElementType
  {
    protected override Type ValueType => typeof(ARDB.DirectShapeType);
    public new ARDB.DirectShapeType Value => base.Value as ARDB.DirectShapeType;

    public DirectShapeType() { }
    public DirectShapeType(ARDB.DirectShapeType value) : base(value) { }
  }
}
