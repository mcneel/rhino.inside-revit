using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  /// <summary>
  /// Converts a "complex" <see cref="Brep"/> to be transfered to a <see cref="DB.Solid"/>.
  /// </summary>
  static class BrepEncoder
  {
    #region Tolerances
    static double JoinTolerance => Math.Sqrt(Revit.VertexTolerance * Revit.VertexTolerance + Revit.VertexTolerance * Revit.VertexTolerance);
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

      var bbox = brep.GetBoundingBox(false);
      if (!bbox.IsValid || bbox.Diagonal.Length < Revit.ShortCurveTolerance)
        return default;

      // Split and Shrink faces
      {
        brep.Faces.SplitKinkyFaces(Revit.AngleTolerance, true);
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

      // Edges
      {
        foreach (var edge in brep.Edges)
        {
          if (edge.Tolerance > Revit.VertexTolerance)
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
              edges.SplitKinkyEdge(ei, Revit.AngleTolerance);

            kinkyEdges += edges.Count - edgeCount;
            microEdges += edges.RemoveNakedMicroEdges(Revit.VertexTolerance);
            mergedEdges += edges.MergeAllEdges(Revit.AngleTolerance);
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
        var join = Brep.JoinBreps(shells, Revit.VertexTolerance);
        if (join.Length == 1) brep = join[0];
        else
        {
          var merge = new Brep();
          foreach (var shell in join)
            merge.Append(shell);

          //var joined = merge.JoinNakedEdges(Revit.VertexTolerance);
          brep = merge;
        }

#if DEBUG
        foreach (var edge in brep.Edges)
        {
          if (edge.Tolerance > Revit.VertexTolerance)
            GeometryEncoder.Context.Peek.RuntimeMessage(255, $"DEBUG - Geometry contains out of tolerance edges", edge);
        }
#endif
      }

      return brep.IsValid;
    }
#endregion

    #region Transfer
    internal static DB.Mesh ToMesh(/*const*/ Brep brep, double factor)
    {
      using (var mp = MeshingParameters.Default)
      {
        mp.MinimumEdgeLength = Revit.ShortCurveTolerance / factor;
        mp.ClosedObjectPostProcess = brep.IsManifold;
        mp.JaggedSeams = false;

        if (Mesh.CreateFromBrep(brep, mp) is Mesh[] shells)
          return MeshEncoder.ToMesh(shells, factor);

        return default;
      }
    }

    internal static DB.Solid ToSolid(/*const*/Brep brep, double factor)
    {
      // Try on existing solids already in memory.
      if (GeometryCache.TryGetExistingGeometry(brep, factor, out DB.Solid existing, out var hash))
      {
        GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Using cached value {GeometryCache.HashToString(hash)}…", default);
        AuditSolid(brep, existing);
        return existing;
      }

      // Try using DB.BRepBuilder
      {
        var raw = ToRawBrep(brep, factor);

        if (ToSolid(raw) is DB.Solid solid)
        {
          AuditSolid(brep, solid);
          GeometryCache.AddExistingGeometry(hash, solid);
          return solid;
        }
      }

      // Try using DB.ShapeImporter | DB.Document.Import
      {
        GeometryEncoder.Context.Peek.RuntimeMessage(255, "Using 3DM…", default);

        if (To3DM(brep, factor) is DB.Solid solid)
        {
          AuditSolid(brep, solid);
          GeometryCache.AddExistingGeometry(hash, solid);
          return solid;
        }
      }

      GeometryEncoder.Context.Peek.RuntimeMessage(20, "Failed to convert geometry.", brep.InHostUnits());
      return default;
    }

    static void AuditSolid(Brep brep, DB.Solid solid)
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
    }

    /// <summary>
    /// Replaces <see cref="Raw.RawEncoder.ToHost(Brep)"/> to catch Revit Exceptions
    /// </summary>
    /// <param name="brep"></param>
    /// <returns></returns>
    internal static DB.Solid ToSolid(/*const*/ Brep brep)
    {
      if (brep is null)
        return null;

      try
      {
        var brepType = DB.BRepType.OpenShell;
        switch (brep.SolidOrientation)
        {
          case BrepSolidOrientation.Inward: brepType = DB.BRepType.Void; break;
          case BrepSolidOrientation.Outward: brepType = DB.BRepType.Solid; break;
        }

        using (var builder = new DB.BRepBuilder(brepType))
        {
#if REVIT_2018
          builder.SetAllowShortEdges();
          builder.AllowRemovalOfProblematicFaces();
#endif

          var brepEdges = new List<DB.BRepBuilderGeometryId>[brep.Edges.Count];
          foreach (var face in brep.Faces)
          {
            var surfaceGeom = default(DB.BRepBuilderSurfaceGeometry);
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
                    edgeIds = brepEdges[edge.EdgeIndex] = new List<DB.BRepBuilderGeometryId>();
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
                  var message = e.Message.Replace("(as identified by Application.ShortCurveTolerance)", $"({Revit.ShortCurveTolerance * Revit.ModelUnits})");
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

    static DB.Line ToEdgeCurve(Line line)
    {
      var length = line.Length;
      bool isShort = length < Revit.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;

      var curve = DB.Line.CreateBound
      (
        line.From.ToXYZ(factor),
        line.To.ToXYZ(factor)
      );

      return isShort ? (DB.Line) curve.CreateTransformed(DB.Transform.Identity.ScaleBasis(length)) : curve;
    }

    static DB.Arc ToEdgeCurve(Arc arc)
    {
      var length = arc.Length;
      bool isShort = length < Revit.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;

      var curve = DB.Arc.Create
      (
        arc.StartPoint.ToXYZ(factor),
        arc.EndPoint.ToXYZ(factor),
        arc.MidPoint.ToXYZ(factor)
      );

      return isShort ? (DB.Arc) curve.CreateTransformed(DB.Transform.Identity.ScaleBasis(length)) : curve;
    }

    static DB.Curve ToEdgeCurve(NurbsCurve nurbs)
    {
      var length = nurbs.GetLength();
      bool isShort = length < Revit.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;

      var degree = nurbs.Degree;
      var knots = Raw.RawEncoder.ToHost(nurbs.Knots);

      var rational = nurbs.IsRational;
      var points = nurbs.Points;
      var count = points.Count;
      var controlPoints = new DB.XYZ[count];
      var weights = rational ? new double[count] : default;

      for (int p = 0; p < count; ++p)
      {
        var location = points[p].Location;
        controlPoints[p] = new DB.XYZ(location.X * factor, location.Y * factor, location.Z * factor);
        if (rational) weights[p] = points[p].Weight;
      }

      var curve = rational ?
        DB.NurbSpline.CreateCurve(degree, knots, controlPoints, weights) :
        DB.NurbSpline.CreateCurve(degree, knots, controlPoints);

      curve = isShort ? curve.CreateTransformed(DB.Transform.Identity.ScaleBasis(1.0 / factor)) : curve;
      return curve;
    }

    static IEnumerable<DB.Curve> ToEdgeCurve(PolyCurve curve)
    {
      if (curve.RemoveShortSegments(Revit.VertexTolerance))
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

    static IEnumerable<DB.Line> ToEdgeCurveMany(PolylineCurve curve)
    {
      if (curve.RemoveShortSegments(Revit.VertexTolerance))
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

    static IEnumerable<DB.Arc> ToEdgeCurveMany(ArcCurve curve)
    {
      if (curve.IsClosed(Revit.ShortCurveTolerance * 1.01))
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

    static IEnumerable<DB.Curve> ToEdgeCurveMany(PolyCurve curve)
    {
      if (curve.RemoveShortSegments(Revit.VertexTolerance))
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

    static IEnumerable<DB.Curve> ToEdgeCurveMany(NurbsCurve curve)
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
      else if (curve.IsClosed(Revit.ShortCurveTolerance * 1.01))
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

    static IEnumerable<DB.Curve> ToEdgeCurveMany(Curve curve)
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
          else throw new ConversionException($"Unable to convert {curve} to DB.Curve");
          yield break;
      }
    }

    static IEnumerable<DB.BRepBuilderEdgeGeometry> ToBRepBuilderEdgeGeometry(BrepEdge edge)
    {
      var edgeCurve = edge.EdgeCurve.Trim(edge.Domain) ?? edge.EdgeCurve.DuplicateCurve();
      if (edgeCurve is null || edge.IsShort(Revit.VertexTolerance, edge.Domain))
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
        Revit.VertexTolerance,
        Revit.AngleTolerance
      ) ?? edgeCurve;

      foreach (var segment in ToEdgeCurveMany(edgeCurve))
      {
        var segmentLength = segment.Length;

        if (segmentLength <= Revit.ShortCurveTolerance)
          GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry contains short edges.{Environment.NewLine}Geometry with short edges may not be as reliable as fully valid geometry.", Raw.RawDecoder.ToRhino(segment));

        if (segmentLength <= Revit.VertexTolerance)
        {
#if DEBUG
          GeometryEncoder.Context.Peek.RuntimeMessage(20, $"The curve is degenerate (its length is too close to zero).", Raw.RawDecoder.ToRhino(segment));
#endif
          continue;
        }

        yield return DB.BRepBuilderEdgeGeometry.Create(segment);
      }
    }
    #endregion

    #region IO Support Methods
    static DB.Document ioDocument;
    static DB.Document IODocument => ioDocument.IsValid() ? ioDocument :
      ioDocument = Revit.ActiveDBApplication.NewProjectDocument(DB.UnitSystem.Imperial);

    static bool TryGetSolidFromInstance(DB.Document doc, DB.ElementId elementId, out DB.Solid solid)
    {
      if (doc.GetElement(elementId) is DB.Element element)
      {
        using (var options = new DB.Options() { DetailLevel = DB.ViewDetailLevel.Fine })
        {
          /// <see cref="DB.GeometryInstance.GetInstanceGeometry"/> is like calling
          /// <see cref="DB.SolidUtils.CreateTransformed"/> on <see cref="DB.GeometryInstance.SymbolGeometry"/>.
          /// It creates a transformed copy of the solid that will survive the Rollback.
          /// Unfortunately Paint information lives in the element so even if we paint the
          /// instance before doing the duplicate this information is lost after Rollback.
          if
          (
            element.get_Geometry(options) is DB.GeometryElement geometryElement &&
            geometryElement.First() is DB.GeometryInstance instance &&
            instance.GetInstanceGeometry().First() is DB.Solid instanceSolid
          )
          {
            solid = DB.SolidUtils.Clone(instanceSolid);
            return true;
          }
        }
      }

      solid = default;
      return false;
    }
    #endregion

    #region SAT
    internal static DB.Solid ToSAT(/*const*/ Brep brep, double factor)
    {
      var TempFolder = Path.Combine(Path.GetTempPath(), AddIn.AddInCompany, AddIn.AddInName, $"V{RhinoApp.ExeVersion}", "IOCaches");
      Directory.CreateDirectory(TempFolder);

      var FileSAT = Path.Combine(TempFolder, $"{Guid.NewGuid():N}.sat");

      // Export
      {
        var activeModel = RhinoDoc.ActiveDoc;
        var rhinoWindowEnabled = Rhinoceros.MainWindow.Enabled;
        var redrawEnabled = activeModel.Views.RedrawEnabled;
        var objectGUID = default(Guid);
        try
        {
          Rhinoceros.MainWindow.Enabled = false;
          activeModel.Views.RedrawEnabled = false;
          activeModel.Objects.UnselectAll();

          objectGUID = activeModel.Objects.Add(brep);

          activeModel.Objects.Select(objectGUID);
          RhinoApp.RunScript($@"_-Export ""{FileSAT}"" ""Default"" _Enter", false);
        }
        finally
        {
          activeModel.Objects.Delete(objectGUID, true);
          activeModel.Views.RedrawEnabled = redrawEnabled;
          Rhinoceros.MainWindow.Enabled = rhinoWindowEnabled;
        }
      }

      // Import
      if (File.Exists(FileSAT))
      {
        try
        {
          var doc = GeometryEncoder.Context.Peek.Document ?? IODocument;

          if (DB.ShapeImporter.IsServiceAvailable())
          {
            using (var importer = new DB.ShapeImporter())
            {
              var list = importer.Convert(doc, FileSAT);
              if (list.OfType<DB.Solid>().FirstOrDefault() is DB.Solid shape)
                return shape;

              GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit Data conversion service failed to import geometry", default);

              // Looks like DB.ShapeImporter do not support short edges geometry
              //return null;
            }
          }
          else GeometryEncoder.Context.Peek.RuntimeMessage(255, "Revit Data conversion service is not available", default);

          // In case we don't have a destination document we create a new one here.
          using (doc.IsValid() ? default : doc = Revit.ActiveDBApplication.NewProjectDocument(DB.UnitSystem.Imperial))
          {
            try
            {
              // Everything in this scope should be rolledback.
              using (doc.RollBackScope())
              {
                using
                (
                  var OptionsSAT = new DB.SATImportOptions()
                  {
                    ReferencePoint = DB.XYZ.Zero,
                    Placement = DB.ImportPlacement.Origin,
                    CustomScale = DB.UnitUtils.Convert
                    (
                      factor,
                      External.DB.Schemas.UnitType.Feet,
                      doc.GetUnits().GetFormatOptions(External.DB.Schemas.SpecType.Measurable.Length).GetUnitTypeId()
                    )
                  }
                )
                {
                  // Create a 3D view to import the SAT file
                  var typeId = doc.GetDefaultElementTypeId(DB.ElementTypeGroup.ViewType3D);
                  var view = DB.View3D.CreatePerspective(doc, typeId);

                  var instanceId = doc.Import(FileSAT, OptionsSAT, view);
                  if (TryGetSolidFromInstance(doc, instanceId, out var solid))
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
              if (doc != IODocument && doc != GeometryEncoder.Context.Peek.Document)
                doc.Close(false);
            }
          }
        }
        finally
        {
          try { File.Delete(FileSAT); } catch { }
        }

        GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit SAT module failed to import geometry", default);
      }
      else GeometryEncoder.Context.Peek.RuntimeMessage(20, "Failed to export geometry to SAT file", default);

      return default;
    }
    #endregion

    #region 3DM
    internal static DB.Solid To3DM(/*const*/ Brep brep, double factor)
    {
      var TempFolder = Path.Combine(Path.GetTempPath(), AddIn.AddInCompany, AddIn.AddInName, $"V{RhinoApp.ExeVersion}", "IOCaches");
      Directory.CreateDirectory(TempFolder);

      var File3DM = Path.Combine(TempFolder, $"{Guid.NewGuid():N}.3dm");

      // Export
      if (RhinoDoc.ActiveDoc is RhinoDoc activeDoc)
      {
        using (var rhinoDoc = RhinoDoc.CreateHeadless(null))
        {
          if (factor == UnitConverter.ToHostUnits)
          {
            rhinoDoc.ModelUnitSystem = activeDoc.ModelUnitSystem;
            rhinoDoc.ModelAbsoluteTolerance = activeDoc.ModelAbsoluteTolerance;
            rhinoDoc.ModelAngleToleranceRadians = activeDoc.ModelAngleToleranceRadians;

            rhinoDoc.Objects.Add(brep);
          }
          else
          {
            rhinoDoc.ModelUnitSystem = UnitSystem.Feet;
            rhinoDoc.ModelAbsoluteTolerance = activeDoc.ModelAbsoluteTolerance * factor;
            rhinoDoc.ModelAngleToleranceRadians = activeDoc.ModelAngleToleranceRadians;

            if (factor != 1.0)
            {
              brep = brep.DuplicateBrep();
              if (!brep.Scale(factor)) return default;
            }
            
            rhinoDoc.Objects.Add(brep);
          }

          rhinoDoc.SaveAs(File3DM);
        }
      }

      // Import
      if (File.Exists(File3DM))
      {
        try
        {
          var doc = GeometryEncoder.Context.Peek.Document ?? IODocument;

          if (DB.ShapeImporter.IsServiceAvailable())
          {
            using (var importer = new DB.ShapeImporter())
            {
              var list = importer.Convert(IODocument, File3DM);
              if (list.OfType<DB.Solid>().FirstOrDefault() is DB.Solid shape)
                return shape;

              GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit Data conversion service failed to import geometry", default);

              // Looks like DB.ShapeImporter do not support short edges geometry
              //return null;
            }
          }
          else GeometryEncoder.Context.Peek.RuntimeMessage(255, "Revit Data conversion service is not available", default);

#if REVIT_2022
          // In case we don't have a destination document we create a new one here.
          using (doc.IsValid() ? default : doc = Revit.ActiveDBApplication.NewProjectDocument(DB.UnitSystem.Imperial))
          {
            try
            {
              // Everything in this scope should be rolledback.
              using (doc.RollBackScope())
              {
                using
                (
                  var Options3DM = new DB.ImportOptions3DM()
                  {
                    ReferencePoint = DB.XYZ.Zero,
                    Placement = DB.ImportPlacement.Origin,
                    CustomScale = DB.UnitUtils.Convert
                    (
                      factor,
                      External.DB.Schemas.UnitType.Feet,
                      doc.GetUnits().GetFormatOptions(External.DB.Schemas.SpecType.Measurable.Length).GetUnitTypeId()
                    )
                  }
                )
                {
                  // Create a 3D view to import the 3DM file
                  var typeId = doc.GetDefaultElementTypeId(DB.ElementTypeGroup.ViewType3D);
                  var view = DB.View3D.CreatePerspective(doc, typeId);

                  var instanceId = doc.Import(File3DM, Options3DM, view);
                  if (TryGetSolidFromInstance(doc, instanceId, out var solid))
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
              if (doc != IODocument && doc != GeometryEncoder.Context.Peek.Document)
                doc.Close(false);
            }
          }
#endif
        }
        finally
        {
          try { File.Delete(File3DM); } catch { }
        }

        GeometryEncoder.Context.Peek.RuntimeMessage(10, "Revit 3DM module failed to import geometry", default);
      }
      else GeometryEncoder.Context.Peek.RuntimeMessage(20, "Failed to export geometry to 3DM file", default);

      return default;
    }
    #endregion
  }

  static class GeometryCache
  {
    // True 
    enum CachePolicy
    {
      Disabled = 0,     // Caching is disabled
      Memory = 1,       // Memory over performance (Not Implemented)
      Performance = 2,  // Performance over memory (Not Implemented)
      Extreme = 3       // Does not allow .NET to collect any geometry
    }
    static CachePolicy Policy = CachePolicy.Performance;

    class SoftReference<T> : WeakReference where T : class
    {
      private object reference;

      public SoftReference(T value) : base(value) { }

      public override object Target
      {
        get => reference is object ? reference : base.Target;
        set
        {
          if (reference is object) throw new InvalidOperationException("Reference is keeping the object alive");
          base.Target = (T) value;
        }
      }

      public T Value
      {
        get => (T) Target;
        set => Target = value;
      }

      public bool KeepAlive
      {
        get => reference is object;
        set => reference = value ? Target : default;
      }

      public bool Hit = false;
    }

    struct HashComparer : IEqualityComparer<byte[]>
    {
      public bool Equals(byte[] x, byte[] y)
      {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        var length = x.Length;
        if (length != y.Length) return false;

        for (int i = 0; i < length; ++i)
          if (x[i] != y[i]) return false;

        return true;
      }

      public int GetHashCode(byte[] obj)
      {
        int hash = 0;

        if (obj is object)
        {
          for (int i = 0; i < obj.Length; ++i)
          {
            var value = (int) obj[i];
            hash ^= (value << 5) + value;
          }
        }

        return hash;
      }
    }

    static readonly Dictionary<byte[], SoftReference<DB.GeometryObject>> GeometryDictionary =
      new Dictionary<byte[], SoftReference<DB.GeometryObject>>(default(HashComparer));

    internal static void StartKeepAliveRegion()
    {
      if (Policy == CachePolicy.Disabled)
      {
        GeometryDictionary.Clear();
      }
      else if (Policy != CachePolicy.Extreme)
      {
        foreach (var value in GeometryDictionary.Values)
        {
          value.KeepAlive = true;
          value.Hit = false;
        }
      }
    }

    internal static void EndKeepAliveRegion()
    {
      if (Policy == CachePolicy.Disabled) return;

      // Mark non hitted references as collectable
      if (Policy != CachePolicy.Extreme)
      {
        foreach (var entry in GeometryDictionary)
        {
          entry.Value.KeepAlive = entry.Value.Hit;
          entry.Value.Hit = false;
        }

        //GC.Collect();
      }

      // Collect unreferenced entries
      {
        var purge = new List<byte[]>();
        foreach (var entry in GeometryDictionary)
        {
          if (!entry.Value.IsAlive)
            purge.Add(entry.Key);
        }

        foreach (var key in purge)
          GeometryDictionary.Remove(key);
      }

#if DEBUG
      Grasshopper.Instances.DocumentEditor.SetStatusBarEvent
      (
        new Grasshopper.Kernel.GH_RuntimeMessage
        (
          $"'{GeometryDictionary.Count}' solids in cache.",
          Grasshopper.Kernel.GH_RuntimeMessageLevel.Remark
        )
      );
#endif
    }

    internal static string HashToString(byte[] hash)
    {
      var hex = new global::System.Text.StringBuilder(hash.Length * 2);
      foreach (var b in hash)
        hex.AppendFormat("{0:x2}", b);

      return hex.ToString();
    }

    static byte[] GetGeometryHashCode(GeometryBase geometry, double factor)
    {
      float Round(double value) => (float) (value * factor);

      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          switch (geometry)
          {
            case Brep brep:

              writer.Write(typeof(Brep).Name);
              writer.Write(brep.Faces.Count);
              writer.Write(brep.Surfaces.Count);
              writer.Write(brep.Edges.Count);
              writer.Write(brep.Curves3D.Count);

              foreach (var face in brep.Faces)
                writer.Write(face.OrientationIsReversed);

              foreach (var surface in brep.Surfaces)
              {
                var nurbs = surface as NurbsSurface ?? surface.ToNurbsSurface(Revit.VertexTolerance * factor, out var accuracy);
                writer.Write(nurbs.OrderU);
                writer.Write(nurbs.OrderV);
                var rational = nurbs.IsRational;
                writer.Write(nurbs.IsRational);
                writer.Write(nurbs.Points.CountU);
                writer.Write(nurbs.Points.CountV);
                foreach (var point in nurbs.Points)
                {
                  var location = point.Location;
                  writer.Write(Round(location.X));
                  writer.Write(Round(location.Y));
                  writer.Write(Round(location.Z));
                  if (rational) writer.Write(point.Weight);
                }
                foreach (var knot in nurbs.KnotsU) writer.Write(knot);
                foreach (var knot in nurbs.KnotsV) writer.Write(knot);
              }

              foreach (var edge in brep.Edges)
              {
                var domain = edge.Domain;
                writer.Write(domain.T0);
                writer.Write(domain.T1);
              }

              foreach (var curve in brep.Curves3D)
              {
                var nurbs = curve as NurbsCurve ?? curve.ToNurbsCurve();
                writer.Write(nurbs.Order);
                var rational = nurbs.IsRational;
                writer.Write(nurbs.IsRational);
                writer.Write(nurbs.Points.Count);
                foreach (var point in nurbs.Points)
                {
                  var location = point.Location;
                  writer.Write(Round(location.X));
                  writer.Write(Round(location.Y));
                  writer.Write(Round(location.Z));
                  if (rational) writer.Write(point.Weight);
                }
                foreach (var knot in nurbs.Knots) writer.Write(knot);
              }

              break;

            default: throw new NotImplementedException();
          }

          writer.Flush();
        }

        using (var sha1 = new global::System.Security.Cryptography.SHA1Managed())
          return sha1.ComputeHash(stream.GetBuffer());
      }
    }

    internal static void AddExistingGeometry(byte[] hash, DB.GeometryObject to)
    {
      if (hash is object && to is object) GeometryDictionary.Add
      (
        hash,
        new SoftReference<DB.GeometryObject>(to) { KeepAlive = true, Hit = true }
      );
    }

    internal static bool TryGetExistingGeometry<R, T>(/*const*/ R from, double factor, out T to, out byte[] hash)
      where R : GeometryBase
      where T : DB.GeometryObject
    {
      if (Policy == CachePolicy.Disabled)
      {
        hash = default;
      }
      else
      {
        hash = GetGeometryHashCode(from, factor);
        if (GeometryDictionary.TryGetValue(hash, out var reference))
        {
          reference.KeepAlive = true;

          if (reference.IsAlive)
          {
            reference.Hit = true;
            to = (T) reference.Value;
            return true;
          }

          GeometryDictionary.Remove(hash);
        }
      }

      to = default;
      return false;
    }

    static bool IsEquivalent(this DB.Solid solid, Brep brep)
    {
      var hit = new bool[brep.Faces.Count];

      foreach (var face in solid.Faces.Cast<DB.Face>())
      {
        double[] samples = { 0.0, 1.0 / 5.0, 2.0 / 5.0, 3.0 / 5.0, 4.0 / 5.0, 1.0};

        foreach (var edges in face.EdgeLoops.Cast<DB.EdgeArray>())
        {
          foreach (var edge in edges.Cast<DB.Edge>())
          {
            foreach (var sample in samples)
            {
              var point = edge.Evaluate(sample).ToPoint3d();

              if (!brep.ClosestPoint(point, out var closest, out var ci, out var s, out var t, Revit.VertexTolerance * Revit.ModelUnits, out var normal))
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
            if (face.Project(vertex) is DB.IntersectionResult result)
            {
              using (result)
              {
                var point = result.XYZPoint.ToPoint3d();

                // Check if is on Brep
                if (!brep.ClosestPoint(point, out var closest, out var ci, out var s, out var t, Revit.VertexTolerance * Revit.ModelUnits, out var normal))
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
  }
}
