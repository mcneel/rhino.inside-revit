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

    public override string DisplayName => Value is ARDB.SpatialElement element ?
      $"{Name} {Number}{(element.Location is null ? " (Unplaced)" : string.Empty)}" : base.DisplayName;

    #region Location
    public override Plane Location => Value?.Location is ARDB.LocationPoint point ?
        new Plane(point.Point.ToPoint3d(), Vector3d.XAxis, Vector3d.YAxis) :
        NaN.Plane;

    public override BoundingBox GetBoundingBox(Transform xform) => IsPlaced ?
      base.GetBoundingBox(xform) : NaN.BoundingBox;

    public override ARDB.ElementId LevelId => Value?.Level?.Id ?? ARDB.ElementId.InvalidElementId;

    public Curve[] Boundaries
    {
      get
      {
        if (Value is ARDB.SpatialElement spatial)
        {
          using (var options = new ARDB.SpatialElementBoundaryOptions())
          {
            options.StoreFreeBoundaryFaces = true;
            options.SpatialElementBoundaryLocation = spatial is ARDB.Area ?
              ARDB.SpatialElementBoundaryLocation.Center :
              ARDB.SpatialElementBoundaryLocation.Finish;

            var tol = GeometryTolerance.Model;
            var plane = Location;
            {
              var ComputationHeight = Value.get_Parameter(ARDB.BuiltInParameter.ROOM_COMPUTATION_HEIGHT).AsDouble() * Revit.ModelUnits;
              plane.Origin = new Point3d(plane.OriginX, plane.OriginY, plane.OriginZ + ComputationHeight);
            }

            var segments = spatial.GetBoundarySegments(options);
            return segments.Select
            (
              loop => Curve.JoinCurves(loop.Select(x => Curve.ProjectToPlane(x.GetCurve().ToCurve(), plane)), tol.VertexTolerance, preserveDirection: false)[0]
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
          if (loops.Length > 0)
          {
            var plane = Location;
            {
              var ComputationHeight = Value.get_Parameter(ARDB.BuiltInParameter.ROOM_COMPUTATION_HEIGHT).AsDouble() * Revit.ModelUnits;
              plane.Origin = new Point3d(plane.OriginX, plane.OriginY, plane.OriginZ + ComputationHeight);
            }

            var loopsBox = BoundingBox.Empty;
            for (int l = 0; l< loops.Length; ++l)
            {
              if (loops[l].ClosedCurveOrientation(plane) == CurveOrientation.Clockwise)
                loops[l].Reverse();

              loopsBox.Union(loops[l].GetBoundingBox(plane));
            }

            var planeSurface = new PlaneSurface
            (
              plane,
              new Interval(loopsBox.Min.X, loopsBox.Max.X),
              new Interval(loopsBox.Min.Y, loopsBox.Max.Y)
            );

            return planeSurface.CreateTrimmedSurface(loops, GeometryTolerance.Model.VertexTolerance);
          }
        }

        return null;
      }
    }
    #endregion

    #region Properties
    public string Number => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER)?.AsString();
    public string Name => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME)?.AsString();
    public Phase Phase => Value is ARDB.SpatialElement element ?
      Phase.FromElementId(element.Document, element.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE)?.AsElementId()) as Phase : default;

    public bool IsPlaced => Value?.Location is object;
    public bool IsEnclosed => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_PERIMETER)?.HasValue == true;

    public double? Perimeter
    {
      get
      {
        if (Value is ARDB.SpatialElement element && Value.get_Parameter(ARDB.BuiltInParameter.ROOM_PERIMETER) is ARDB.Parameter roomPerimeter)
        {
          if (roomPerimeter.HasValue)
            return roomPerimeter.AsDouble() * Revit.ModelUnits;
        }

        return default;
      }
    }

    public double? Area
    {
      get
      {
        if (Value is ARDB.SpatialElement element && Value.get_Parameter(ARDB.BuiltInParameter.ROOM_AREA) is ARDB.Parameter roomArea)
        {
          if (roomArea.HasValue)
            return roomArea.AsDouble() * Revit.ModelUnits * Revit.ModelUnits;
        }

        return default;
      }
    }

    public double? Volume
    {
      get
      {
        if (Value is ARDB.SpatialElement element && Value.get_Parameter(ARDB.BuiltInParameter.ROOM_VOLUME) is ARDB.Parameter roomVolume)
        {
          if (roomVolume.HasValue)
            return roomVolume.AsDouble() * Revit.ModelUnits * Revit.ModelUnits * Revit.ModelUnits;
        }

        return default;
      }
    }
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
          return Brep.MergeBreps(solids.Select(x => x.ToBrep()), GeometryTolerance.Model.VertexTolerance);
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
          return Brep.MergeBreps(solids.Select(x => x.ToBrep()), GeometryTolerance.Model.VertexTolerance);
        }

        return null;
      }
    }
    #endregion
  }
}
