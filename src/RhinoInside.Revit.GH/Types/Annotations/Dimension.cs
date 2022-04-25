using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Dimension")]
  public class Dimension : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Dimension);
    public static explicit operator ARDB.Dimension(Dimension value) => value?.Value;
    public new ARDB.Dimension Value => base.Value as ARDB.Dimension;

    public Dimension() { }
    public Dimension(ARDB.Dimension dimension) : base(dimension) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Dimension dimension && dimension.Curve is ARDB.Curve curve)
        {
          if (curve.Project(dimension.Origin) is ARDB.IntersectionResult result)
          {
            var transform = curve.ComputeDerivatives(result.Parameter, false);
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

    public override Curve Curve
    {
      get
      {
        if (Value is ARDB.Dimension dimension && dimension.Curve is ARDB.Curve curve)
        {
          try
          {
            if (!curve.IsBound && dimension.Value.HasValue)
            {
              if (dimension.Curve.Project(dimension.Origin) is ARDB.IntersectionResult result)
              {
                var startParameter = dimension.Value.Value * -0.5;
                var endParameter = dimension.Value.Value * +0.5;
                curve.MakeBound(result.Parameter + startParameter, result.Parameter + endParameter);
              }
            }
          }
          catch { }

          return curve.ToCurve();

        }

        return default;
      }
    }
  }
}
