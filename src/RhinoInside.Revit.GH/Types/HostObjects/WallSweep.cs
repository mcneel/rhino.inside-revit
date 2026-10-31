using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Wall Sweep")]
  public sealed class WallSweep : HostObject, IHostElementAccess
  {
    protected override Type ValueType => typeof(ARDB.WallSweep);
    public new ARDB.WallSweep Value => base.Value as ARDB.WallSweep;

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

    //public override Plane Location
    //{
    //  get
    //  {
    //    if (Curve is Curve curve)
    //    {
    //      var start = curve.PointAtStart;
    //      var end = curve.PointAtEnd;
    //      var axis = end - start;
    //      var origin = start + (axis * 0.5);
    //      var perp = axis.PerpVector();
    //      return new Plane(origin, axis, perp);
    //    }

    //    return base.Location;
    //  }
    //}

    //public override Curve Curve
    //{
    //  get
    //  {
    //    using (var info = Value?.GetWallSweepInfo())
    //    {
    //      var hostIds = Value.GetHostIds();
    //      var runs = new List<Curve>(hostIds.Count);
    //      foreach (var wall in hostIds.Select(x => Wall.FromElementId(Document, x) as Wall))
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
    //            else t = domain1.T1 + info.Distance * Revit.ModelUnits;
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
    //            case ARDB.WallSweepType.Sweep:  direction = +1.0; break;
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
    //            var point = iso.PointAtMid;
    //            surface.ClosestPoint(point, out var u, out var v);
    //            var normal = surface.NormalAt(u, v);
    //            normal.Unitize();
    //            iso.Translate(normal * offset * Revit.ModelUnits);
    //          }
    //          else
    //          {
    //            var point = iso.PointAtMid;
    //            surface.ClosestPoint(point, out var u, out var v);
    //            surface.FrameAt(u, v, out var frame);

    //            //iso = iso.Offset
    //            //(
    //            //  new Plane(frame.Origin, frame.XAxis, frame.ZAxis),
    //            //  offset * Revit.ModelUnits,
    //            //  GeometryTolerance.Model.VertexTolerance,
    //            //  CurveOffsetCornerStyle.None
    //            //)[0];

    //            iso = iso.ToCurve().CreateOffset(offset, UnitXYZ.BasisZ).ToCurve();
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

    #region IHostElementAccess
    public override GraphicalElement HostElement => Value is ARDB.WallSweep wallSweep ?
      wallSweep.GetWallSweepInfo().IsFixed ?
      GetElement<GraphicalElement>(wallSweep.GetHostIds().FirstOrDefault() ?? ElementIdExtension.Invalid) :
      base.HostElement :
      default;
    #endregion
  }
}
