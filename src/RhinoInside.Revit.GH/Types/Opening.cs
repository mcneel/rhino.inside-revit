using System;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Opening")]

  public class Opening : InstanceElement, ISketchAccess, IHostObjectAccess
  {
    protected override Type ValueType => typeof(ARDB.Opening);
    public new ARDB.Opening Value => base.Value as ARDB.Opening;

    public Opening() { }
    public Opening(ARDB.Opening host) : base(host) { }

    #region Location
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (!xform.IsIdentity)
      {
        var boundaries = Boundaries;
        if (boundaries?.Length > 0)
        {
          var bbox = BoundingBox.Empty;

          foreach (var boundary in boundaries)
            bbox.Union(boundary.GetBoundingBox(xform));

          return bbox;
        }
      }

      return base.GetBoundingBox(xform);
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Opening opening)
        {
          if (opening.IsRectBoundary)
          {
            var rect = opening.BoundaryRect;
            var p0 = rect[0].ToPoint3d();
            var p1 = rect[1].ToPoint3d();
            var p2 = new Point3d(p0.X, p0.Y, p1.Z);
            var p3 = new Point3d(p1.X, p1.Y, p0.Z);

            return new Plane(p0 + ((p1 - p0) * 0.5), p3 - p0, p2 - p0);
          }
        }

        return Sketch?.ProfilesPlane ?? NaN.Plane;
      }
    }

    /// <summary>
    /// Planar profiles
    /// </summary>
    public Curve[] Profiles
    {
      get
      {
        var profiles = default(Curve[]);
        if (Value is ARDB.Opening opening)
        {
          if (opening.IsRectBoundary)
          {
            var rect = opening.BoundaryRect;
            var p0 = rect[0].ToPoint3d();
            var p1 = rect[1].ToPoint3d();
            var p2 = new Point3d(p0.X, p0.Y, p1.Z);
            var p3 = new Point3d(p1.X, p1.Y, p0.Z);

            profiles = new Curve[] { new PolylineCurve(new Point3d[] { p0, p2, p1, p3, p0 }) };
          }
          else profiles = opening.BoundaryCurves.ToCurves();

          var plane = Location;
          foreach (var profile in profiles)
          {
            if (!profile.IsClosed) continue;
            if (profile.ClosedCurveOrientation(plane) == CurveOrientation.Clockwise)
              profile.Reverse();
          }
        }

        return profiles;
      }
    }

    /// <summary>
    /// Boundary of the opening on the host surface
    /// </summary>
    public Curve[] Boundaries
    {
      get
      {
        if (Value is ARDB.Opening opening)
        {
          if (opening.IsRectBoundary)
          {
            if (Host?.Surface is Surface surface)
            {
              var p0 = opening.BoundaryRect[0].ToPoint3d();
              var p1 = opening.BoundaryRect[1].ToPoint3d();
              var location = surface.IsoCurve(0, Host.Location.Origin.Z);

              if
              (
                location.ClosestPoint(p0, out var u0) &&
                location.ClosestPoint(p1, out var u1)
              )
              {
                var v0 = p0.Z;
                var v1 = p1.Z;

                var u = new Interval(Math.Min(u0, u1), Math.Max(u0, u1));
                var v = new Interval(Math.Min(v0, v1), Math.Max(v0, v1));

                var south = surface.IsoCurve(0, v.T0).Trim(u);
                var east = surface.IsoCurve(1, u.T1).Trim(v);
                var north = surface.IsoCurve(0, v.T1).Trim(u);
                var west = surface.IsoCurve(1, u.T0).Trim(v);

                if (south is object && east is object && north is object && west is object)
                {
                  north.Reverse();
                  west.Reverse();

                  var curve = new PolyCurve();
                  curve.AppendSegment(south);
                  curve.AppendSegment(east);
                  curve.AppendSegment(north);
                  curve.AppendSegment(west);
                  curve.MakeClosed(GeometryObjectTolerance.Model.VertexTolerance);

                  return new Curve[] { curve };
                }
              }
            }
          }
          else return opening.BoundaryCurves.ToCurves();
        }

        return null;
      }
    }

    public override Brep PolySurface
    {
      get
      {
        if (Value is ARDB.Opening opening)
        {
          if (Sketch.FromElement(opening.GetSketch()) is Sketch sketch)
            return sketch.TrimmedSurface;
        }

        return Host?.Surface?.CreateTrimmedSurface(Boundaries, GeometryObjectTolerance.Model.VertexTolerance);
      }
    }
    #endregion

    #region ISketchAccess
    public Sketch Sketch => Value is ARDB.Opening opening ? new Sketch(opening.GetSketch()) : default;
    #endregion

    #region IHostObjectAccess
    public HostObject Host => Value is ARDB.Opening opening ?
      HostObject.FromElement(opening.Host) as HostObject : default;
    #endregion
  }
}
