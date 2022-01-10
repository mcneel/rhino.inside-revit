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
    public static explicit operator ARDB.Sketch(Sketch value) => value?.Value;

    public Sketch() : base() { }
    public Sketch(ARDB.Sketch sketchPlane) : base(sketchPlane) { }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is ARDB.HostObject host)
      {
        var sketch = host.GetSketch();
        return sketch is object && SetValue(sketch);
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
      if(Region is object)
        args.Pipeline.DrawBrepShaded(Region, args.Material);
    }
    #endregion

    #region Location
    public override Plane Location => Value?.SketchPlane.GetPlane().ToPlane() ?? base.Location;
    public override Brep Surface => Region;

    bool profileIsValid;
    Curve[] profile;
    public Curve[] Profile
    {
      get
      {
        if (!profileIsValid)
        {
          profile = Value?.Profile.ToArray(GeometryDecoder.ToCurve);
          profileIsValid = true;
        }

        return profile;
      }
    }

    bool regionIsValid;
    Brep region;
    public Brep Region
    {
      get
      {
        if (!regionIsValid && Value is ARDB.Sketch sketch)
        {
          var loops = sketch.Profile.ToCurveMany().Where(x => x.IsClosed).ToArray();
          var plane = sketch.SketchPlane.GetPlane().ToPlane();

          var loopsBox = BoundingBox.Empty;
          foreach (var loop in loops)
            loopsBox.Union(loop.GetBoundingBox(plane, out var _));

          var planeSurface = new PlaneSurface(plane, new Interval(loopsBox.Min.X, loopsBox.Max.X), new Interval(loopsBox.Min.Y, loopsBox.Max.Y));

          if(loops.Length > 0)
            region = CreateTrimmedSurface(planeSurface, loops);

          regionIsValid = true;
        }

        return region;
      }
    }

    struct BrepBoundary
    {
      public BrepLoopType type;
      public List<BrepEdge> edges;
      public PolyCurve trims;
      public List<int> orientation;
    }

    static int AddSurface(Brep brep, Surface surface, Curve[] loops, out List<BrepBoundary>[] shells)
    {
      // Extract base surface
      if (surface is object)
      {
        var tol = GeometryObjectTolerance.Model;
        var trimTolerance = tol.VertexTolerance * 0.1;

        int si = brep.AddSurface(surface);

        if (surface is PlaneSurface)
        {
          var nurbs = surface.ToNurbsSurface();
          nurbs.KnotsU.InsertKnot(surface.Domain(0).Mid);
          nurbs.KnotsV.InsertKnot(surface.Domain(1).Mid);
          surface = nurbs;
        }

        // Classify Loops
        var nesting = new int[loops.Length];
        var edgeLoops = new BrepBoundary[loops.Length];
        {
          var trims = new Curve[loops.Length];

          int index = 0;
          foreach (var loop in loops)
          {
            if (loop is PolyCurve polycurve)
            {
              var trim = new PolyCurve();
              for (int s = 0; s < polycurve.SegmentCount; s++)
              {
                var segment = polycurve.SegmentCurve(s);
                var trimSegment = surface.Pullback(segment, trimTolerance);
                trim.AppendSegment(trimSegment);
              }

              trims[index++] = trim;
            }
            else trims[index++] = surface.Pullback(loop, trimTolerance);
          }

          for (int i = 0; i < edgeLoops.Length; ++i)
          {
            for (int j = i + 1; j < edgeLoops.Length; ++j)
            {
              var containment = Curve.PlanarClosedCurveRelationship(trims[i], trims[j], Plane.WorldXY, tol.VertexTolerance);
              if (containment == RegionContainment.MutualIntersection)
              {
                edgeLoops[i].type = BrepLoopType.Outer;
                edgeLoops[j].type = BrepLoopType.Outer;
              }
              else if (containment == RegionContainment.AInsideB)
              {
                nesting[i]++;
              }
              else if (containment == RegionContainment.BInsideA)
              {
                nesting[j]++;
              }
            }
          }

          // Fix orientation if necessary
          index = 0;
          foreach (var loop in loops)
          {
            // Ignore intersecting loops
            if (edgeLoops[index].type == BrepLoopType.Unknown)
            {
              if (nesting[index] % 2 != 0)
                edgeLoops[index].type = BrepLoopType.Inner;
              else
                edgeLoops[index].type = BrepLoopType.Outer;
            }

            switch (trims[index].ClosedCurveOrientation())
            {
              case CurveOrientation.Undefined:
                break;
              case CurveOrientation.CounterClockwise:
                if (edgeLoops[index].type == BrepLoopType.Inner) loops[index].Reverse(); break;
              case CurveOrientation.Clockwise:
                if (edgeLoops[index].type == BrepLoopType.Outer) loops[index].Reverse(); break;
            }

            ++index;
          }
        }

        // Create Brep Edges and compute trims
        {
          int index = 0;
          foreach (var edgeLoop in loops)
          {
            // Ignore unclasified loops
            if (edgeLoops[index].type == BrepLoopType.Unknown)
              continue;

            var kinks = new List<double>();
            {
              var domain = edgeLoop.Domain;
              var t = domain.Min;
              while (edgeLoop.GetNextDiscontinuity(Continuity.C1_locus_continuous, t, domain.Max, out t))
                kinks.Add(t);
            }

            var edges = kinks.Count > 0 ? edgeLoop.Split(kinks) : new Curve[] { edgeLoop };

            edgeLoops[index].edges = new List<BrepEdge>();
            edgeLoops[index].trims = new PolyCurve();
            edgeLoops[index].orientation = new List<int>();

            foreach (var edge in edges)
            {
              var brepEdge = default(BrepEdge);
              brepEdge = brep.Edges.Add(brep.AddEdgeCurve(edge));

              edgeLoops[index].edges.Add(brepEdge);
              var segment = edge;

              edgeLoops[index].orientation.Add(segment.TangentAt(segment.Domain.Mid).IsParallelTo(brepEdge.TangentAt(brepEdge.Domain.Mid)));

              var trim = surface.Pullback(segment, trimTolerance);
              edgeLoops[index].trims.Append(trim);
            }

            edgeLoops[index].trims.MakeClosed(tol.VertexTolerance);

            ++index;
          }
        }

        // Sort edgeLoops in nesting orther, shallow loops first
        Array.Sort(nesting, edgeLoops);

        var outerLoops = edgeLoops.Where(x => x.type == BrepLoopType.Outer);
        var innerLoops = edgeLoops.Where(x => x.type == BrepLoopType.Inner);

        // Group Edge loops in shells with the outer loop as the first one
        shells = outerLoops.
                 Select(x => new List<BrepBoundary>() { x }).
                 ToArray();

        if (shells.Length == 1)
        {
          shells[0].AddRange(innerLoops);
        }
        else
        {
          // Iterate in reverse order to found deeper loops before others
          foreach (var innerLoop in innerLoops.Reverse())
          {
            foreach (var shell in shells.Reverse())
            {
              var containment = Curve.PlanarClosedCurveRelationship(innerLoop.trims, shell[0].trims, Plane.WorldXY, tol.VertexTolerance);
              if (containment == RegionContainment.AInsideB)
              {
                shell.Add(innerLoop);
                break;
              }
            }
          }
        }

        return si;
      }

      shells = default;
      return -1;
    }

    static void TrimSurface(Brep brep, int surface, bool orientationIsReversed, IEnumerable<IEnumerable<BrepBoundary>> shells)
    {
      foreach (var shell in shells)
      {
        var brepFace = brep.Faces.Add(surface);
        brepFace.OrientationIsReversed = orientationIsReversed;

        foreach (var loop in shell)
        {
          var brepLoop = brep.Loops.Add(loop.type, brepFace);

          var edgeCount = loop.edges.Count;
          for (int e = 0; e < edgeCount; ++e)
          {
            var brepEdge = loop.edges[e];

            int orientation = loop.orientation[e];
            if (orientation == 0)
              continue;

            if (loop.trims.SegmentCurve(e) is Curve trim)
            {
              var ti = brep.AddTrimCurve(trim);
              brep.Trims.Add(brepEdge, orientation < 0, brepLoop, ti);
            }
          }

          brep.Trims.MatchEnds(brepLoop);
        }
      }
    }

    static Brep CreateTrimmedSurface(Surface surface, Curve[] loops)
    {
      var brep = new Brep();

      // Set surface
      var si = AddSurface(brep, surface, loops, out var shells);
      if (si < 0)
        return null;

      // Set edges & trims
      TrimSurface(brep, si, false, shells);

      // Set vertices
      brep.SetVertices();

      // Set flags
      brep.SetTolerancesBoxesAndFlags
      (
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true
      );

      return brep;
    }
    #endregion
  }
}
