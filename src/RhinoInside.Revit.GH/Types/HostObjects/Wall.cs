using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

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
    public Wall(ARDB.Wall wall) : base(wall) { }

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
          var perp = axis.RightDirection(GeometryDecoder.Tolerance.DefaultTolerance);
          return new Plane(origin, axis, perp);
        }

        return base.Location;
      }
    }

    public override Vector3d FacingOrientation => Value?.Flipped == true ? -Location.YAxis : Location.YAxis;

    public static bool IsValidCurve(Curve curve, out string log)
    {
      if (!curve.IsValidWithLog(out log)) return false;

      var tol = GeometryTolerance.Model;
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

          var tol = GeometryTolerance.Model;
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

          if (!locationCurve.Curve.AlmostEquals(newCurve, GeometryTolerance.Internal.VertexTolerance))
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
              // This is important to evaluate rectangular Opening and WallSweep on that surface.
              var o0 = axis0.ToCurve().CreateOffset(GeometryEncoder.ToInternalLength(offset0), ERDB.UnitXYZ.BasisZ);
              var o1 = axis1.ToCurve().CreateOffset(GeometryEncoder.ToInternalLength(offset1), ERDB.UnitXYZ.BasisZ);

              axis0 = o0.ToCurve();
              axis1 = o1.ToCurve();
            }
          }
