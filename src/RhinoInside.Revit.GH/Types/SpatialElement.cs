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
    public override Level Level => Level.FromElement(Value?.Level) as Level;

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

  [Kernel.Attributes.Name("Area")]
  public class AreaElement : SpatialElement
  {
    protected override Type ValueType => typeof(ARDB.Area);
    public new ARDB.Area Value => base.Value as ARDB.Area;

    public AreaElement() { }
    public AreaElement(ARDB.Area element) : base(element) { }
  }

  [Kernel.Attributes.Name("Room")]
  public class RoomElement : SpatialElement
  {
    protected override Type ValueType => typeof(ARDB.Architecture.Room);
    public new ARDB.Architecture.Room Value => base.Value as ARDB.Architecture.Room;

    public RoomElement() { }
    public RoomElement(ARDB.Architecture.Room element) : base(element) { }

    #region Location
    public override Brep PolySurface
    {
      get
      {
        if (Value is ARDB.Architecture.Room room)
        {
          var solids = room.ClosedShell.OfType<ARDB.Solid>().Where(x => x.Faces.Size > 0);
          return Brep.MergeBreps(solids.Select(x => x.ToBrep()), GeometryObjectTolerance.Model.VertexTolerance);
        }

        return null;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Space")]
  public class SpaceElement : SpatialElement
  {
    protected override Type ValueType => typeof(ARDB.Mechanical.Space);
    public new ARDB.Mechanical.Space Value => base.Value as ARDB.Mechanical.Space;

    public SpaceElement() { }
    public SpaceElement(ARDB.Mechanical.Space element) : base(element) { }

    #region Location
    public override Brep PolySurface
    {
      get
      {
        if (Value is ARDB.Mechanical.Space space)
        {
          var solids = space.ClosedShell.OfType<ARDB.Solid>().Where(x => x.Faces.Size > 0);
          return Brep.MergeBreps(solids.Select(x => x.ToBrep()), GeometryObjectTolerance.Model.VertexTolerance);
        }

        return null;
      }
    }
    #endregion
  }
}
