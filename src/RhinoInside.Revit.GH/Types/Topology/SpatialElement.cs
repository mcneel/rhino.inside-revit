using System;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Spatial Element")]
  public class SpatialElement : GeometricElement
  {
    protected override Type ValueType => typeof(ARDB.SpatialElement);
    public new ARDB.SpatialElement Value => base.Value as ARDB.SpatialElement;

    public SpatialElement() { }
    public SpatialElement(ARDB.SpatialElement element) : base(element) { }

    public override string DisplayName => Value is ARDB.SpatialElement element ?
      $"{Name} {Number}{(element.Location is null ? " (Unplaced)" : string.Empty)}" : base.DisplayName;

    #region Location
    public override Point3d Position => Value?.Location is ARDB.LocationPoint point ?
        point.Point.ToPoint3d() :
        NaN.Point3d;

    public override Plane Location => Value?.Location is ARDB.LocationPoint point ?
        new Plane(point.Point.ToPoint3d(), Vector3d.XAxis, Vector3d.YAxis) :
        NaN.Plane;

    public override BoundingBox GetBoundingBox(Transform xform) => IsPlaced ?
      base.GetBoundingBox(xform) : NaN.BoundingBox;

    public override ARDB.ElementId LevelId => Value?.Level?.Id ?? ARDB.ElementId.InvalidElementId;

    protected override void SubInvalidateGraphics()
    {
      _Boundaries = null;
      base.SubInvalidateGraphics();
    }

    Curve[] _Boundaries;
    public Curve[] Boundaries
    {
      get
      {
        if (_Boundaries is null && Value is ARDB.SpatialElement spatial)
        {
          if (IsPlaced && IsEnclosed)
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
                plane.Origin = new Point3d(plane.OriginX, plane.OriginY, plane.OriginZ + ComputationOffset.Value);
              }

              var segments = spatial.GetBoundarySegments(options);
              _Boundaries = segments.Select
              (
                loop => Curve.JoinCurves(loop.Select(x => Curve.ProjectToPlane(x.GetCurve().ToCurve(), plane)), tol.VertexTolerance, preserveDirection: false)[0]
              ).ToArray();
            }
          }
          else _Boundaries = Array.Empty<Curve>();
        }

        return _Boundaries;
      }
    }

    public override Brep TrimmedSurface
    {
      get
      {
        if (Boundaries is Curve[] loops && loops.Length > 0)
        {
          var plane = Location;
          {
            plane.Origin = new Point3d(plane.OriginX, plane.OriginY, plane.OriginZ + ComputationOffset.Value);
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

        return null;
      }
    }
    #endregion

    #region Properties
    public string Number => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_NUMBER)?.AsString();
    public string Name => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_NAME)?.AsString();
    public Phase Phase => GetElement<Phase>(Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_PHASE)?.AsElementId());

    public bool IsPlaced => Value?.Location is object;
    public bool IsEnclosed => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_PERIMETER)?.HasValue == true;

    protected virtual double? ComputationOffset => Value?.get_Parameter(ARDB.BuiltInParameter.ROOM_COMPUTATION_HEIGHT).AsDouble() * Revit.ModelUnits;

    public double? Perimeter
    {
      get
      {
        if (Value is ARDB.SpatialElement element && element.get_Parameter(ARDB.BuiltInParameter.ROOM_PERIMETER) is ARDB.Parameter roomPerimeter)
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
        if (Value is ARDB.SpatialElement element && element.get_Parameter(ARDB.BuiltInParameter.ROOM_AREA) is ARDB.Parameter roomArea)
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
        if (Value is ARDB.SpatialElement element && element.get_Parameter(ARDB.BuiltInParameter.ROOM_VOLUME) is ARDB.Parameter roomVolume)
        {
          if (roomVolume.HasValue)
            return roomVolume.AsDouble() * Revit.ModelUnits * Revit.ModelUnits * Revit.ModelUnits;
        }

        return default;
      }
    }
    #endregion
  }
}
