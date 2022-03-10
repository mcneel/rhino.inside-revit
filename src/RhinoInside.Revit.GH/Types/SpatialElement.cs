using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Spatial Element")]
  public class SpatialElement : InstanceElement
  {
    protected override Type ValueType => typeof(ARDB.SpatialElement);
    public new ARDB.SpatialElement Value => base.Value as ARDB.SpatialElement;

    public SpatialElement() { }
    public SpatialElement(ARDB.SpatialElement element) : base(element) { }

    public override string DisplayName => Value is object ? $"{Name} - {Number}" : base.DisplayName;

    #region Lolcation
    public Curve[] Boundaries
    {
      get
      {
        if (Value is ARDB.SpatialElement spatial)
        {
          using (var options = new ARDB.SpatialElementBoundaryOptions())
          {
            var tol = GeometryObjectTolerance.Model;
            return spatial.GetBoundarySegments(options).Select
            (
              loop => Curve.JoinCurves(loop.Select(x => x.GetCurve().ToCurve()), tol.VertexTolerance)[0]
            ).ToArray();
          }
        }

        return null;
      }
    }

    public override Brep TrimmedSurface
    {
      get
      {
        if (Boundaries is Curve[] loops)
        {
          var plane = Location;
          if (loops.Length > 0)
          {
            var loopsBox = BoundingBox.Empty;
            foreach (var loop in loops)
            {
              if (loop.ClosedCurveOrientation(plane) == CurveOrientation.Clockwise)
                loop.Reverse();

              loopsBox.Union(loop.GetBoundingBox(plane));
            }

            var planeSurface = new PlaneSurface
            (
              plane,
              new Interval(loopsBox.Min.X, loopsBox.Max.X),
              new Interval(loopsBox.Min.Y, loopsBox.Max.Y)
            );

            return planeSurface.CreateTrimmedSurface(loops, GeometryObjectTolerance.Model.VertexTolerance);
          }
        }

        return null;
      }
    }
    #endregion

    #region Properties
    public string Number => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER)?.AsString();
    public string Name => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME)?.AsString();
    #endregion
  }
}
