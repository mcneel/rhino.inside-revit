using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using global::System;
  using global::System.Collections.Generic;

  /// <summary>
  /// Converts <see cref="Mesh"/> to be transfered to a <see cref="DB.Mesh"/>.
  /// </summary>
  static class MeshEncoder
  {
    #region Tolerances
    static readonly double ShortEdgeTolerance = 2.0 * Revit.VertexTolerance;
    #endregion

    #region Encode
    internal static Mesh ToRawMesh(/*const*/ Mesh mesh, double scaleFactor)
    {
      mesh = mesh.DuplicateShallow() as Mesh;
      return EncodeRaw(ref mesh, scaleFactor) ? mesh : default;
    }

    internal static Brep ToRawBrep(/*const*/ Mesh mesh, double scaleFactor)
    {
      mesh = mesh.DuplicateShallow() as Mesh;
      if (EncodeRaw(ref mesh, scaleFactor))
      {
        // Revit needs Solid edges to be greater than Revit.ShortCurveTolerance
        mesh.Vertices.Align(Revit.ShortCurveTolerance, default);
        mesh.Vertices.Align(Revit.ShortCurveTolerance, mesh.GetNakedEdgePointStatus().Select(x => !x));
        mesh.Weld(Revit.AngleTolerance);

        return Brep.CreateFromMesh(mesh, true);
      }

      return default;
    }

    /// <summary>
    /// Scales <paramref name="mesh"/> by <paramref name="scaleFactor"/>,
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="scaleFactor"></param>
    /// <returns>false if <paramref name="mesh"/> is not valid or too small</returns>
    static bool EncodeRaw(ref Mesh mesh, double scaleFactor)
    {
      if (scaleFactor != 1.0 && !mesh.Scale(scaleFactor))
        return false;

      var bbox = mesh.GetBoundingBox(false);
      if (!bbox.IsValid || bbox.Diagonal.Length < ShortEdgeTolerance)
        return false;

      return true;
    }

    [Flags]
    enum MeshIssues
    {
      Nothing = 0,
      ShortEdges = 1,
    }

    static MeshIssues AuditEdge(Point3d from, Point3d to)
    {
      var issues = default(MeshIssues);

      if (from.DistanceTo(to) < ShortEdgeTolerance)
      {
        issues |= MeshIssues.ShortEdges;
        GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry contains short edges. Edges will be collapsed at the output.", new LineCurve(from, to));
      }

      return issues;
    }

    static MeshIssues AuditMesh(Mesh mesh)
    {
      var issues = default(MeshIssues);

      var vertices = mesh.Vertices.ToPoint3dArray();

      var faceCount = mesh.Faces.Count;
      for (int f = 0; f < faceCount; ++f)
      {
        var face = mesh.Faces[f];

        var A = vertices[face.A];
        var B = vertices[face.B];
        var C = vertices[face.C];

        issues |= AuditEdge(A, B);
        issues |= AuditEdge(B, C);

        if (face.IsQuad)
        {
          var D = vertices[face.D];

          issues |= AuditEdge(C, D);
          issues |= AuditEdge(D, A);
        }
        else issues |= AuditEdge(C, A);
      }

      var edges = mesh.TopologyEdges;
      for (int ei = 0; ei < edges.Count; ++ei)
      {
        if (edges.GetConnectedFaces(ei).Length > 2)
          GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry is nonmanifold.", new LineCurve(edges.EdgeLine(ei)));
      }

      var degenerated = mesh.Faces.CullDegenerateFaces();
      if(degenerated > 0)
        GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry contains degenerated faces. Those faces will be removed.", default);

      return issues;
    }

    static bool TryRebuildMesh(Mesh mesh, MeshIssues issues)
    {
      if (issues.HasFlag(MeshIssues.ShortEdges) && mesh.Ngons.Count == 0)
      {
        mesh.Vertices.Align(ShortEdgeTolerance, mesh.GetNakedEdgePointStatus().Select(x => !x));
        mesh.Vertices.Align(ShortEdgeTolerance, default);
        mesh.Weld(Revit.AngleTolerance);
      }

      return mesh.IsValid;
    }
    #endregion

    #region Transfer
    /// <summary>
    /// Replaces <see cref="Raw.RawEncoder.ToHost(Mesh)"/> to catch Revit Exceptions and handle Ngons
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    internal static DB.Mesh ToMesh(Mesh mesh, double factor = UnitConverter.NoScale)
    {
      if (mesh is null)
        return default;

      var shells = mesh.ExplodeAtUnweldedEdges();
      return ToMesh(shells.Length == 0 ? new Mesh[] { mesh } : shells, factor);
    }

    internal static DB.Mesh ToMesh(Mesh[] shells, double factor = UnitConverter.NoScale)
    {
      try
      {
        using
        (
          var builder = new DB.TessellatedShapeBuilder()
          {
            GraphicsStyleId = GeometryEncoder.Context.Peek.GraphicsStyleId,
            Target = DB.TessellatedShapeBuilderTarget.Mesh,
            Fallback = DB.TessellatedShapeBuilderFallback.Salvage
          }
        )
        { 
          foreach (var shell in shells)
          {
            var issues = AuditMesh(shell);

            var nGonCount = shell.Ngons.Count;
            if (nGonCount != 0)
            {
              shell.Ngons.Count = 0;
              if
              (
                nGonCount == 1 &&
                Plane.FitPlaneToPoints(shell.Vertices.ToPoint3dArray(), out var _, out var deviation) == PlaneFitResult.Success &&
                deviation <= Revit.VertexTolerance
              )
                shell.Ngons.AddPlanarNgons(Revit.VertexTolerance, 4, 2, true);
            }

            if (TryRebuildMesh(shell, issues))
              AddConnectedFaceSet(builder, shell, factor, true);
          }

          builder.Build();
          using (var result = builder.GetBuildResult())
          {
#if DEBUG
            if (result.HasInvalidData)
            {
              var faceSets = result.GetNumberOfFaceSets();
              for (int fs = 0; fs < faceSets; ++fs)
              {
                foreach (var issue in result.GetIssuesForFaceSet(fs))
                  GeometryEncoder.Context.Peek.RuntimeMessage(255, $"DEBUG - {issue.GetIssueDescription()}", default);
              }
            }
#endif
            if (result.Outcome != DB.TessellatedShapeBuilderOutcome.Nothing)
            {
              var geometries = result.GetGeometricalObjects();
              if (geometries.Count == 1)
              {
                return geometries[0] as DB.Mesh;
              }
            }
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException e)
      {
        Debug.Fail(e.Source, e.Message);
      }

      return default;
    }

    static void AddConnectedFaceSet(DB.TessellatedShapeBuilder builder, Mesh mesh, double factor, bool assumePlanarNgons = false)
    {
      var vertices = mesh.Vertices.ToPoint3dArray();
      if (vertices.Length < 3)
        return;

      if (factor != 1.0)
      {
        for (int v = 0; v < vertices.Length; ++v)
          vertices[v] *= factor;
      }

      var isSolid = mesh.SolidOrientation() != 0;
      builder.OpenConnectedFaceSet(isSolid);
      var faces = 0;

      var faceToNgon = new int[mesh.Faces.Count];

      for (int n = 0; n < mesh.Ngons.Count; ++n)
      {
        var ngon = mesh.Ngons[n];
        var ngonFaces = ngon.FaceIndexList();
        if (ngonFaces.Length < 1)
          continue;

        // Check if ngon is planar, if not transfer it face by face
        if (!assumePlanarNgons)
        {
          var verticesCount = 0;
          for (int f = 0; f < ngonFaces.Length; ++f)
            verticesCount += mesh.Faces[(int) ngonFaces[f]].IsQuad ? 4 : 3;

          var faceVertices = new Point3d[verticesCount];
          int index = 0;
          for (int f = 0; f < ngonFaces.Length; ++f)
          {
            var face = mesh.Faces[(int) ngonFaces[f]];
            faceVertices[index++] = vertices[face.A];
            faceVertices[index++] = vertices[face.B];
            faceVertices[index++] = vertices[face.C];
            if (face.IsQuad)
              faceVertices[index++] = vertices[face.D];
          }

          if (Plane.FitPlaneToPoints(faceVertices, out var _, out var deviation) == PlaneFitResult.Failure)
            continue;

          if (deviation > Revit.VertexTolerance)
            continue;
        }

        // Mark faces as used
        foreach (var fi in ngon.FaceIndexList())
          faceToNgon[fi] = n + 1;

        if (mesh.Ngons.NgonHasHoles(n))
        {
          var lines = new List<LineCurve>();

          var topology = mesh.TopologyEdges;
          bool manifold = true;

          // Extract boundaries
          foreach (var ngonFace in ngonFaces)
          {
            var edgeIndices = topology.GetEdgesForFace((int) ngonFace, out var edgeOrientation);
            for (int i = 0; i < edgeIndices.Length; ++i)
            {
              Line Oriented(Line value, bool oriented) => oriented ? value : new Line(value.To, value.From);

              var ei = edgeIndices[i];
              var connectedFaces = topology.GetConnectedFaces(ei);
              if (connectedFaces.Length == 1)
              {
                lines.Add(new LineCurve(Oriented(topology.EdgeLine(ei), edgeOrientation[i])));
              }
              else if (connectedFaces.Length == 2)
              {
                if (faceToNgon[connectedFaces[0]] != faceToNgon[connectedFaces[1]])
                  lines.Add(new LineCurve(Oriented(topology.EdgeLine(ei), edgeOrientation[i])));
              }
              else
              {
                manifold = false;
                break;
              }
            }

            if (!manifold) break;
          }

          if (manifold)
          {
            // Compute a normal for the ngon
            var normal = Vector3d.Zero;
            if (mesh.Normals.Count == vertices.Length)
            {
              for (int f = 0; f < ngonFaces.Length; ++f)
              {
                var face = mesh.Faces[f];
                normal += mesh.Normals[face.A];
                normal += mesh.Normals[face.B];
                normal += mesh.Normals[face.C];
                if (face.IsQuad)
                  normal += mesh.Normals[face.D];
              }

              normal.Unitize();
            }
            else if (mesh.FaceNormals.Count == mesh.Faces.Count)
            {
              for (int f = 0; f < ngonFaces.Length; ++f)
                normal += mesh.FaceNormals[f];

              normal.Unitize();
            }

            var loops = Curve.JoinCurves(lines, Revit.VertexTolerance, true);

            for (int i = 0; i < loops.Length; ++i)
            {
              loops[i] = loops[i].Simplify(CurveSimplifyOptions.All, ShortEdgeTolerance, Revit.AngleTolerance);
              loops[i].RemoveShortSegments(ShortEdgeTolerance);
            }

            var allLoopVertices = new List<IList<DB.XYZ>>(loops.Length);

            foreach (var loop in loops)
            {
              if (loop is PolylineCurve polyline && polyline.SpanCount > 2)
              {
                var loopVertices = new DB.XYZ[polyline.PointCount - 1];
                for (int p = 0; p < loopVertices.Length; ++p)
                  loopVertices[p] = Raw.RawEncoder.AsXYZ(polyline.Point(p));

                var orientation = normal.IsZero ? loop.ClosedCurveOrientation() : loop.ClosedCurveOrientation(normal);
                if (orientation == CurveOrientation.CounterClockwise)
                  allLoopVertices.Insert(0, loopVertices);
                else
                  allLoopVertices.Add(loopVertices);
              }
            }

            builder.AddFace(new DB.TessellatedFace(allLoopVertices, GeometryEncoder.Context.Peek.MaterialId));
            faces++;
          }
          else
          {
            // Clear marks to export face by face
            foreach (var fi in ngon.FaceIndexList())
              faceToNgon[fi] = 0;
          }
        }
        else
        {
          var pline = new PolylineCurve(Array.ConvertAll(ngon.BoundaryVertexIndexList(), vi => vertices[vi]));
          if (pline.Simplify(CurveSimplifyOptions.All, ShortEdgeTolerance, Revit.AngleTolerance) is PolylineCurve polyline)
          {
            polyline.RemoveShortSegments(ShortEdgeTolerance);
            if (polyline.SpanCount > 2)
            {
              var outerLoopVertices = polyline.ToPolyline().ConvertAll(vi => Raw.RawEncoder.AsXYZ(vi));
              builder.AddFace(new DB.TessellatedFace(outerLoopVertices, GeometryEncoder.Context.Peek.MaterialId));
              faces++;
            }
          }
        }
      }

      var triangle = new DB.XYZ[3];
      var quad = new DB.XYZ[4];
      var fitPoints = new Point3d[4];

      var faceCount = mesh.Faces.Count;
      for (int fi = 0; fi < faceCount; ++fi)
      {
        // Already transfered as NGon
        if (faceToNgon[fi] != 0)
          continue;

        var face = mesh.Faces[fi];
        if (!face.IsValid(vertices))
          continue;

        if (face.IsQuad)
        {
          fitPoints[0] = vertices[face.A];
          fitPoints[1] = vertices[face.B];
          fitPoints[2] = vertices[face.C];
          fitPoints[3] = vertices[face.D];

          if (Plane.FitPlaneToPoints(fitPoints, out var plane, out var deviation) == PlaneFitResult.Success)
          {
            var planar = deviation < Revit.VertexTolerance * 0.5;

            var A = planar ? plane.ClosestPoint(fitPoints[0]) : fitPoints[0];
            var B = planar ? plane.ClosestPoint(fitPoints[1]) : fitPoints[1];
            var C = planar ? plane.ClosestPoint(fitPoints[2]) : fitPoints[2];
            var D = planar ? plane.ClosestPoint(fitPoints[3]) : fitPoints[3];

            var diagonal = new Line(A, C);
            var P = new Plane(A, C - A, plane.Normal);

            var distanceB = planar ? P.DistanceTo(B) : diagonal.DistanceTo(B, false);
            var distanceD = planar ? P.DistanceTo(D) : diagonal.DistanceTo(D, false);

            bool validB = Math.Abs(distanceB) > +Revit.VertexTolerance;
            bool validD = Math.Abs(distanceD) > +Revit.VertexTolerance;

            quad[0] = Raw.RawEncoder.AsXYZ(A);
            quad[1] = Raw.RawEncoder.AsXYZ(B);
            quad[2] = Raw.RawEncoder.AsXYZ(C);
            quad[3] = Raw.RawEncoder.AsXYZ(D);

            // If is not planar transfer as two triangles
            if (planar && validB && validD && (distanceB < 0.0 != distanceD < 0.0))
            {
              builder.AddFace(new DB.TessellatedFace(quad, GeometryEncoder.Context.Peek.MaterialId));
              faces++;
            }
            else
            {
              if (validB)
              {
                triangle[0] = quad[0]; triangle[1] = quad[1]; triangle[2] = quad[2];
                builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
                faces++;
              }

              if (validD)
              {
                triangle[0] = quad[2]; triangle[1] = quad[3]; triangle[2] = quad[0];
                builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
                faces++;
              }
            }
          }
        }
        else
        {
          triangle[0] = Raw.RawEncoder.AsXYZ(vertices[face.A]);
          triangle[1] = Raw.RawEncoder.AsXYZ(vertices[face.B]);
          triangle[2] = Raw.RawEncoder.AsXYZ(vertices[face.C]);

          builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
          faces++;
        }
      }

      if (faces == 0) builder.CancelConnectedFaceSet();
      else
      {
        try { builder.CloseConnectedFaceSet(); }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
          builder.CancelConnectedFaceSet();
        }
      }
    }
    #endregion
  }
}
