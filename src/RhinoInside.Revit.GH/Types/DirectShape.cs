using System;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Direct Shape")]
  public class DirectShape : GeometricElement
  {
    protected override Type ScriptVariableType => typeof(DB.DirectShape);
    public new DB.DirectShape Value => base.Value as DB.DirectShape;

    public DirectShape() { }
    public DirectShape(DB.DirectShape value) : base(value) { }
  }

  [Kernel.Attributes.Name("Direct Shape Type")]
  public class DirectShapeType : ElementType
  {
    protected override Type ScriptVariableType => typeof(DB.DirectShapeType);
    public new DB.DirectShapeType Value => base.Value as DB.DirectShapeType;

    public DirectShapeType() { }
    public DirectShapeType(DB.DirectShapeType value) : base(value) { }
  }
}
