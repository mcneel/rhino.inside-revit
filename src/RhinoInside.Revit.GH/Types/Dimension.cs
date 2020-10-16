using System;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class Dimension : InstanceElement
  {
    public override string TypeDescription => "Represents a Revit Dimension";
    protected override Type ScriptVariableType => typeof(DB.Dimension);
    public static explicit operator DB.Dimension(Dimension value) => value?.Value;
    public new DB.Dimension Value => base.Value as DB.Dimension;

    public Dimension() { }
    public Dimension(DB.Dimension dimension) : base(dimension) { }

    public override Rhino.Geometry.Curve Curve => Value?.Curve?.ToCurve();
  }
}
