using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Host")]
  public interface IGH_HostObject : IGH_GeometricElement { }

  [Kernel.Attributes.Name("Host")]
  public class HostObject : GeometricElement, IGH_HostObject
  {
    protected override Type ValueType => typeof(ARDB.HostObject);
    public new ARDB.HostObject Value => base.Value as ARDB.HostObject;

    public HostObject() { }
    protected internal HostObject(ARDB.HostObject host) : base(host) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.HostObject host && !(host.Location is ARDB.LocationPoint) && !(host.Location is ARDB.LocationCurve))
        {
          if (host.GetSketch() is ARDB.Sketch sketch)
          {
            var center = Point3d.Origin;
            var count = 0;
            try
            {
              foreach (var curveArray in sketch.Profile.Cast<ARDB.CurveArray>())
              {
                foreach (var curve in curveArray.Cast<ARDB.Curve>())
                {
                  count++;
                  center += curve.Evaluate(0.0, normalized: true).ToPoint3d();
                  count++;
                  center += curve.Evaluate(1.0, normalized: true).ToPoint3d();
                }
              }
              center /= count;
            }
            catch { }

            var plane = sketch.SketchPlane.GetPlane().ToPlane();
            var origin = count == 0 ? plane.Origin : center;
            var xAxis = plane.XAxis;
            var yAxis = plane.YAxis;

            var hostLevelId = host.LevelId;
            if (hostLevelId == ARDB.ElementId.InvalidElementId)
              hostLevelId = host.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM)?.AsElementId() ?? hostLevelId;

            if (host.Document.GetElement(hostLevelId) is ARDB.Level level)
              origin.Z = level.GetElevation() * Revit.ModelUnits;

            if (host is ARDB.Wall)
            {
              xAxis = -plane.XAxis;
              yAxis = plane.ZAxis;
            }

            if (host is ARDB.FootPrintRoof)
              origin.Z += host.get_Parameter(ARDB.BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;

            if (host is ARDB.ExtrusionRoof)
            {
              origin.Z += host.get_Parameter(ARDB.BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM).AsDouble() * Revit.ModelUnits;
              yAxis = -plane.ZAxis;
            }

            return new Plane(origin, xAxis, yAxis);
          }
        }

        return base.Location;
      }
    }

    public virtual Plane SketchPlane
    {
      get => Location;
    }

    internal bool SetSketchPlane(Plane plane)
    {
      if (Value is ARDB.HostObject host)
      {
        var modified = false;
        var normal = plane.Normal;
        var vertical = normal.EpsilonEquals(Vector3d.ZAxis, Rhino.RhinoMath.ZeroTolerance);
        var horizontal = Math.Abs(normal * Vector3d.ZAxis) < Rhino.RhinoMath.ZeroTolerance;

        if (vertical == Value is ARDB.Wall)
          return false;

        if (Value is ARDB.Wall wallElement && !horizontal)
        {
#if REVIT_2021
          if(wallElement.get_Parameter(ARDB.BuiltInParameter.WALL_CROSS_SECTION).AsEnum<ARDB.WallCrossSection>() == ARDB.WallCrossSection.Tapered)
#endif
            return false;
        }

        if (host.Location is ARDB.LocationCurve locationCurve && locationCurve.Curve is ARDB.Line locationLine)
        {
          if (Intersection.PlanePlane(Level.Location, plane, out var line))
          {
            using (var scope = Document.CommitScope())
            {
              var pinned = host.Pinned;
              var mid0 = locationLine.Evaluate(0.5, normalized: true);
              var mid1 = line.ClosestPoint(mid0.ToPoint3d(), false).ToXYZ();
              if (!mid0.AlmostEqualPoints(mid1))
              {
                host.Pinned = false;
                modified = locationCurve.Move(mid1 - mid0);
              }

              var angle0 = Vector3d.VectorAngle(Vector3d.XAxis, locationLine.Direction.ToVector3d(), Vector3d.ZAxis);
              var angle1 = Vector3d.VectorAngle(Vector3d.XAxis, line.Direction, Vector3d.ZAxis);
              var angle = angle1 - angle0;
              if (Math.Abs(angle) > GeometryTolerance.Internal.DefaultTolerance)
              {
                host.Pinned = false;
                using (var axis = ARDB.Line.CreateUnbound(mid1, UnitXYZ.BasisZ))
                  modified = locationCurve.Rotate(axis, angle);
              }

              if (this is Wall wall)
              {
                var orientation = new Vector3d(normal.X, normal.Y, 0.0); orientation.Unitize();
                var direction = new Vector3d(orientation.Y, -orientation.X, 0.0); direction.Unitize();
                var slantAngle = -Math.Atan2(-direction * Vector3d.CrossProduct(orientation, normal), orientation * normal);
                if (Math.Abs(wall.SlantAngle - slantAngle) > GeometryTolerance.Internal.DefaultTolerance)
                {
                  wall.SlantAngle = slantAngle;
                  modified = true;
                }
              }

              if (modified)
              {
                if (host.Pinned != pinned) host.Pinned = pinned;
                scope.Commit();
                InvalidateGraphics();
              }
            }
          }
        }
        else if (host.Location is ARDB.Location location && vertical)
        {
          using (var scope = Document.CommitScope())
          {
            var pinned = host.Pinned;
            var source = SketchPlane.Origin.ToXYZ();
            var target = plane.Origin.ToXYZ();

            if (!source.AlmostEqualPoints(target))
            {
              host.Pinned = false;
              modified = location.Move(target - source);
            }

            if (modified)
            {
              if (host.Pinned != pinned) host.Pinned = pinned;
              scope.Commit();
              InvalidateGraphics();
            }
          }
        }

        if (modified)
          InvalidateGraphics();
      }

      return true;
    }

    internal bool SetProfile(IList<Curve> boundaries, Vector3d normal)
    {
      if (Value?.GetSketch() is ARDB.Sketch sketch)
      {
        using (var scope = Document.CommitScope())
        {
          if (Sketch.SetProfile(sketch, boundaries, normal))
          {
            InvalidateGraphics();
            scope.Commit();
            return true;
          }
        }
      }

      return false;
    }

    internal bool SetSlabShape(IList<Point3d> points, IList<Line> creases, out IList<Point3d> skipedPoints, out IList<Line> skipedCreases)
    {
      if (Value is ARDB.HostObject host)
      {
        InvalidateGraphics();

        skipedPoints = new List<Point3d>();
        skipedCreases = new List<Line>();

        var shape = host.GetSlabShapeEditor();
        using (shape as IDisposable) // ARDB.SlabShapeEditor is IDisposable since Revit 2023
        {
          shape.ResetSlabShape();
          shape.Enable();
          host.Document.Regenerate();

          var bbox = BoundingBox;
          var elevation = GeometryEncoder.ToInternalLength(bbox.Max.Z);

          var vertices = new Dictionary<Point3d, ARDB.SlabShapeVertex>(shape.SlabShapeVertices.Size + (points?.Count ?? 0) + (creases?.Count ?? 0));
          ARDB.SlabShapeVertex AddVertex(Point3d point)
          {
            var x = GeometryEncoder.ToInternalLength(point.X);
            var y = GeometryEncoder.ToInternalLength(point.Y);
            var z = GeometryEncoder.ToInternalLength(point.Z);

            var xyz = new Point3d(x, y, z);
            if (!vertices.TryGetValue(xyz, out var vertex))
            {
              try
              {
                if ((vertex = shape.AddPoint(new ARDB.XYZ(x, y, elevation))) is object)
                  vertices.Add(xyz, vertex);
              }
              catch { }
            }

            return vertex?.VertexType == ARDB.SlabShapeVertexType.Invalid ? null : vertex;
          }

          if (points is object)
          {
            foreach (var point in points)
            {
              if (AddVertex(point) is null)
                skipedPoints.Add(point);
            }
          }

          if (creases is object)
          {
            foreach (var crease in creases)
            {
              if (crease.IsValid)
              {
                try
                {
                  var from = AddVertex(crease.From);
                  var to = AddVertex(crease.To);
                  if (from is null || to is null || shape.AddSplitLine(from, to) is null)
                    skipedCreases.Add(crease);
                }
                catch { skipedCreases.Add(crease); }
              }
              else skipedCreases.Add(crease);
            }
          }

          var bottomUpVertices = vertices.OrderBy(x => x.Key.Z);
          foreach (var vertex in bottomUpVertices)
            shape.ModifySubElement(vertex.Value, vertex.Key.Z - elevation);
        }

        return true;
      }

      skipedPoints = default;
      skipedCreases = default;
      return false;
    }
  }

  [Kernel.Attributes.Name("Host Type")]
  public interface IGH_HostObjectType : IGH_ElementType { }

  [Kernel.Attributes.Name("Host Type")]
  public class HostObjectType : ElementType, IGH_HostObjectType
  {
    protected override Type ValueType => typeof(ARDB.HostObjAttributes);
    public new ARDB.HostObjAttributes Value => base.Value as ARDB.HostObjAttributes;

    public HostObjectType() { }
    protected internal HostObjectType(ARDB.HostObjAttributes type) : base(type) { }

    public CompoundStructure CompoundStructure
    {
      get => Value is ARDB.HostObjAttributes type ? new CompoundStructure(Document, type.GetCompoundStructure()) : default;
      set
      {
        if (value is object && Value is ARDB.HostObjAttributes type)
          type.SetCompoundStructure(value.Value);
      }
    }
  }
}
