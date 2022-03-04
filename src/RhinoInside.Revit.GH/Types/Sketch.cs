using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

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
      if (source is ISketchAccess access)
      {
        var sketch = access.Sketch;
        return sketch is object && SetValue(sketch.Value);
      }

      return base.CastFrom(source);
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (!location.IsValid)
        return;

      GH_Plane.DrawPlane(args.Pipeline, location, Grasshopper.CentralSettings.PreviewPlaneRadius, 4, args.Color, System.Drawing.Color.DarkRed, System.Drawing.Color.DarkGreen);

      foreach(var loop in Profile)
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
      profile = default;
      region = default;

      base.SubInvalidateGraphics();
    }

    public override Plane Location => Value?.SketchPlane.GetPlane().ToPlane() ?? base.Location;

    (bool HasValue, Curve[] Value) profile;
    public Curve[] Profile
    {
      get
      {
        if (!profile.HasValue && Value is ARDB.Sketch sketch)
        {
          profile.Value = sketch.Profile.Cast<ARDB.CurveArray>().SelectMany(GeometryDecoder.ToCurves).ToArray();
          profile.HasValue = true;
        }

        return profile.Value;
      }
    }

    (bool HasValue, Brep Value) region;
    public override Brep TrimmedSurface
    {
      get
      {
        if (!region.HasValue && Value is ARDB.Sketch sketch)
        {
          var loops = sketch.Profile.ToCurveMany().Where(x => x.IsClosed).ToArray();
          var plane = sketch.SketchPlane.GetPlane().ToPlane();

          if (loops.Length > 0)
          {
            var loopsBox = BoundingBox.Empty;
            foreach (var loop in loops)
              loopsBox.Union(loop.GetBoundingBox(plane, out var _));

            var planeSurface = new PlaneSurface
            (
              plane,
              new Interval(loopsBox.Min.X, loopsBox.Max.X),
              new Interval(loopsBox.Min.Y, loopsBox.Max.Y)
            );

            region.Value = planeSurface.CreateTrimmedSurface(loops, GeometryObjectTolerance.Model.VertexTolerance);
          }

          region.HasValue = true;
        }

        return region.Value;
      }
    }
    #endregion

    #region Owner
    public Element Owner =>
      Value is ARDB.Sketch sketch ? Element.FromElement(sketch.GetOwner<ARDB.Element>()) : default;
    #endregion

    #region SketchPlane
    public SketchPlane SketchPlane =>
      Value is ARDB.Sketch sketch ? SketchPlane.FromElement(sketch.SketchPlane) as SketchPlane : default;
    #endregion
  }
}
