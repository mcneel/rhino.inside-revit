using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  [Kernel.Attributes.Name("Wall")]
  public class Wall : HostObject
  {
    protected override Type ValueType => typeof(ARDB.Wall);
    public static explicit operator ARDB.Wall(Wall value) => value?.Value;
    public new ARDB.Wall Value => base.Value as ARDB.Wall;

    public Wall() { }
    public Wall(ARDB.Wall host) : base(host) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value?.Location is ARDB.LocationCurve curveLocation)
        {
          var start = curveLocation.Curve.Evaluate(0.0, normalized: true).ToPoint3d();
          var end = curveLocation.Curve.Evaluate(1.0, normalized: true).ToPoint3d();
          var axis = end - start;
          var origin = start + (axis * 0.5);
          var perp = axis.PerpVector();
          return new Plane(origin, axis, perp);
        }

        return base.Location;
      }
    }

    public override Vector3d FacingOrientation => Value?.Flipped == true ? -Location.YAxis : Location.YAxis;

    public static bool IsValidCurve(Curve curve, out string log)
    {
      if (!curve.IsValidWithLog(out log)) return false;

      var tol = GeometryObjectTolerance.Model;
#if REVIT_2020
      if
      (
        !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance) || curve.IsEllipse(tol.VertexTolerance)) ||
        !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
        axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
      {
        log = "Curve should be a horizontal line, arc or ellipse curve.";
        return false;
      }
#else
      if
      (
        !(curve.IsLinear(tol.VertexTolerance) || curve.IsArc(tol.VertexTolerance)) ||
        !curve.TryGetPlane(out var axisPlane, tol.VertexTolerance) ||
        axisPlane.ZAxis.IsParallelTo(Vector3d.ZAxis) == 0
      )
      {
        log = "Curve should be a horizontal line or arc curve.";
        return false;
      }
#endif

      return true;
    }

    public override Curve Curve
    {
      set
      {
        if (value is object && Value is ARDB.Wall wall && wall.Location is ARDB.LocationCurve)
        {
          if (!IsValidCurve(value, out var log))
            throw new Exceptions.RuntimeArgumentException(nameof(Curve), log, value);

          var tol = GeometryObjectTolerance.Model;

          switch (Curve)
          {
            case LineCurve _:
              if (value.TryGetLine(out var valueLine, tol.VertexTolerance))
                base.Curve = new LineCurve(valueLine);
              else
                throw new Exceptions.RuntimeArgumentException(nameof(Curve), "Curve should be a line like curve.", value);
              break;

            case ArcCurve _:
              if (value.TryGetArc(out var valueArc, tol.VertexTolerance))
                base.Curve = new ArcCurve(valueArc);
              else
                throw new Exceptions.RuntimeArgumentException(nameof(Curve), "Curve should be an arc like curve.", value);
              break;

            case NurbsCurve _:
              if (value.TryGetEllipse(out var _, tol.VertexTolerance))
                base.Curve = value;
              else
                throw new Exceptions.RuntimeArgumentException(nameof(Curve), "Curve should be an ellipse like curve.", value);
              break;
          }
        }
      }
    }

    public override Surface Surface
    {
      get
      {
        if (Curve is Curve axis)
        {
          var location = Location;
          var origin = location.Origin;
          var domain = Domain;

          var p0 = new Plane(new Point3d(origin.X, origin.Y, domain.T0), Vector3d.ZAxis);
          var p1 = new Plane(new Point3d(origin.X, origin.Y, domain.T1), Vector3d.ZAxis);

          var axis0 = Curve.ProjectToPlane(axis, p0);
          var axis1 = Curve.ProjectToPlane(axis, p1);

#if REVIT_2021
          if (Value.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL) is ARDB.Parameter slantAngle)
          {
            var angle = slantAngle.AsDouble();
            if (angle > 0.0)
            {
              var offset0 = (domain.T0 - origin.Z) * Math.Tan(angle);
              var offset1 = (domain.T1 - origin.Z) * Math.Tan(angle);

              var edge0 = axis0.Offset(p0, offset0, GeometryObjectTolerance.Model.VertexTolerance, CurveOffsetCornerStyle.Smooth);
              var edge1 = axis1.Offset(p1, offset1, GeometryObjectTolerance.Model.VertexTolerance, CurveOffsetCornerStyle.Smooth);

              axis0 = edge0?.Length == 1 ? edge0[0].Fit(3, GeometryObjectTolerance.Model.VertexTolerance, GeometryObjectTolerance.Model.AngleTolerance) : axis0;
              axis1 = edge1?.Length == 1 ? edge1[0].Fit(3, GeometryObjectTolerance.Model.VertexTolerance, GeometryObjectTolerance.Model.AngleTolerance) : axis1;
            }
          }
#endif
          return NurbsSurface.CreateRuledSurface(axis0, axis1);
        }

        return base.Surface;
      }
    }
    #endregion

    #region Joins
    public override bool? IsJoinAllowedAtStart
    {
      get =>  Value is ARDB.Wall wall ?
        ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 0) :
        default;

      set
      {
        if (value is object && Value is ARDB.Wall wall && value != IsJoinAllowedAtEnd)
        {
          InvalidateGraphics();

          if (value == true)
            ARDB.WallUtils.AllowWallJoinAtEnd(wall, 0);
          else
            ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 0);
        }
      }
    }

    public override bool? IsJoinAllowedAtEnd
    {
      get => Value is ARDB.Wall wall ?
        ARDB.WallUtils.IsWallJoinAllowedAtEnd(wall, 1) :
        default;

      set
      {
        if (value is object && Value is ARDB.Wall wall && value != IsJoinAllowedAtEnd)
        {
          InvalidateGraphics();

          if (value == true)
            ARDB.WallUtils.AllowWallJoinAtEnd(wall, 1);
          else
            ARDB.WallUtils.DisallowWallJoinAtEnd(wall, 1);
        }
      }
    }
    #endregion
  }
}
