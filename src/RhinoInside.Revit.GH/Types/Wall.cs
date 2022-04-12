using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Wall")]
  public class Wall : HostObject, ISketchAccess, ICurtainGridsAccess
  {
    protected override Type ValueType => typeof(ARDB.Wall);
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

    public override void SetCurve(Curve curve, bool keepJoins = false)
    {
      if (Value is ARDB.Wall wall && curve is object)
      {
        if (wall.Location is ARDB.LocationCurve locationCurve)
        {
          if (!IsValidCurve(curve, out var log))
            throw new Exceptions.RuntimeArgumentException(nameof(curve), log, curve);

          var tol = GeometryObjectTolerance.Model;
          var newCurve = default(ARDB.Curve);
          switch (Curve)
          {
            case LineCurve _:
              if (curve.TryGetLine(out var valueLine, tol.VertexTolerance))
                newCurve = valueLine.ToLine();
              else
                throw new Exceptions.RuntimeArgumentException(nameof(curve), "Curve should be a line like curve.", curve);
              break;

            case ArcCurve _:
              if (curve.TryGetArc(out var valueArc, tol.VertexTolerance))
                newCurve = valueArc.ToArc();
              else
                throw new Exceptions.RuntimeArgumentException(nameof(curve), "Curve should be an arc like curve.", curve);
              break;

            case Curve _:
              if (curve.TryGetEllipse(out var _, tol.VertexTolerance))
                newCurve = curve.ToCurve();
              else
                throw new Exceptions.RuntimeArgumentException(nameof(curve), "Curve should be an ellipse like curve.", curve);
              break;
          }

          if (!locationCurve.Curve.IsAlmostEqualTo(newCurve))
          {
            using (!keepJoins ? ElementJoins.DisableJoinsScope(wall) : default)
              locationCurve.Curve = newCurve;

            InvalidateGraphics();
          }
        }
        else base.SetCurve(curve, keepJoins);
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

          var axis0 = axis.DuplicateCurve();
          axis0.Translate(new Vector3d(0.0, 0.0, domain.T0 - origin.Z));

          var axis1 = axis.DuplicateCurve();
          axis1.Translate(new Vector3d(0.0, 0.0, domain.T1 - origin.Z));

#if REVIT_2021
          if (Value.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL) is ARDB.Parameter slantAngle)
          {
            var angle = slantAngle.AsDouble();
            if (angle != 0.0)
            {
              var offset0 = (domain.T0 - origin.Z) * Math.Tan(angle);
              var offset1 = (domain.T1 - origin.Z) * Math.Tan(angle);

              // We need to use `ARDB.Curve.CreateOffset` to obtain same kind of "offset"
              // else the resulting surface is not parameterized like Revit.
              // This is important to evaluate rectangular openings on that surface.
              var o0 = axis0.ToCurve().CreateOffset(GeometryEncoder.ToInternalLength(offset0), ARDB.XYZ.BasisZ);
              var o1 = axis1.ToCurve().CreateOffset(GeometryEncoder.ToInternalLength(offset1), ARDB.XYZ.BasisZ);

              axis0 = o0.ToCurve();
              axis1 = o1.ToCurve();
            }
          }
#endif

          if (NurbsSurface.CreateRuledSurface(axis0, axis1) is Surface surface)
          {
            surface.SetDomain(0, new Interval(0.0, axis.GetLength()));
            surface.SetDomain(1, domain);
            return surface;
          }
        }

        return null;
      }
    }

    //public override Brep PolySurface
    //{
    //  get
    //  {
    //    if (Value?.CurtainGrid is ARDB.CurtainGrid grid && Surface is Surface surface)
    //    {
    //      var loops = grid.GetCurtainCells().SelectMany(x => x.CurveLoops.ToCurveMany()).ToArray();
    //      return surface.CreateTrimmedSurface(loops, GeometryObjectTolerance.Model.VertexTolerance);
    //    }

    //    return base.PolySurface;
    //  }
    //}
    #endregion

    #region ISketchAccess
    public Sketch Sketch => Value is ARDB.Wall wall ?
      new Sketch(wall.GetSketch()) : default;
    #endregion

    #region ICurtainGridsAccess
    public IList<CurtainGrid> CurtainGrids => Value is ARDB.Wall wall && wall.CurtainGrid is ARDB.CurtainGrid grid?
      new CurtainGrid[] { new CurtainGrid(wall, grid) } : default;
    #endregion

    #region Joins
    public bool? IsJoinAllowedAtStart
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

    public bool? IsJoinAllowedAtEnd
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
