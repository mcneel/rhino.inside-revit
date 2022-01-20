using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using External.DB;
  using External.DB.Extensions;

  /// <summary>
  /// Converts a "complex" <see cref="Brep"/> to be transfered to a <see cref="ARDB.Solid"/>.
  /// </summary>
  static class BrepEncoder
  {
    #region Tolerances
    static double JoinTolerance => Math.Sqrt(Math.Pow(GeometryObjectTolerance.Internal.VertexTolerance, 2.0) * 2.0);
    static double EdgeTolerance => JoinTolerance * 0.5;
    #endregion

    #region Encode
    internal static Brep ToRawBrep(/*const*/ Brep brep, double scaleFactor)
    {
      brep = brep.DuplicateShallow() as Brep;
      return EncodeRaw(ref brep, scaleFactor) ? brep : default;
    }

    internal static bool EncodeRaw(ref Brep brep, double scaleFactor)
    {
      if (scaleFactor != 1.0 && !brep.Scale(scaleFactor))
        return default;

      var tol = GeometryObjectTolerance.Internal;
      var bbox = brep.GetBoundingBox(false);
      if (!bbox.IsValid || bbox.Diagonal.Length < tol.ShortCurveTolerance)
        return default;

      // Split and Shrink faces
      {
        brep.Faces.SplitKinkyFaces(tol.AngleTolerance, true);
        brep.Faces.SplitClosedFaces(0);
        brep.Faces.ShrinkFaces();
      }

      var options = AuditBrep(brep);

      return RebuildBrep(ref brep, options);
    }

    [Flags]
    enum BrepIssues
    {
      Nothing                     = 0,
      OutOfToleranceEdges         = 1,
      OutOfToleranceSurfaceKnots  = 2,
    }

    static BrepIssues AuditBrep(Brep brep)
    {
      var options = default(BrepIssues);
      var tol = GeometryObjectTolerance.Internal;

      // Edges
      {
        foreach (var edge in brep.Edges)
        {
          if (edge.Tolerance > tol.VertexTolerance)
          {
            options |= BrepIssues.OutOfToleranceEdges;
            GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry contains out of tolerance edges, it will be rebuilt.", edge);
          }
        }
      }

      // Faces
      {
        foreach (var face in brep.Faces)
        {
          var deltaU = KnotListEncoder.MinDelta(face.GetSpanVector(0));
          if (deltaU < 1e-5)
          {
            options |= BrepIssues.OutOfToleranceSurfaceKnots;
            break;
          }

          var deltaV = KnotListEncoder.MinDelta(face.GetSpanVector(1));
          if (deltaV < 1e-5)
          {
            options |= BrepIssues.OutOfToleranceSurfaceKnots;
            break;
          }
        }
      }

      return options;
    }

    static bool RebuildBrep(ref Brep brep, BrepIssues options)
    {
      if(options != BrepIssues.Nothing)
      {
        var tol = GeometryObjectTolerance.Internal;
        var edgesToUnjoin = brep.Edges.Select(x => x.EdgeIndex);
        var shells = brep.UnjoinEdges(edgesToUnjoin);
        if (shells.Length == 0)
          shells = new Brep[] { brep };

        var kinkyEdges = 0;
        var microEdges = 0;
        var mergedEdges = 0;

        foreach (var shell in shells)
        {
          // Edges
          {
            var edges = shell.Edges;

            int edgeCount = edges.Count;
            for (int ei = 0; ei < edgeCount; ++ei)
              edges.SplitKinkyEdge(ei, tol.AngleTolerance);

            kinkyEdges += edges.Count - edgeCount;
            microEdges += edges.RemoveNakedMicroEdges(tol.VertexTolerance, cleanUp: true);
            mergedEdges += edges.MergeAllEdges(tol.AngleTolerance) - edgeCount;
          }

          // Faces
          {
            foreach (var face in shell.Faces)
            {
              if(options.HasFlag(BrepIssues.OutOfToleranceSurfaceKnots))
              {
                face.GetSurfaceSize(out var width, out var height);

                face.SetDomain(0, new Interval(0.0, width));
                var deltaU = KnotListEncoder.MinDelta(face.GetSpanVector(0));
                if (deltaU < 1e-6)
                  face.SetDomain(0, new Interval(0.0, width * (1e-6 / deltaU)));

                face.SetDomain(1, new Interval(0.0, height));
                var deltaV = KnotListEncoder.MinDelta(face.GetSpanVector(1));
                if (deltaV < 1e-6)
                  face.SetDomain(1, new Interval(0.0, height * (1e-6 / deltaV)));
              }

              face.RebuildEdges(1e-6, false, true);
            }
          }

          // Flags
          shell.SetTrimIsoFlags();
        }

        if(kinkyEdges > 0)
          GeometryEncoder.Context.Peek.RuntimeMessage(10, $"{kinkyEdges} kinky-edges splitted", default);

#if DEBUG
        if (microEdges > 0)
          GeometryEncoder.Context.Peek.RuntimeMessage(255, $"DEBUG - {microEdges} Micro-edges removed", default);

        if (mergedEdges > 0)
          GeometryEncoder.Context.Peek.RuntimeMessage(255, $"DEBUG - {mergedEdges} Edges merged", default);
#endif

        //var join = shells;
        var join = Brep.JoinBreps(shells, tol.VertexTolerance);
        if (join.Length == 1) brep = join[0];
        else
        {
          var merge = new Brep();
          foreach (var shell in join)
            merge.Append(shell);

          //var joined = merge.JoinNakedEdges(tol.VertexTolerance);
          brep = merge;
        }

#if DEBUG
        foreach (var edge in brep.Edges)
        {
          if (edge.Tolerance > tol.VertexTolerance)
            GeometryEncoder.Context.Peek.RuntimeMessage(255, $"DEBUG - Geometry contains out of tolerance edges", edge);
        }
#endif
      }

      return brep.IsValid;
    }
    #endregion

    #region Transfer
    internal static ARDB.Mesh ToMesh(/*const*/ Brep brep, double factor)
    {
      using (var mp = MeshingParameters.Default)
      {
        mp.Tolerance = 0.0;// GeometryObjectTolerance.Internal.VertexTolerance / factor;
        mp.MinimumTolerance = 0.0;
        mp.RelativeTolerance = 0.0;

        mp.RefineGrid = false;
        mp.GridAspectRatio = 0.0;
        mp.GridAngle = 0.0;
        mp.GridMaxCount = 0;
        mp.GridMinCount = 0;
        mp.MinimumEdgeLength = MeshEncoder.ShortEdgeTolerance / factor;
        mp.MaximumEdgeLength = 0.0;

        mp.ClosedObjectPostProcess = brep.IsSolid;
        mp.JaggedSeams = brep.IsManifold;
        mp.SimplePlanes = true;

        if (Mesh.CreateFromBrep(brep, mp) is Mesh[] shells)
          return MeshEncoder.ToMesh(shells, factor);

        return default;
      }
    }

    internal static ARDB.Solid ToSolid(/*const*/Brep brep, double factor)
    {
      // Try on existing solids already in memory.
      if (GeometryCache.TryGetExistingGeometry(brep, factor, out ARDB.Solid existing, out var signature))
      {
#if DEBUG
        GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Using cached value {signature}…", default);
#endif
        return AuditSolid(brep, existing);
      }

      // Try to convert...
      if (TryGetSolid(brep, factor, out var solid))
      {
        GeometryCache.AddExistingGeometry(signature, solid);

        return AuditSolid(brep, solid);
      }

      return default;
    }

    static ARDB.Solid AuditSolid(Brep brep, ARDB.Solid solid)
    {
      if (brep.IsSolid)
      {
//#if DEBUG
//        if (solid.TryGetNakedEdges(out var nakedEdges))
//        {
//          GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Output geometry has {nakedEdges.Count} naked edges.", default);
//          foreach (var edge in nakedEdges)
//            GeometryEncoder.Context.Peek.RuntimeMessage(20, default, edge.AsCurve().ToCurve().InHostUnits());
//        }
//#else
        if (!solid.IsWatertight())
          GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Output geometry has naked edges.", default);
//#endif
      }

      // DirectShape geometry has aditional validation
      switch(GeometryEncoder.Context.Peek.Element)
      {
        case ARDB.DirectShape ds:
          if (!ds.IsValidGeometry(solid))
          {
            GeometryEncoder.Context.Peek.RuntimeMessage(20, "Geometry does not satisfy DirectShape validation criteria", brep.InHostUnits());
            return default;
          }
          break;

        case ARDB.DirectShapeType dst:
          if (!dst.IsValidShape(new ARDB.Solid[] { solid }))
          {
            GeometryEncoder.Context.Peek.RuntimeMessage(20, "Geometry does not satisfy DirectShapeType validation criteria", brep.InHostUnits());
            return default;
          }
          break;
      }

      return solid;
    }

    internal static bool TryGetSolid(/*const*/Brep brep, double factor, out ARDB.Solid solid)
    {
      solid = default;

      // Try convert flat extrusions under tolerance as surfaces
      if (brep.TryGetExtrusion(out var extrusion))
      {
        var tol = GeometryObjectTolerance.Internal;
        var height = extrusion.PathStart.DistanceTo(extrusion.PathEnd);
        if (height < tol.VertexTolerance / factor)
        {
          var curves = new List<Curve>(extrusion.ProfileCount);
          for (int p = 0; p < extrusion.ProfileCount; ++p)
            curves.Add(extrusion.Profile3d(p, 0.5));

          var regions = Brep.CreatePlanarBreps(curves, tol.VertexTolerance / factor);
          if (regions.Length != 1)
            return false;

          brep = regions[0];
        }
      }

      // Try using ARDB.BRepBuilder
      {
        var raw = ToRawBrep(brep, factor);

        if (ToSolid(raw) is ARDB.Solid converted)
        {
          solid = converted;
          return true;
        }
      }

      // Try using ARDB.ShapeImporter | ARDB.Document.Import
      {
        GeometryEncoder.Context.Peek.RuntimeMessage(255, "Using SAT…", default);

        if (ToSAT(brep, factor) is ARDB.Solid imported)
        {
          solid = imported;
          return true;
        }
      }

      GeometryEncoder.Context.Peek.RuntimeMessage(20, "Failed to convert geometry.", brep.InHostUnits());
      return false;
    }

    /// <summary>
    /// Replaces <see cref="Raw.RawEncoder.ToHost(Brep)"/> to catch Revit Exceptions
    /// </summary>
    /// <param name="brep"></param>
    /// <returns></returns>
    internal static ARDB.Solid ToSolid(/*const*/ Brep brep)
    {
      if (brep is null)
        return null;

      try
      {
        var tol = GeometryObjectTolerance.Internal;
        var brepType = ARDB.BRepType.OpenShell;
        switch (brep.SolidOrientation)
        {
          case BrepSolidOrientation.Inward: brepType = ARDB.BRepType.Void; break;
          case BrepSolidOrientation.Outward: brepType = ARDB.BRepType.Solid; break;
        }

        using (var builder = new ARDB.BRepBuilder(brepType))
        {
#if REVIT_2018
          builder.SetAllowShortEdges();
          builder.AllowRemovalOfProblematicFaces();
#endif

          var brepEdges = new List<ARDB.BRepBuilderGeometryId>[brep.Edges.Count];
          foreach (var face in brep.Faces)
          {
            var surfaceGeom = default(ARDB.BRepBuilderSurfaceGeometry);
            try { surfaceGeom = Raw.RawEncoder.ToHost(face); }
            catch (Autodesk.Revit.Exceptions.ArgumentException e)
            {
              var message = e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0];
              GeometryEncoder.Context.Peek.RuntimeMessage(20, $"{message}{Environment.NewLine}Face will be removed from the output.", face);
              continue;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException e)
            {
              var message = e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0];
              GeometryEncoder.Context.Peek.RuntimeMessage(20, $"{message}{Environment.NewLine}Face will be removed from the output.", face);
              continue;
            }

            bool error = false;
            var faceId = builder.AddFace(surfaceGeom, face.OrientationIsReversed);
            builder.SetFaceMaterialId(faceId, GeometryEncoder.Context.Peek.MaterialId);

            foreach (var loop in face.Loops)
            {
              switch (loop.LoopType)
              {
                case BrepLoopType.Outer: break;
                case BrepLoopType.Inner: break;
                default: GeometryEncoder.Context.Peek.RuntimeMessage(10, $"{loop.LoopType} loop skipped.", loop); continue;
              }

              var loopId = builder.AddLoop(faceId);

              IEnumerable<BrepTrim> trims = loop.Trims;
              if (face.OrientationIsReversed)
                trims = trims.Reverse();

              foreach (var trim in trims)
              {
                if (trim.TrimType != BrepTrimType.Boundary && trim.TrimType != BrepTrimType.Mated)
                  continue;

                var edge = trim.Edge;
                if (edge is null)
                  continue;

                try
                {
                  var edgeIds = brepEdges[edge.EdgeIndex];
                  if (edgeIds is null)
                  {
                    edgeIds = brepEdges[edge.EdgeIndex] = new List<ARDB.BRepBuilderGeometryId>();
                    edgeIds.AddRange(ToBRepBuilderEdgeGeometry(edge).Select(e => builder.AddEdge(e)));
                  }

                  bool trimReversed = face.OrientationIsReversed ?
                                      !trim.IsReversed() :
                                       trim.IsReversed();

                  if (trimReversed)
                  {
                    for (int e = edgeIds.Count - 1; e >= 0; --e)
                      builder.AddCoEdge(loopId, edgeIds[e], true);
                  }
                  else
                  {
                    for (int e = 0; e < edgeIds.Count; ++e)
                      builder.AddCoEdge(loopId, edgeIds[e], false);
                  }
                }
                catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException e)
                {
                  error = true;
                  var message = e.Message.Replace("(as identified by Application.ShortCurveTolerance)", $"({GeometryObjectTolerance.Model.ShortCurveTolerance})");
                  message = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0];
                  GeometryEncoder.Context.Peek.RuntimeMessage(20, message, edge);
                  break;
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException e)
                {
                  error = true;
                  var message = e.Message.Replace("BRepBuilder::addCoEdgeInternal_()", "BRepBuilder");
                  message = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0];
                  GeometryEncoder.Context.Peek.RuntimeMessage(20, message, edge);
                  return default;
                }
              }

              try { builder.FinishLoop(loopId); }
              catch (Autodesk.Revit.Exceptions.ArgumentException e)
              {
                if (!error)
                {
                  var message = e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0];
                  GeometryEncoder.Context.Peek.RuntimeMessage(20, message, loop);
                }
              }
            }

            try { builder.FinishFace(faceId); }
            catch (Autodesk.Revit.Exceptions.ArgumentException e)
            {
              if (!error)
              {
                error = true;
                var message = e.Message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0];
                GeometryEncoder.Context.Peek.RuntimeMessage(20, message, face);
              }
            }
          }

          try
          {
            var brepBuilderOutcome = builder.Finish();
            if (builder.IsResultAvailable())
            {
#if REVIT_2018
              if (builder.RemovedSomeFaces())
                GeometryEncoder.Context.Peek.RuntimeMessage(20, "Some problematic faces were removed from the output", default);
#endif
              return builder.GetResult();
            }
          }
          catch (Autodesk.Revit.Exceptions.InvalidOperationException) { /* BRepBuilder contains an incomplete Brep definiton */ }
        }
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException e)
      {
        // Any unexpected Revit Exception will be catched here.
        GeometryEncoder.Context.Peek.RuntimeMessage(20, e.Message, default);
      }

      return null;
    }

    static ARDB.Line ToEdgeCurve(Line line)
    {
      var length = line.Length;
      var isShort = length < GeometryObjectTolerance.Internal.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;

      var curve = ARDB.Line.CreateBound
      (
        line.From.ToXYZ(factor),
        line.To.ToXYZ(factor)
      );

      return isShort ? (ARDB.Line) curve.CreateTransformed(ARDB.Transform.Identity.ScaleBasis(length)) : curve;
    }

    static ARDB.Arc ToEdgeCurve(Arc arc)
    {
      var length = arc.Length;
      var isShort = length < GeometryObjectTolerance.Internal.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;

      var curve = ARDB.Arc.Create
      (
        arc.StartPoint.ToXYZ(factor),
        arc.EndPoint.ToXYZ(factor),
        arc.MidPoint.ToXYZ(factor)
      );

      return isShort ? (ARDB.Arc) curve.CreateTransformed(ARDB.Transform.Identity.ScaleBasis(length)) : curve;
    }

    static ARDB.Curve ToEdgeCurve(NurbsCurve nurbs)
    {
      var length = nurbs.GetLength();
      var isShort = length < GeometryObjectTolerance.Internal.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;

      var degree = nurbs.Degree;
      var knots = Raw.RawEncoder.ToHost(nurbs.Knots);

      var rational = nurbs.IsRational;
      var points = nurbs.Points;
      var count = points.Count;
      var controlPoints = new ARDB.XYZ[count];
      var weights = rational ? new double[count] : default;

      for (int p = 0; p < count; ++p)
      {
        var location = points[p].Location;
        controlPoints[p] = new ARDB.XYZ(location.X * factor, location.Y * factor, location.Z * factor);
        if (rational) weights[p] = points[p].Weight;
      }

      var curve = rational ?
        ARDB.NurbSpline.CreateCurve(degree, knots, controlPoints, weights) :
        ARDB.NurbSpline.CreateCurve(degree, knots, controlPoints);

      curve = isShort ? curve.CreateTransformed(ARDB.Transform.Identity.ScaleBasis(1.0 / factor)) : curve;
      return curve;
    }

    static IEnumerable<ARDB.Curve> ToEdgeCurve(PolyCurve curve)
    {
      var tol = GeometryObjectTolerance.Internal;
      if (curve.RemoveShortSegments(tol.VertexTolerance))
      {
#if DEBUG
        GeometryEncoder.Context.Peek.RuntimeMessage(10, "Edge micro-segment removed.", curve);
#endif
      }

      int segmentCount = curve.SegmentCount;
      for (int s = 0; s < segmentCount; ++s)
      {
        var segment = curve.SegmentCurve(s);
        switch (segment)
        {
          case LineCurve line:   yield return ToEdgeCurve(line.Line); break;
          case ArcCurve arc:     yield return ToEdgeCurve(arc.Arc); break;
          case NurbsCurve nurbs: yield return ToEdgeCurve(nurbs); break;
          default: throw new NotImplementedException();
        }
      }
    }

    static IEnumerable<ARDB.Line> ToEdgeCurveMany(PolylineCurve curve)
    {
      var tol = GeometryObjectTolerance.Internal;
      if (curve.RemoveShortSegments(tol.VertexTolerance))
      {
#if DEBUG
        GeometryEncoder.Context.Peek.RuntimeMessage(10, "Edge micro-segment removed.", curve);
#endif
      }

      int pointCount = curve.PointCount;
      if (pointCount > 1)
      {
        var point = curve.Point(0);
        var segment = new Line { From = point };
        for (int p = 1; p < pointCount; segment.From = segment.To, ++p)
        {
          point = curve.Point(p);
          segment.To = point;
          yield return ToEdgeCurve(segment);
        }
      }
    }

    static IEnumerable<ARDB.Arc> ToEdgeCurveMany(ArcCurve curve)
    {
      var tol = GeometryObjectTolerance.Internal;
      if (curve.IsClosed(tol.ShortCurveTolerance * 1.01))
      {
        var interval = curve.Domain;
        double min = interval.Min, mid = interval.Mid, max = interval.Max;
        var points = new Point3d[]
        {
          curve.PointAt(min),
          curve.PointAt(min + (mid - min) * 0.5),
          curve.PointAt(mid),
          curve.PointAt(mid + (max - mid) * 0.5),
          curve.PointAt(max),
        };

        yield return ToEdgeCurve(new Arc(points[0], points[1], points[2]));
        yield return ToEdgeCurve(new Arc(points[2], points[3], points[4]));
      }
      else yield return ToEdgeCurve(curve.Arc);
    }

    static IEnumerable<ARDB.Curve> ToEdgeCurveMany(PolyCurve curve)
    {
      var tol = GeometryObjectTolerance.Internal;
      if (curve.RemoveShortSegments(tol.VertexTolerance))
      {
#if DEBUG
        GeometryEncoder.Context.Peek.RuntimeMessage(10, "Edge micro-segment removed.", curve);
#endif
      }

      int segmentCount = curve.SegmentCount;
      for (int s = 0; s < segmentCount; ++s)
      {
        foreach (var segment in ToEdgeCurveMany(curve.SegmentCurve(s)))
          yield return segment;
      }
    }

    static IEnumerable<ARDB.Curve> ToEdgeCurveMany(NurbsCurve curve)
    {
      // Reparametrize edgeCurve here to avoid two knots overlap due tolerance.
      // In case overlap happens curve will be splitted in more segments.
      if (curve.Degree > 2)
      {
        var length = curve.GetLength();
        curve.Domain = new Interval(0.0, length);

        var delta = KnotListEncoder.MinDelta(curve.GetSpanVector());
        if (delta < 1e-6)
          curve.Domain = new Interval(0.0, length * (1e-6 / delta));
      }

      if (curve.Degree == 1)
      {
        var curvePoints = curve.Points;
        int pointCount = curvePoints.Count;
        if (pointCount > 1)
        {
          var segment = new Line { From = curvePoints[0].Location };
          for (int p = 1; p < pointCount; ++p)
          {
            segment.To = curvePoints[p].Location;
            yield return ToEdgeCurve(segment);
            segment.From = segment.To;
          }
        }
        yield break;
      }
      else if (curve.Degree == 2)
      {
        for (int s = 0; s < curve.SpanCount; ++s)
        {
          var segment = curve.Trim(curve.SpanDomain(s)) as NurbsCurve;
          yield return ToEdgeCurve(segment);
        }
      }
      else if (curve.TryGetPolyCurveC2(out var polyCurve))
      {
        foreach (var segment in ToEdgeCurve(polyCurve))
          yield return segment;

        yield break;
      }
      else if (curve.IsClosed(GeometryObjectTolerance.Internal.VertexTolerance))
      {
        var segments = curve.DuplicateSegments();
        if (segments.Length == 1)
        {
          if
          (
            curve.NormalizedLengthParameter(0.5, out var mid) &&
            curve.Split(mid) is Curve[] half
          )
          {
            yield return ToEdgeCurve(half[0] as NurbsCurve);
            yield return ToEdgeCurve(half[1] as NurbsCurve);
          }
          else throw new ConversionException("Failed to Split closed Edge");
        }
        else
        {
          foreach (var segment in segments)
            yield return ToEdgeCurve(segment as NurbsCurve);
        }
      }
      else
      {
        yield return ToEdgeCurve(curve);
      }
    }

    static IEnumerable<ARDB.Curve> ToEdgeCurveMany(Curve curve)
    {
      switch (curve)
      {
        case LineCurve lineCurve:

          yield return ToEdgeCurve(lineCurve.Line);
          yield break;

        case PolylineCurve polylineCurve:

          foreach (var line in ToEdgeCurveMany(polylineCurve))
            yield return line;
          yield break;

        case ArcCurve arcCurve:

          foreach (var arc in ToEdgeCurveMany(arcCurve))
            yield return arc;
          yield break;

        case PolyCurve polyCurve:

          foreach (var segment in ToEdgeCurveMany(polyCurve))
            yield return segment;
          yield break;

        case NurbsCurve nurbsCurve:

          foreach (var nurbs in ToEdgeCurveMany(nurbsCurve))
            yield return nurbs;
          yield break;

        default:
          if (curve.HasNurbsForm() != 0)
          {
            var nurbsForm = curve.ToNurbsCurve();
            foreach (var c in ToEdgeCurveMany(nurbsForm))
              yield return c;
          }
          else throw new ConversionException($"Unable to convert {curve} to {typeof(ARDB.Curve)}");
          yield break;
      }
    }

    static IEnumerable<ARDB.BRepBuilderEdgeGeometry> ToBRepBuilderEdgeGeometry(BrepEdge edge)
    {
      var tol = GeometryObjectTolerance.Internal;
      var edgeCurve = edge.EdgeCurve.Trim(edge.Domain) ?? edge.EdgeCurve.DuplicateCurve();
      if (edgeCurve is null || edge.IsShort(tol.VertexTolerance, edge.Domain))
      {
        GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Micro edge skipped.", edge);
        yield break;
      }

      if (edge.ProxyCurveIsReversed)
        edgeCurve.Reverse();

      edgeCurve = edgeCurve.Simplify
      (
        CurveSimplifyOptions.AdjustG1 |
        CurveSimplifyOptions.Merge,
        tol.VertexTolerance,
        tol.AngleTolerance
      ) ?? edgeCurve;

      foreach (var segment in ToEdgeCurveMany(edgeCurve))
      {
        var segmentLength = segment.Length;

        if (segmentLength <= tol.ShortCurveTolerance)
          GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry contains short edges.{Environment.NewLine}Geometry with short edges may not be as reliable as fully valid geometry.", Raw.RawDecoder.ToRhino(segment));

        if (segmentLength <= tol.VertexTolerance)
        {
#if DEBUG
          GeometryEncoder.Context.Peek.RuntimeMessage(20, $"The curve is degenerate (its length is too close to zero).", Raw.RawDecoder.ToRhino(segment));
#endif
          continue;
        }

        yield return ARDB.BRepBuilderEdgeGeometry.Create(segment);
      }
    }
    #endregion

    #region IO Support Methods
    static ARDB.Document ioDocument;
    static ARDB.Document IODocument => ioDocument.IsValid() ? ioDocument :
      ioDocument = Revit.ActiveDBApplication.NewProjectDocument(ARDB.UnitSystem.Imperial);

    static FileInfo NewSwapFileInfo(string extension)
    {
      var swapFile = Path.Combine(Core.SwapFolder, $"{Guid.NewGuid():N}.{extension}");
      return new FileInfo(swapFile);
    }

    static bool ExportGeometry(string fileName, GeometryBase geometry, double factor)
    {
      var extension = Path.GetExtension(fileName);
      if (extension is null)
        throw new ArgumentException("File name does not contain a valid extension", nameof(fileName));

      var activeDoc = RhinoDoc.ActiveDoc;
      if (activeDoc is null)
        return false;

      using (var rhinoDoc = RhinoDoc.CreateHeadless(default))
      {
        if (factor == GeometryEncoder.ModelScaleFactor)
        {
          rhinoDoc.ModelUnitSystem = activeDoc.ModelUnitSystem;
          rhinoDoc.ModelAbsoluteTolerance = activeDoc.ModelAbsoluteTolerance;
          rhinoDoc.ModelAngleToleranceRadians = activeDoc.ModelAngleToleranceRadians;
        }
        else
        {
          rhinoDoc.ModelUnitSystem = UnitSystem.Feet;
          rhinoDoc.ModelAbsoluteTolerance = activeDoc.ModelAbsoluteTolerance * factor;
          rhinoDoc.ModelAngleToleranceRadians = activeDoc.ModelAngleToleranceRadians;

          if (factor != UnitConverter.NoScale)
          {
            geometry = geometry.Duplicate();
            if (!geometry.Scale(factor)) return false;
          }
        }

        rhinoDoc.Objects.Add(geometry);

        using
        (
          var options = new Rhino.FileIO.FileWriteOptions()
          {
            FileVersion = 6,
            UpdateDocumentPath = false,
            WriteUserData = false,
            WriteGeometryOnly = false,
            SuppressAllInput = true,
            SuppressDialogBoxes = true,
            IncludeHistory = false,
            IncludeBitmapTable = false,
            IncludeRenderMeshes = false,
            IncludePreviewImage = false
          }
        )
          return rhinoDoc.WriteFile(fileName, options);
      }
    }

    static bool TryGetSolidFromInstance(ARDB.Document doc, ARDB.ElementId elementId, ARDB.XYZ center, out ARDB.Solid solid)
    {
      if (doc.GetElement(elementId) is ARDB.Element element)
      {
        using (var options = new ARDB.Options() { DetailLevel = ARDB.ViewDetailLevel.Fine })
        {
          // <see cref="ARDB.GeometryInstance.GetInstanceGeometry"/> is like calling
          // <see cref="ARDB.SolidUtils.CreateTransformed"/> on <see cref="ARDB.GeometryInstance.SymbolGeometry"/>.
          // It creates a transformed copy of the solid that will survive the Rollback.
          // Unfortunately Paint information lives in the element so even if we paint the
          // instance before doing the duplicate this information is lost after Rollback.
          if
          (
            element.get_Geometry(options) is ARDB.GeometryElement geometryElement &&
            geometryElement.First() is ARDB.GeometryInstance instance &&
            instance.GetSymbolGeometry().First() is ARDB.Solid symbolSolid
          )
          {
            var solidBBox = symbolSolid.GetBoundingBox();
            var translate = ARDB.Transform.CreateTranslation(new ARDB.XYZ(0.0, 0.0, center.Z - solidBBox.Transform.Origin.Z));
            solid = ARDB.SolidUtils.CreateTransformed(symbolSolid, translate);
            return true;
          }
        }
      }

      solid = default;
      return false;
    }
    #endregion

    #region SAT
    internal static ARDB.Solid ToSAT(/*const*/ Brep brep, double factor)
    {
      var FileSAT = NewSwapFileInfo("sat");
      try
      {
        if (ExportGeometry(FileSAT.FullName, brep, factor))
        {
          var doc = GeometryEncoder.Context.Peek.Document ?? IODocument;

          if (ARDB.ShapeImporter.IsServiceAvailable())
          {
            using (var importer = new ARDB.ShapeImporter())
            {
              var list = importer.Convert(doc, FileSAT.FullName);
              if (list.OfType<ARDB.Solid>().FirstOrDefault() is ARDB.Solid shape)
                return shape;

              GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit Data conversion service failed to import geometry", default);

              // Looks like ARDB.Document.Import do more cleaning while importing, let's try it.
              //return null;
            }
          }
          else GeometryEncoder.Context.Peek.RuntimeMessage(255, "Revit Data conversion service is not available", default);

          // In case we don't have a destination document we create a new one here.
          using (doc.IsValid() ? default : doc = Revit.ActiveDBApplication.NewProjectDocument(ARDB.UnitSystem.Imperial))
          {
            try
            {
              // Everything in this scope should be rolledback.
              using (doc.RollBackScope())
              {
                using
                (
                  var OptionsSAT = new ARDB.SATImportOptions()
                  {
                    ReferencePoint = ARDB.XYZ.Zero,
                    Placement = ARDB.ImportPlacement.Origin,
                    CustomScale = ARDB.UnitUtils.Convert
                    (
                      factor,
                      External.DB.Schemas.UnitType.Feet,
                      doc.GetUnits().GetFormatOptions(External.DB.Schemas.SpecType.Measurable.Length).GetUnitTypeId()
                    )
                  }
                )
                {
                  // Create a 3D view to import the SAT file
                  var typeId = doc.GetDefaultElementTypeId(ARDB.ElementTypeGroup.ViewType3D);
                  var view = ARDB.View3D.CreatePerspective(doc, typeId);

                  var instanceId = doc.Import(FileSAT.FullName, OptionsSAT, view);
                  var center = brep.GetBoundingBox(accurate: true).Center.ToXYZ(factor);
                  if (TryGetSolidFromInstance(doc, instanceId, center, out var solid))
                    return solid;
                }
              }
            }
            catch (Autodesk.Revit.Exceptions.OptionalFunctionalityNotAvailableException e)
            {
              GeometryEncoder.Context.Peek.RuntimeMessage(255, e.Message, default);
            }
            finally
            {
              if (!doc.IsEquivalent(ioDocument) && doc != GeometryEncoder.Context.Peek.Document)
                doc.Close(false);
            }
          }

          GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit SAT module failed to import geometry", default);
        }
        else GeometryEncoder.Context.Peek.RuntimeMessage(20, "Failed to export geometry to SAT file", default);
      }
      finally
      {
        try { FileSAT.Delete(); } catch { }
      }

      return default;
    }
    #endregion

    #region 3DM
    internal static ARDB.Solid To3DM(/*const*/ Brep brep, double factor)
    {
      var File3DM = NewSwapFileInfo("3dm");
      try
      {
        if (ExportGeometry(File3DM.FullName, brep, factor))
        {
          var doc = GeometryEncoder.Context.Peek.Document ?? IODocument;

          if (ARDB.ShapeImporter.IsServiceAvailable())
          {
            using (var importer = new ARDB.ShapeImporter())
            {
              var list = importer.Convert(doc, File3DM.FullName);
              if (list.OfType<ARDB.Solid>().FirstOrDefault() is ARDB.Solid shape)
                return shape;

              GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit Data conversion service failed to import geometry", default);

              // Looks like DB.Document.Import do more cleaning while importing, let's try it.
              //return null;
            }
          }
          else GeometryEncoder.Context.Peek.RuntimeMessage(255, "Revit Data conversion service is not available", default);

#if REVIT_2022
          // In case we don't have a destination document we create a new one here.
          using (doc.IsValid() ? default : doc = Revit.ActiveDBApplication.NewProjectDocument(ARDB.UnitSystem.Imperial))
          {
            try
            {
              // Everything in this scope should be rolledback.
              using (doc.RollBackScope())
              {
                using
                (
                  var Options3DM = new ARDB.ImportOptions3DM()
                  {
                    ReferencePoint = ARDB.XYZ.Zero,
                    Placement = ARDB.ImportPlacement.Origin,
                    CustomScale = ARDB.UnitUtils.Convert
                    (
                      factor,
                      External.DB.Schemas.UnitType.Feet,
                      doc.GetUnits().GetFormatOptions(External.DB.Schemas.SpecType.Measurable.Length).GetUnitTypeId()
                    )
                  }
                )
                {
                  // Create a 3D view to import the 3DM file
                  var typeId = doc.GetDefaultElementTypeId(ARDB.ElementTypeGroup.ViewType3D);
                  var view = ARDB.View3D.CreatePerspective(doc, typeId);

                  var instanceId = doc.Import(File3DM.FullName, Options3DM, view);
                  var center = brep.GetBoundingBox(accurate: true).Center.ToXYZ(factor);
                  if (TryGetSolidFromInstance(doc, instanceId, center, out var solid))
                    return solid;
                }
              }
            }
            catch (Autodesk.Revit.Exceptions.OptionalFunctionalityNotAvailableException e)
            {
              GeometryEncoder.Context.Peek.RuntimeMessage(255, e.Message, default);
            }
            finally
            {
              if (!doc.IsEquivalent(ioDocument) && doc != GeometryEncoder.Context.Peek.Document)
                doc.Close(false);
            }
          }

          GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit 3DM module failed to import geometry", default);
#endif
        }
        else GeometryEncoder.Context.Peek.RuntimeMessage(20, "Failed to export geometry to 3DM file", default);
      }
      finally
      {
        try { File3DM.Delete(); } catch { }
      }

      return default;
    }
    #endregion

    #region Debug
    static bool IsEquivalent(this ARDB.Solid solid, Brep brep)
    {
      var tol = GeometryObjectTolerance.Model;
      var hit = new bool[brep.Faces.Count];

      foreach (var face in solid.Faces.Cast<ARDB.Face>())
      {
        double[] samples = { 0.0, 1.0 / 5.0, 2.0 / 5.0, 3.0 / 5.0, 4.0 / 5.0, 1.0 };

        foreach (var edges in face.EdgeLoops.Cast<ARDB.EdgeArray>())
        {
          foreach (var edge in edges.Cast<ARDB.Edge>())
          {
            foreach (var sample in samples)
            {
              var point = edge.Evaluate(sample).ToPoint3d();

              if (!brep.ClosestPoint(point, out var closest, out var ci, out var s, out var t, tol.VertexTolerance, out var normal))
                return false;
            }
          }
        }

        // Get some samples
        using (var mesh = face.Triangulate(0.0))
        {
          foreach (var vertex in mesh.Vertices)
          {
            // Recover real position on face
            if (face.Project(vertex) is ARDB.IntersectionResult result)
            {
              using (result)
              {
                var point = result.XYZPoint.ToPoint3d();

                // Check if is on Brep
                if (!brep.ClosestPoint(point, out var closest, out var ci, out var s, out var t, tol.VertexTolerance, out var normal))
                  return false;

                if (ci.ComponentIndexType == ComponentIndexType.BrepFace)
                  hit[ci.Index] = true;
              }
            }
          }
        }
      }

      return !hit.Any(x => !x);
    }
    #endregion
  }
}
