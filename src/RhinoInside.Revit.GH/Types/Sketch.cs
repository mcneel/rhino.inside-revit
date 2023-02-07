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

  interface ISketchAccess
  {
    Sketch Sketch { get; }
  }

  [Kernel.Attributes.Name("Sketch")]
  public class Sketch : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Sketch);
    public new ARDB.Sketch Value => base.Value as ARDB.Sketch;

    public Sketch() : base() { }
    public Sketch(ARDB.Sketch sketch) : base(sketch) { }

    public override bool CastFrom(object source)
    {
      if (source is Element element)
      {
        if (element.Value?.GetSketch() is ARDB.Sketch sketch) return SetValue(sketch);
        else
        {
          ResetValue();
          return true;
        }
      }

      return base.CastFrom(source);
    }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      foreach (var loop in Profiles)
        args.Pipeline.DrawCurve(loop, args.Color, args.Thickness);
    }

    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (TrimmedSurface is object)
        args.Pipeline.DrawBrepShaded(TrimmedSurface, args.Material);
    }
    #endregion

    #region GraphicalElement
    protected override void SubInvalidateGraphics()
    {
      _Profiles = default;
      _TrimmedSurface = default;

      base.SubInvalidateGraphics();
    }

    public override Plane Location => Value?.SketchPlane.GetPlane().ToPlane() ?? NaN.Plane;

    public Plane ProfilesPlane
    {
      get
      {
        if (Value is ARDB.Sketch sketch)
        {
          var plane = sketch.SketchPlane.GetPlane().ToPlane();

          var bbox = BoundingBox.Empty;
          foreach (var profile in Profiles)
            bbox.Union(profile.GetBoundingBox(plane));

          plane.Origin = plane.PointAt(bbox.Center.X, bbox.Center.Y);
          return plane;
        }

        return NaN.Plane;
      }
    }

    (bool HasValue, Curve[] Value) _Profiles;
    public Curve[] Profiles
    {
      get
      {
        if (!_Profiles.HasValue && Value is ARDB.Sketch sketch)
        {
          try
          {
            _Profiles.Value = sketch.Profile.Cast<ARDB.CurveArray>().SelectMany(GeometryDecoder.ToCurves).ToArray();
            var plane = sketch.SketchPlane.GetPlane().ToPlane();

            foreach (var profile in _Profiles.Value)
            {
              if (!profile.IsClosed) continue;
              if (profile.ClosedCurveOrientation(plane) == CurveOrientation.Clockwise)
                profile.Reverse();
            }
          }
          catch { }

          _Profiles.HasValue = true;
        }

        return _Profiles.Value;
      }
    }

    (bool HasValue, Brep Value) _TrimmedSurface;
    public override Brep TrimmedSurface
    {
      get
      {
        if (!_TrimmedSurface.HasValue && Value is ARDB.Sketch sketch)
        {
          var loops = sketch.Profile.ToCurveMany().Where(x => x.IsClosed).ToArray();
          var plane = sketch.SketchPlane.GetPlane().ToPlane();

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

            _TrimmedSurface.Value = planeSurface.CreateTrimmedSurface(loops, GeometryTolerance.Model.VertexTolerance);
          }

          _TrimmedSurface.HasValue = true;
        }

        return _TrimmedSurface.Value;
      }
    }
    #endregion

    #region Owner
    public Element Owner =>
      Value is ARDB.Sketch sketch ? Element.FromElement(sketch.GetOwner()) : default;
    #endregion

    #region SketchPlane
    public SketchPlane SketchPlane =>
      Value is ARDB.Sketch sketch ? SketchPlane.FromElement(sketch.SketchPlane) as SketchPlane : default;
    #endregion

    #region Slope Arrow
    public CurveElement SlopeArrow
    {
      get
      {
        if (Value is ARDB.Sketch sketch)
        {
          var slopeArrow = sketch.GetDependentElements(ERDB.CompoundElementFilter.ElementClassFilter(typeof(ARDB.CurveElement))).
            Select(Document.GetElement).FirstOrDefault(x => x.get_Parameter(ARDB.BuiltInParameter.SPECIFY_SLOPE_OR_OFFSET) is object);

          return CurveElement.FromElement(slopeArrow) as CurveElement;
        }

        return default;
      }
    }
    #endregion

    internal static bool SetProfile(ARDB.Sketch sketch, IList<Curve> boundaries, Vector3d normal)
    {
      var tol = GeometryTolerance.Model;
      var sketchPlane = sketch.SketchPlane.GetPlane();
      var plane = sketchPlane.ToPlane();
      if (normal.IsParallelTo(plane.Normal, tol.AngleTolerance) == 0)
        return false;

      var profiles = sketch.Profile.ToArray(GeometryDecoder.ToPolyCurve);
      if (profiles.Length != boundaries.Count)
        return false;

      bool constraintsRemoved = false;
      bool splitClosed = true;

      switch (sketch.GetOwner())
      {
        // FilledRegion needs the constraints to be edited from the Revi UI latter!!
        case ARDB.FilledRegion _:     constraintsRemoved = true;  break;
        case ARDB.FootPrintRoof _:    splitClosed = false;        break;
#if REVIT_2022
        case ARDB.CeilingAndFloor _:  splitClosed = false;        break;
#endif
      }

      void RemoveConstraints()
      {
        if (constraintsRemoved) return;
        var constraintsIds = sketch.GetDependentElements
        (
          ERDB.CompoundElementFilter.Intersect
          (
          new ARDB.ElementClassFilter(typeof(ARDB.Dimension)),
          new ARDB.ElementCategoryFilter(ARDB.BuiltInCategory.OST_WeakDims)
          )
        );

        sketch.Document.Delete(constraintsIds);
        constraintsRemoved = true;
      }

      var loops = sketch.GetProfileCurveElements();
      var pi = 0;
      foreach (var boundary in boundaries)
      {
        var profile = Curve.ProjectToPlane(boundary, plane);

        if
        (
          !Curve.GetDistancesBetweenCurves(profiles[pi], profile, tol.VertexTolerance, out var max, out var _, out var _, out var _, out var _, out var _) ||
          max > tol.VertexTolerance
        )
        {
          // Remove all constraints to move curves freely.
          RemoveConstraints();

          var segments = profile.TryGetPolyCurve(out var polyCurve, tol.AngleTolerance) ?
            polyCurve.DuplicateSegments() :
            splitClosed ?
            profile.Split(profile.Domain.Mid) :
            new Curve[] { profile };

          if (pi < loops.Count)
          {
            var loop = loops[pi];
            if (segments.Length != loop.Count)
              return false;

            var index = 0;
            foreach (var edge in loop)
            {
              var segment = profile.IsClosed ?
                segments[(++index) % segments.Length ] :
                segments[index++];

              var curve = default(ARDB.Curve);
              if (edge.GeometryCurve is ARDB.HermiteSpline)
                curve = segment.ToHermiteSpline();
              else
                curve = segment.ToCurve();

              if (!edge.GeometryCurve.IsSameKindAs(curve))
                return false;

              if (!edge.GeometryCurve.AlmostEquals(curve, GeometryTolerance.Internal.VertexTolerance))
                edge.SetGeometryCurve(curve, overrideJoins: true);
            }
          }
        }

        pi++;
      }

      return true;
    }
  }
}
