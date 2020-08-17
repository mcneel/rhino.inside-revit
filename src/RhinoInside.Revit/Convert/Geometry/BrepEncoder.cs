using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using RhinoInside.Revit.Geometry.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using Units;

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
      brep.Faces.SplitClosedFaces(1);
      brep.Faces.ShrinkFaces();

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
    /// <summary>
    /// Replaces <see cref="Raw.RawEncoder.ToHost(Brep)"/> to catch Revit Exceptions
    /// </summary>
    /// <param name="brep"></param>
    /// <returns></returns>
    internal static DB.Solid ToSolid(/*const*/ Brep brep)
    {
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
          var brepEdges = new List<DB.BRepBuilderGeometryId>[brep.Edges.Count];
          foreach (var face in brep.Faces)
          {
            var faceId = builder.AddFace(Raw.RawEncoder.ToHost(face), face.OrientationIsReversed);
            builder.SetFaceMaterialId(faceId, GeometryEncoder.Context.Peek.MaterialId);

            foreach (var loop in face.Loops)
            {
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

              builder.FinishLoop(loopId);
            }

            builder.FinishFace(faceId);
          }

          var brepBuilderOutcome = builder.Finish();
          if (builder.IsResultAvailable())
            return builder.GetResult();
        }
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException e)
      {
        // TODO: Fix cases with singularities and uncomment this line
        //Debug.Fail(e.Source, e.Message);
        Debug.WriteLine(e.Message, e.Source);
      }

      return null;
    }

    static DB.Line ToEdgeCurve(LineCurve curve)
    {
      var start = curve.Line.From;
      var end = curve.Line.To;
      return DB.Line.CreateBound(new DB.XYZ(start.X, start.Y, start.Z), new DB.XYZ(end.X, end.Y, end.Z));
    }

    static IEnumerable<DB.Line> ToEdgeCurveMany(PolylineCurve curve)
    {
      int pointCount = curve.PointCount;
      if (pointCount > 1)
      {
        var point = curve.Point(0);
        DB.XYZ end, start = new DB.XYZ(point.X, point.Y, point.Z);
        for (int p = 1; p < pointCount; start = end, ++p)
        {
          point = curve.Point(p);
          end = new DB.XYZ(point.X, point.Y, point.Z);
          yield return DB.Line.CreateBound(start, end);
        }
      }
    }

    static IEnumerable<DB.Arc> ToEdgeCurveMany(ArcCurve curve)
    {
      DB.XYZ ToXYZ(Point3d p) => new DB.XYZ(p.X, p.Y, p.Z);

      if (curve.IsClosed(Revit.ShortCurveTolerance * 1.01))
      {
        var interval = curve.Domain;
        double min = interval.Min, mid = interval.Mid, max = interval.Max;
        var points = new DB.XYZ[]
        {
          ToXYZ(curve.PointAt(min)),
          ToXYZ(curve.PointAt(min + (mid - min) * 0.5)),
          ToXYZ(curve.PointAt(mid)),
          ToXYZ(curve.PointAt(mid + (max - mid) * 0.5)),
          ToXYZ(curve.PointAt(max)),
        };

        yield return DB.Arc.Create(points[0], points[2], points[1]);
        yield return DB.Arc.Create(points[2], points[4], points[3]);
      }
      else yield return DB.Arc.Create(ToXYZ(curve.Arc.StartPoint), ToXYZ(curve.Arc.EndPoint), ToXYZ(curve.Arc.MidPoint));
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

    static IEnumerable<DB.Curve> ToEdgeCurveMany(Curve curve)
    {
      switch (curve)
      {
        case LineCurve lineCurve:

          yield return ToEdgeCurve(lineCurve);
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

          foreach (var nurbs in nurbsCurve.ToCurveMany(UnitConverter.NoScale))
            yield return nurbs;
          yield break;

        default:
          if (curve.HasNurbsForm() != 0)
          {
            var nurbsForm = curve.ToNurbsCurve();
            foreach (var c in nurbsForm.ToCurveMany(UnitConverter.NoScale))
              yield return c;
          }
          else throw new ConversionException($"Unable to convert {curve} to DB.Curve");
          yield break;
      }
    }

    static IEnumerable<DB.BRepBuilderEdgeGeometry> ToBRepBuilderEdgeGeometry(BrepEdge edge)
    {
      var edgeCurve = edge.EdgeCurve.Trim(edge.Domain) ?? edge.EdgeCurve;

      if (edgeCurve is null || edge.IsShort(Revit.ShortCurveTolerance, edge.Domain))
      {
        Debug.WriteLine($"Short edge skipped, Length = {edge.GetLength(edge.Domain)}");
        return Enumerable.Empty<DB.BRepBuilderEdgeGeometry>();
      }

      if (edge.ProxyCurveIsReversed)
        edgeCurve.Reverse();

      if (edgeCurve.RemoveShortSegments(Revit.ShortCurveTolerance))
        Debug.WriteLine("Short segment removed");

      return ToEdgeCurveMany(edgeCurve).Select(x => DB.BRepBuilderEdgeGeometry.Create(x));
    }
    #endregion
  }
}