#endif

          if (NurbsSurface.CreateRuledSurface(axis0, axis1) is Surface surface)
          {
            surface.SetDomain(0, axis.Domain);
            surface.SetDomain(1, new Interval(domain.T0 - origin.Z, domain.T1 - origin.Z));
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
    public IList<CurtainGrid> CurtainGrids => Value is ARDB.Wall wall ?
      wall.CurtainGrid is ARDB.CurtainGrid grid ?
      new CurtainGrid[] { new CurtainGrid(this, grid, 0) } :
      new CurtainGrid[] { } :
      default;
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

  [Kernel.Attributes.Name("Wall Sweep")]
  public class WallSweep : HostObject, IHostObjectAccess
  {
    protected override Type ValueType => typeof(ARDB.WallSweep);
    public new ARDB.WallSweep Value => base.Value as ARDB.WallSweep;

    public HostObject Host => Value is ARDB.WallSweep wallSweep ?
      HostObject.FromElementId(Document, wallSweep.GetHostIds().FirstOrDefault() ?? ElementIdExtension.InvalidElementId) as HostObject :
      default;

    public WallSweep() { }
    public WallSweep(ARDB.WallSweep wallSweep) : base(wallSweep) { }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      using (var info = Value?.GetWallSweepInfo())
      {
        if (info?.WallSweepType == ARDB.WallSweepType.Reveal)
        {
          if (PolySurface is Brep brep)
            args.Pipeline.DrawBrepWires(brep, args.Color, 0);
          return;
        }
      }

      base.DrawViewportWires(args);
    }

    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      using (var info = Value?.GetWallSweepInfo())
      {
        if (info?.WallSweepType == ARDB.WallSweepType.Reveal)
        {
          //if (PolySurface is Brep brep)
          //  args.Pipeline.DrawBrepShaded(brep, args.Material);

          return;
        }
      }

      base.DrawViewportMeshes(args);
    }
    #endregion

    #region Geometry
    protected override void ResetValue()
    {
      using (_PolySurface) _PolySurface = null;
      base.ResetValue();
    }

    public override Plane Location
    {
      get
      {
        //if (Curve is Curve curve)
        //{
        //  var start = curve.PointAtStart;
        //  var end = curve.PointAtEnd;
        //  var axis = end - start;
        //  var origin = start + (axis * 0.5);
        //  var perp = axis.PerpVector();
        //  return new Plane(origin, axis, perp);
        //}

        return base.Location;
      }
    }

    //public override Curve Curve
    //{
    //  get
    //  {
    //    using (var info = Value?.GetWallSweepInfo())
    //    {
    //      var hostIds = Value.GetHostIds();
    //      var runs = new List<Curve>(hostIds.Count);
    //      foreach(var wall in hostIds.Select(x => Wall.FromElementId(Document, x) as Wall))
    //      {
    //        var surface = wall.Surface;
    //        var direction0 = info.IsVertical ? 1 : 0;
    //        var direction1 = info.IsVertical ? 0 : 1;
    //        var domain1 = surface.Domain(direction1);
    //        var t = double.NaN;

    //        switch (info.DistanceMeasuredFrom)
    //        {
    //          case ARDB.DistanceMeasuredFrom.Base:
    //            if (info.IsVertical) t = /*domain1.T0 + */info.Distance;
    //            else t = Level.Elevation + info.Distance * Revit.ModelUnits;
    //            break;

    //          case ARDB.DistanceMeasuredFrom.Top:
    //            if (info.IsVertical) t = /*domain1.T1 - */info.Distance;
    //            else t = domain1.T1 - info.Distance * Revit.ModelUnits;
    //            break;
    //        }

    //        var iso = surface.IsoCurve(direction0, t);

    //        using (var structure = wall.Value.WallType.GetCompoundStructure())
    //        {
    //          var width = structure.GetWidth();
    //          var offset = 0.0;
    //          var direction = 0.0;

    //          switch (info.WallSweepType)
    //          {
    //            case ARDB.WallSweepType.Sweep:  direction= +1.0; break;
    //            case ARDB.WallSweepType.Reveal: direction = -1.0; break;
    //          }

    //          // There is a bug on Revit 2023 and always report as Exterior.
    //          // TODO : Find an alternative way of know the WallSide.
    //          switch (info.WallSide)
    //          {
    //            case ARDB.WallSide.Exterior: offset = (width * -0.5) + (direction * info.WallOffset); break;
    //            case ARDB.WallSide.Interior: offset = (width * +0.5) - (direction * info.WallOffset); break;
    //          }

    //          if (info.IsVertical)
    //          {
    //            var point = iso.PointAt(iso.Domain.Mid);
    //            surface.ClosestPoint(point, out var u, out var v);
    //            var normal = surface.NormalAt(u, v);
    //            normal.Unitize();
    //            iso.Translate(normal * offset * Revit.ModelUnits);
    //          }
    //          else
    //          {
    //            var point = iso.PointAt(iso.Domain.Mid);
    //            surface.ClosestPoint(point, out var u, out var v);
    //            surface.FrameAt(u, v, out var frame);

    //            //iso = iso.Offset
    //            //(
    //            //  new Plane(frame.Origin, frame.XAxis, frame.ZAxis),
    //            //  offset * Revit.ModelUnits,
    //            //  GeometryTolerance.Model.VertexTolerance,
    //            //  CurveOffsetCornerStyle.None
    //            //)[0];

    //            iso = iso.ToCurve().CreateOffset(offset, XYZExtension.BasisZ).ToCurve();
    //          }
    //        }

    //        runs.Add(iso);
    //      }

    //      if (runs.Count > 0)
    //      {
    //        if (runs.Count == 1)
    //          return runs[0];

    //        // TODO : runs do intersect each other, we need to cut-extend each other before join them.
    //        var polycurve = new PolyCurve();
    //        foreach (var run in runs)
    //          polycurve.AppendSegment(run);

    //        return polycurve;
    //      }
    //    }

    //    return default;
    //  }
    //}

    Brep _PolySurface;
    public override Brep PolySurface
    {
      get
      {
        if (Value is ARDB.WallSweep sweep)
        {
          if (_PolySurface is null)
          {
            using (var options = new ARDB.Options() { IncludeNonVisibleObjects = true })
            {
              using (var geometry = sweep.get_Geometry(options))
              {
                var breps = new List<Brep>();
                foreach (var g in geometry)
                {
                  if (g is ARDB.Solid solid)
                    breps.Add(solid.ToBrep());

                  g.Dispose();
                }

                if (breps.Count > 0)
                {
                  var brep = Brep.MergeBreps(breps, Rhino.RhinoMath.UnsetValue);
                  if (!brep.IsValid) brep.Repair(GeometryTolerance.Model.VertexTolerance);
                  using (var info = sweep.GetWallSweepInfo())
                    if (info.WallSweepType == ARDB.WallSweepType.Reveal) brep.Flip();

                  _PolySurface = brep;
                }
              }
            }
          }
        }

        return _PolySurface;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Wall Foundation")]
  public class WallFoundation : HostObject, IHostObjectAccess
  {
    protected override Type ValueType => typeof(ARDB.WallFoundation);
    public new ARDB.WallFoundation Value => base.Value as ARDB.WallFoundation;

    public HostObject Host => Value is ARDB.WallFoundation wallFoundation?
      HostObject.FromElementId(Document, wallFoundation.WallId) as HostObject:
      default;

    public WallFoundation() { }
    public WallFoundation(ARDB.WallFoundation wallFoundation) : base(wallFoundation) { }
  }
}
