using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Sketch")]
  public class Sketch : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Sketch);
    public new ARDB.Sketch Value => base.Value as ARDB.Sketch;

    public Sketch() : base() { }
    public Sketch(ARDB.Sketch sketch) : base(sketch) { }

    public override bool CastFrom(object source)
    {
      if (source is Element element && element.Value?.GetSketch() is ARDB.Sketch sketch)
        return SetValue(sketch);

      return base.CastFrom(source);
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      foreach(var loop in Profiles)
        args.Pipeline.DrawCurve(loop, args.Color, args.Thickness);
    }

    public override void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if(TrimmedSurface is object)
        args.Pipeline.DrawBrepShaded(TrimmedSurface, args.Material);
    }
    #endregion

    #region Location
    protected override void SubInvalidateGraphics()
    {
      profiles = default;
      trimmedSurface = default;

      base.SubInvalidateGraphics();
    }

    public override Plane Location =>
      Value?.SketchPlane.GetPlane().ToPlane() ?? NaN.Plane;

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

    (bool HasValue, Curve[] Value) profiles;
    public Curve[] Profiles
    {
      get
      {
        if (!profiles.HasValue && Value is ARDB.Sketch sketch)
        {
          try
          {
            profiles.Value = sketch.Profile.Cast<ARDB.CurveArray>().SelectMany(GeometryDecoder.ToCurves).ToArray();
            var plane = sketch.SketchPlane.GetPlane().ToPlane();

            foreach (var profile in profiles.Value)
            {
              if (!profile.IsClosed) continue;
              if (profile.ClosedCurveOrientation(plane) == CurveOrientation.Clockwise)
                profile.Reverse();
            }
          }
          catch { }

          profiles.HasValue = true;
        }

        return profiles.Value;
      }
    }

    (bool HasValue, Brep Value) trimmedSurface;
    public override Brep TrimmedSurface
    {
      get
      {
        if (!trimmedSurface.HasValue && Value is ARDB.Sketch sketch)
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

            trimmedSurface.Value = planeSurface.CreateTrimmedSurface(loops, GeometryObjectTolerance.Model.VertexTolerance);
          }

          trimmedSurface.HasValue = true;
        }

        return trimmedSurface.Value;
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
  }
}
