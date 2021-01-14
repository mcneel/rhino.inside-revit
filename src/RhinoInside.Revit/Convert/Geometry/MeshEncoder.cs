using System.Diagnostics;
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
        while (mesh.CollapseFacesByEdgeLength(false, Revit.ShortCurveTolerance) > 0) ;

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
        return default;

      var bbox = mesh.GetBoundingBox(false);
      if (!bbox.IsValid)
        return default;

      return true;
    }
    #endregion

    #region Transfer
    /// <summary>
    /// Replaces <see cref="Raw.RawEncoder.ToHost(Mesh)"/> to catch Revit Exceptions and handle Ngons
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    internal static DB.Mesh ToMesh(/*const*/ Mesh mesh, double factor = UnitConverter.NoScale)
    {
      return ToMesh(mesh.ExplodeAtUnweldedEdges(), factor);
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
            shell.Ngons.Count = 0;
            shell.Ngons.AddPlanarNgons(Revit.VertexTolerance * factor, 4, 2, true);
            AddConnectedFaceSet(builder, shell, factor, true);
          }

          builder.Build();
          using (var result = builder.GetBuildResult())
          {
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
      var isSolid = mesh.SolidOrientation() != 0;
      builder.OpenConnectedFaceSet(isSolid);

      var vertices = mesh.Vertices.ToPoint3dArray();
      if (factor != 1.0)
      {
        for (int v = 0; v < vertices.Length; ++v)
          vertices[v] *= factor;
      }

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
            var allLoopVertices = new List<IList<DB.XYZ>>(loops.Length);

            foreach (var loop in loops)
            {
              var polyline = loop as PolylineCurve;
              var loopVertices = new DB.XYZ[polyline.PointCount - 1];
              for (int p = 0; p < loopVertices.Length; ++p)
                loopVertices[p] = Raw.RawEncoder.AsXYZ(polyline.Point(p));

              var orientation = normal.IsZero ? loop.ClosedCurveOrientation() : loop.ClosedCurveOrientation(normal);
              if (orientation == CurveOrientation.CounterClockwise)
                allLoopVertices.Insert(0, loopVertices);
              else
                allLoopVertices.Add(loopVertices);
            }

            builder.AddFace(new DB.TessellatedFace(allLoopVertices, GeometryEncoder.Context.Peek.MaterialId));
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
          var outerLoopVertices = Array.ConvertAll(ngon.BoundaryVertexIndexList(), vi => Raw.RawEncoder.AsXYZ(vertices[vi]));
          builder.AddFace(new DB.TessellatedFace(outerLoopVertices, GeometryEncoder.Context.Peek.MaterialId));
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
        if (face.IsQuad)
        {
          fitPoints[0] = vertices[face.A];
          fitPoints[1] = vertices[face.B];
          fitPoints[2] = vertices[face.C];
          fitPoints[3] = vertices[face.D];

          quad[0] = Raw.RawEncoder.AsXYZ(fitPoints[0]);
          quad[1] = Raw.RawEncoder.AsXYZ(fitPoints[1]);
          quad[2] = Raw.RawEncoder.AsXYZ(fitPoints[2]);
          quad[3] = Raw.RawEncoder.AsXYZ(fitPoints[3]);

          // If is not planar transfer as two triangles
          var fit = Plane.FitPlaneToPoints(fitPoints, out var _, out var deviation);
          if (fit == PlaneFitResult.Failure || deviation > Revit.VertexTolerance)
          {
            // Split along the short diagonal
            if (quad[0].DistanceTo(quad[2]) <= quad[1].DistanceTo(quad[3]))
            {
              triangle[0] = quad[0]; triangle[1] = quad[1]; triangle[2] = quad[2];
              builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));

              triangle[0] = quad[2]; triangle[1] = quad[3]; triangle[2] = quad[0]; 
              builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
            }
            else
            {
              triangle[0] = quad[0]; triangle[1] = quad[1]; triangle[2] = quad[3];
              builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));

              triangle[0] = quad[1]; triangle[1] = quad[2]; triangle[2] = quad[3];
              builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
            }
          }
          else builder.AddFace(new DB.TessellatedFace(quad, GeometryEncoder.Context.Peek.MaterialId));
        }
        else
        {
          triangle[0] = Raw.RawEncoder.AsXYZ(vertices[face.A]);
          triangle[1] = Raw.RawEncoder.AsXYZ(vertices[face.B]);
          triangle[2] = Raw.RawEncoder.AsXYZ(vertices[face.C]);

          builder.AddFace(new DB.TessellatedFace(triangle, GeometryEncoder.Context.Peek.MaterialId));
        }
      }

      builder.CloseConnectedFaceSet();
    }
    #endregion
  }
}
