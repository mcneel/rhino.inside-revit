using System;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;
using Rhino.Geometry;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Dimension")]
  public class Dimension : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.Dimension);
    public static explicit operator DB.Dimension(Dimension value) => value?.Value;
    public new DB.Dimension Value => base.Value as DB.Dimension;

    public Dimension() { }
    public Dimension(DB.Dimension dimension) : base(dimension) { }

    public override Plane Location
    {
      get
      {
        if (Value is DB.Dimension dimension)
        {
          if (dimension.Curve.Project(dimension.Origin) is DB.IntersectionResult result)
          {
            var transform = dimension.Curve.ComputeDerivatives(result.Parameter, false);
            var origin = transform.Origin.ToPoint3d();
            var xAxis = transform.BasisX.ToVector3d();
            var yAxis = transform.BasisY.ToVector3d();
            if (yAxis.IsZero) yAxis = xAxis.PerpVector();

            return new Plane(origin, xAxis, yAxis);
          }
        }

        return base.Location;
      }
    }

    public override Curve Curve => Value?.Curve?.ToCurve();
  }
}
