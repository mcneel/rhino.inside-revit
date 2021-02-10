using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.Convert.Geometry
{
  /// <summary>
  /// Converts a "complex" <see cref="Brep"/> to be transfered to a <see cref="DB.Solid"/>.
  /// </summary>
  static class BrepEncoder
  {
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

      brep.Faces.SplitKinkyFaces(Revit.AngleTolerance, true);
      brep.Faces.SplitClosedFaces(2);
      brep.Faces.ShrinkFaces();

      // Normalize faces
      {
        var Identity = new Interval(0.0, 1.0);

        foreach (var face in brep.Faces)
        {
          face.SetDomain(0, Identity);
          face.SetDomain(1, Identity);
        }

        brep.SetTrimIsoFlags();
      }

      return brep.IsValid;
    }

    static bool SplitFaces(ref Brep brep)
    {
      Brep brepToSplit = null;
      while (!ReferenceEquals(brepToSplit, brep))
      {
        brepToSplit = brep;

        foreach (var face in brepToSplit.Faces)
        {
          var splitters = new List<Curve>();

          var trimsBBox = BoundingBox.Empty;
          foreach (var trim in face.OuterLoop.Trims)
            trimsBBox.Union(trim.GetBoundingBox(true));

          var domainUV = new Interval[]
          {
            new Interval(trimsBBox.Min.X, trimsBBox.Max.X),
            new Interval(trimsBBox.Min.Y, trimsBBox.Max.Y),
          };

          // Compute splitters
          var splittedUV = new bool[2] { false, false };
          for (int d = 0; d < 2; d++)
          {
            var domain = domainUV[d];
            var t = domain.Min;

            while (face.GetNextDiscontinuity(d, Continuity.Gsmooth_continuous, t, domain.Max, out t))
            {
              splitters.AddRange(face.TrimAwareIsoCurve(1 - d, t));
              splittedUV[d] = true;
            }
          }

          var closedUV = new bool[2] { face.IsClosed(0), face.IsClosed(1) };
          if (!splittedUV[0] && closedUV[0])
          {
            splitters.AddRange(face.TrimAwareIsoCurve(1, face.Domain(0).Mid));
            splittedUV[0] = true;
          }
          if (!splittedUV[1] && closedUV[1])
          {
            splitters.AddRange(face.TrimAwareIsoCurve(0, face.Domain(1).Mid));
            splittedUV[1] = true;
          }

          if (splitters.Count > 0)
          {
            var surfaceIndex = face.SurfaceIndex;
            var splitted = face.Split(splitters, Revit.ShortCurveTolerance);
            if (splitted is null)
            {
              Debug.Fail("BrepFace.Split", "Failed to split a closed face.");
              return false;
            }

            if (brepToSplit.Faces.Count == splitted.Faces.Count)
            {
              // Split was ok but for tolerance reasons no new faces were created.
              // Too near from the limits.
            }
            else
            {

              foreach (var f in splitted.Faces)
              {
                if (f.SurfaceIndex != surfaceIndex)
                  continue;

                if (splittedUV[0] && splittedUV[1])
                  f.ShrinkFace(BrepFace.ShrinkDisableSide.ShrinkAllSides);
                else if (splittedUV[0])
                  f.ShrinkFace(BrepFace.ShrinkDisableSide.DoNotShrinkSouthSide | BrepFace.ShrinkDisableSide.DoNotShrinkNorthSide);
                else if (splittedUV[1])
                  f.ShrinkFace(BrepFace.ShrinkDisableSide.DoNotShrinkEastSide | BrepFace.ShrinkDisableSide.DoNotShrinkWestSide);
              }

              // Start again until no face is splitted
              brep = splitted;
              break;
            }
          }
        }
      }

      return brep is object;
    }
    #endregion

    #region Transfer
    internal static DB.Solid ToSolid(/*const*/Brep brep, double factor)
    {
      // Try using DB.BRepBuilder
      if (ToSolid(ToRawBrep(brep, factor)) is DB.Solid solid)
        return solid;

      // Try using DB.ShapeImporter or DB.Document.Import
      Debug.WriteLine("Try exporting-importing as ACIS.");
      GeometryEncoder.Context.Peek.RuntimeMessage(255, "Using SAT…", default);
      if (ToACIS(brep, factor) is DB.Solid sat) return sat;
      else GeometryEncoder.Context.Peek.RuntimeMessage(20, "SAT operation failed.", default);

      return default;
    }

    internal static DB.Mesh ToMesh(/*const*/ Brep brep, double factor)
    {
      using (var mp = MeshingParameters.Default)
      {
        mp.MinimumEdgeLength = Revit.ShortCurveTolerance * factor;
        mp.ClosedObjectPostProcess = brep.IsManifold;
        mp.JaggedSeams = false;

        if (Mesh.CreateFromBrep(brep, mp) is Mesh[] shells)
          return MeshEncoder.ToMesh(shells, factor);

        return default;
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

                if (edge.Tolerance > Revit.VertexTolerance)
                {
                  error = true;
                  GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry contains out of tolerance edges.{Environment.NewLine}Resulting geometry may not be accurate.", edge);
                }

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
      var curve = line.ToLine(factor);

      return isShort ? (DB.Line) curve.CreateTransformed(DB.Transform.Identity.ScaleBasis(length)) : curve;
    }

    static DB.Arc ToEdgeCurve(Arc arc)
    {
      var length = arc.Length;
      bool isShort = length < Revit.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;
      var curve = arc.ToArc(factor);

      return isShort ? (DB.Arc) curve.CreateTransformed(DB.Transform.Identity.ScaleBasis(length)) : curve;
    }

    static DB.Curve ToEdgeCurve(NurbsCurve nurbs)
    {
      var length = nurbs.GetLength();
      bool isShort = length < Revit.ShortCurveTolerance;
      var factor = isShort ? 1.0 / length : UnitConverter.NoScale;
      var curve = NurbsSplineEncoder.ToNurbsSpline(nurbs, factor);

      return isShort ? curve.CreateTransformed(DB.Transform.Identity.ScaleBasis(length)) : curve;
    }

    static IEnumerable<DB.Line> ToEdgeCurveMany(PolylineCurve curve)
    {
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
      int segmentCount = curve.SegmentCount;
      for (int s = 0; s < segmentCount; ++s)
      {
        foreach (var segment in ToEdgeCurveMany(curve.SegmentCurve(s)))
          yield return segment;
      }
    }

    static IEnumerable<DB.Curve> ToEdgeCurveMany(NurbsCurve curve)
    {
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
      }
      else if (curve.Degree == 2)
      {
        for (int s = 0; s < curve.SpanCount; ++s)
        {
          var segment = curve.Trim(curve.SpanDomain(s)) as NurbsCurve;
          yield return ToEdgeCurve(segment);
        }
      }
      else if (curve.GetNextDiscontinuity(Continuity.C1_continuous, curve.Domain.Min, curve.Domain.Max, out var t))
      {
        var splitters = new List<double>() { t };
        while (curve.GetNextDiscontinuity(Continuity.C1_continuous, t, curve.Domain.Max, out t))
          splitters.Add(t);

        var segments = curve.Split(splitters);
        foreach (var segment in segments.Select(x => x.ToNurbsCurve()))
          yield return ToEdgeCurve(segment);
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

      var RuntimeMessage = GeometryEncoder.Context.Peek.RuntimeMessage;

      if (edgeCurve is null || edge.IsShort(Revit.VertexTolerance, edge.Domain))
      {
        RuntimeMessage(10, $"Micro edge skipped.", edge);
        yield break;
      }

      if (edge.ProxyCurveIsReversed)
        edgeCurve.Reverse();

      if (edgeCurve is PolyCurve poly)
        poly.RemoveNesting();

      if (edgeCurve.RemoveShortSegments(Revit.VertexTolerance))
      {
#if DEBUG
        RuntimeMessage(10, "Edge micro-segment removed.", edge);
#endif
      }

      foreach (var segment in ToEdgeCurveMany(edgeCurve))
      {
        if(segment.Length < Revit.ShortCurveTolerance)
          RuntimeMessage(10, $"Geometry contains short edges.{Environment.NewLine}Geometry with short edges may not be as reliable as fully valid geometry.", Raw.RawDecoder.ToRhino(segment));

        yield return DB.BRepBuilderEdgeGeometry.Create(segment);
      }
    }

    static DB.Document acisDocument;
    static DB.Document ACISDocument => acisDocument.IsValid() ? acisDocument :
      acisDocument = Revit.ActiveDBApplication.NewProjectDocument(DB.UnitSystem.Imperial);

    internal static DB.Solid ToACIS(/*const*/ Brep brep, double factor)
    {
      var TempFolder = Path.Combine(Path.GetTempPath(), Addin.AddinCompany, Addin.AddinName, $"V{RhinoApp.ExeVersion}", "SATCaches");
      Directory.CreateDirectory(TempFolder);

      var SATFile = Path.Combine(TempFolder, $"{Guid.NewGuid():N}.sat");

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
          RhinoApp.RunScript($@"_-Export ""{SATFile}"" ""Default"" _Enter", false);
        }
        finally
        {
          activeModel.Objects.Delete(objectGUID, true);
          activeModel.Views.RedrawEnabled = redrawEnabled;
          Rhinoceros.MainWindow.Enabled = rhinoWindowEnabled;
        }
      }

      // Import
      if (File.Exists(SATFile))
      {
        try
        {
          var doc = GeometryEncoder.Context.Peek.Document ?? ACISDocument;

          if (DB.ShapeImporter.IsServiceAvailable())
          {
            using (var importer = new DB.ShapeImporter())
            {
              var list = importer.Convert(doc, SATFile);
              if (list.OfType<DB.Solid>().FirstOrDefault() is DB.Solid shape)
                return shape;

              // Looks like DB.ShapeImporter do not support short edges geometry
              //return null;
            }
          }

          // In case we don't have a  destination document we create a new one here.
          using (doc.IsValid() ? default : doc = Revit.ActiveDBApplication.NewProjectDocument(DB.UnitSystem.Imperial))
          {
            try
            {
              // Everything in this scope should be rolledback.
              using (doc.RollBackScope())
              {
                using
                (
                  var SATOptions = new DB.SATImportOptions()
                  {
                    ReferencePoint = DB.XYZ.Zero,
                    Placement = DB.ImportPlacement.Origin,
                    CustomScale = DB.UnitUtils.Convert(factor, DB.DisplayUnitType.DUT_DECIMAL_FEET, doc.GetUnits().GetFormatOptions(DB.UnitType.UT_Length).DisplayUnits),
                  }
                )
                {
                  // Create a 3D view to 
                  var typeId = doc.GetDefaultElementTypeId(DB.ElementTypeGroup.ViewType3D);
                  var view = DB.View3D.CreatePerspective(doc, typeId);

                  var instanceId = doc.Import(SATFile, SATOptions, view);
                  if (doc.GetElement(instanceId) is DB.Element element)
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
                        instance.GetInstanceGeometry().First() is DB.Solid solid
                      )
                      {
                        return DB.SolidUtils.Clone(solid);
                      }
                    }
                  }
                }
              }
            }
            finally
            {
              if (doc != ACISDocument && doc != GeometryEncoder.Context.Peek.Document)
                doc.Close(false);
            }
          }
        }
        finally
        {
          try { File.Delete(SATFile); } catch { }
        }
      }

      return default;
    }

    internal static bool IsEquivalent(this DB.Solid solid, Brep brep)
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

    #endregion
  }
}
