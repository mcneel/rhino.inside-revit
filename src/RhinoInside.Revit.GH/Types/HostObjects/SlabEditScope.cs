using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  internal sealed class SlabEditScope : IDisposable
  {
    readonly HostObject Host;
    readonly ARDB.SlabShapeEditor Editor;
    public SlabEditScope(HostObject host)
    {
      Host = host;
      Editor = Host.Value?.GetSlabShapeEditor();
    }
    public void Dispose() => (Editor as IDisposable).Dispose();

    public bool IsAvailable => Editor is object;
    public bool IsEnabled => Editor?.IsEnabled is true;
    public bool Enable()
    {
      if (!IsAvailable) return false;
      Editor.Enable();
      return true;
    }

    public void ResetSlabShape() => Editor?.ResetSlabShape();

    private static double GetBoundaryElevation(IList<Curve> boundary2d, IList<Curve> boundary3d, Point3d point)
    {
      var point2d = new Point3d(point.X, point.Y, 0.0);
      var closestPoint = NaN.Point3d;
      double minDistance = double.PositiveInfinity;
      for (int c = 0; c < boundary2d.Count; ++c)
      {
        if (!boundary2d[c].ClosestPoint(point, out var t, minDistance)) continue;
        var distance = boundary2d[c].PointAt(t).DistanceTo(point2d);
        if (distance < minDistance)
        {
          minDistance = distance;
          closestPoint = boundary3d[c].PointAt(t);
        }
      }

      return closestPoint.Z;
    }

    public bool SetSlabShape
    (
      IList<Curve> boundary2d,
      IList<Curve> boundary3d,
      IList<Point3d> points,
      IList<Line> creases,
      out IList<Point3d> skipedPoints,
      out IList<Line> skipedCreases,
      AbortToken token = default
    )
    {
      var hostLocation = Host.Location;
      var elevation = hostLocation.OriginZ;
      if (!double.IsNaN(elevation))
      {
        var compoundStructure = Host.Type.CompoundStructure;
        var minElevation = elevation - (compoundStructure.VariableLayerThickness ?? double.PositiveInfinity);

        elevation = GeometryEncoder.ToInternalLength(elevation);
        minElevation = GeometryEncoder.ToInternalLength(minElevation);

        skipedPoints = new List<Point3d>();
        skipedCreases = new List<Line>();

#if REVIT_2025
        var addInPlace = true;
#elif REVIT_2024
        // Move the points after crashes in Revit 2024.3.
        var addInPlace = Host.Value is ARDB.Toposolid;
#else
        // Before Revit 2024 is much faster to draw the points on the plane and move them after.
        var addInPlace = false;
#endif

        var vertices = new Dictionary<Point3d, ARDB.SlabShapeVertex>(Editor.SlabShapeVertices.Size + (points?.Count ?? 0) + (creases?.Count ?? 0));
        ARDB.SlabShapeVertex AddVertex(Point3d point)
        {
          var x = GeometryEncoder.ToInternalLength(point.X);
          var y = GeometryEncoder.ToInternalLength(point.Y);
          var z = Math.Max(GeometryEncoder.ToInternalLength(point.Z), minElevation);

          var xyz = new Point3d(x, y, z);
          if (!vertices.TryGetValue(xyz, out var vertex))
          {
            try
            {
              if (addInPlace)
              {
                if ((vertex = Editor.AddPoint(new ARDB.XYZ(x, y, z))) is object)
                  vertices.Add(xyz, vertex);
              }
              else
              {
                if ((vertex = Editor.AddPoint(new ARDB.XYZ(x, y, elevation))) is object)
                  vertices.Add(xyz, vertex);
              }
            }
            catch { }
          }

          return vertex?.VertexType == ARDB.SlabShapeVertexType.Invalid ? null : vertex;
        }

        if (boundary2d is object && boundary3d is object)
        {
          for (int b = 0; b < boundary2d.Count; ++b)
            boundary2d[b] = boundary2d[b].ProjectToPlane(hostLocation);

          foreach (var vertex in Editor.SlabShapeVertices.Cast<ARDB.SlabShapeVertex>().TakeWhileNotAborted(token))
          {
            if (vertex.VertexType == ARDB.SlabShapeVertexType.Invalid) continue;
            if (vertex.VertexType == ARDB.SlabShapeVertexType.Interior) continue;

            var xyz = vertex.Position.ToPoint3d();
            xyz.Z = GetBoundaryElevation(boundary2d, boundary3d, xyz);

            if (addInPlace)
              Editor.ModifySubElement(vertex, GeometryEncoder.ToInternalLength(xyz.Z) - elevation);
            else
              vertices.Add(xyz / Revit.ModelUnits, vertex);
          }
        }

        if (points is object)
        {
          foreach (var point in points.TakeWhileNotAborted(token))
          {
            if (AddVertex(point) is null)
              skipedPoints.Add(point);
          }
        }

        if (creases is object)
        {
          foreach (var crease in creases.TakeWhileNotAborted(token))
          {
            if (crease.IsValid)
            {
              try
              {
                var from = AddVertex(crease.From);
                var to = AddVertex(crease.To);
                if (from is object && to is object && Editor.AddSplitLine(from, to) is IList<ARDB.SlabShapeCrease> lines)
                {
                  if (!addInPlace && lines.Count > 1)
                  {
                    foreach (var line in lines)
                    {
                      foreach (ARDB.SlabShapeVertex vertex in line.EndPoints)
                        AddVertex(vertex.Position.ToPoint3d());
                    }
                  }
                }
                else skipedCreases.Add(crease);
              }
              catch { skipedCreases.Add(crease); }
            }
            else skipedCreases.Add(crease);
          }
        }

        if (!addInPlace)
        {
          foreach (var vertex in vertices.OrderBy(x => x.Key.Z))
            Editor.ModifySubElement(vertex.Value, vertex.Key.Z - elevation);
        }

        return !token.IsAbortedRequested;
      }

      skipedPoints = null;
      skipedCreases = null;
      return false;
    }

    internal static bool TryGetBoundaries(Mesh mesh, Brep shell, out IList<Curve> boundaries2d, out IList<Curve> boundaries3d, int orientation)
    {
      var tol = GeometryTolerance.Model;
      if (shell is object)
      {
        var mbbox = mesh.GetBoundingBox(true);
        var silhouettes = Silhouette.Compute(shell, SilhouetteType.Boundary, Vector3d.ZAxis, tol.VertexTolerance, tol.AngleTolerance);
        boundaries3d = silhouettes.
          Where
          (
            x =>
            {
              if (x.Curve is object && x.GeometryComponentIndex.ComponentIndexType == ComponentIndexType.BrepEdge)
              {
                var edge = shell.Edges[x.GeometryComponentIndex.Index];
                var point = x.Curve.PointAt(x.Curve.Domain.Mid);
                foreach (var fi in edge.AdjacentFaces())
                {
                  var face = shell.Faces[fi];
                  face.ClosestPoint(point, out var u, out var v);
                  if (face.NormalAt(u, v).Z > 0.0)
                    return IsPointOnSurface(mesh, point, mbbox, orientation);
                }
                return false;
              }

              return true;
            }
          ).
          Select(x => x.Curve).ToArray();
        boundaries3d = Curve.JoinCurves(boundaries3d);
        boundaries2d = default;

        var bbox = BoundingBox.Empty;
        foreach (var edge in boundaries3d)
          bbox.Union(edge.GetBoundingBox(accurate: true));

        var plane = new Plane(new Point3d(0.0, 0.0, bbox.Min.Z), orientation < 0 ? -Vector3d.ZAxis : Vector3d.ZAxis);
        boundaries2d = boundaries3d.Select(x => x.ProjectToPlane(plane)).ToArray();

        if (boundaries2d.All(x => x.IsClosed && Intersection.CurveSelf(x, tol.VertexTolerance).Count == 0))
          return true;
      }

      var count = mesh.TopologyEdges.Count;
      var edges = new List<Line>(count);
      if (count > 0)
      {
        // Find relevant edges
        for (int e = 0; e < count; ++e)
        {
          var faces = mesh.TopologyEdges.GetConnectedFaces(e);
          switch (faces.Length)
          {
            case 1:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              if (face0.Z <= 0.0) continue;
            }
            break;
            case 2:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              var face1 = mesh.FaceNormals[faces[1]];
              if (face0.Z <= 0.0 == face1.Z <= 0.0) continue;
            }
            break;
            default: continue;
          }

          edges.Add(mesh.TopologyEdges.EdgeLine(e));
        }

        var bbox = BoundingBox.Empty;
        foreach (var edge in edges)
          bbox.Union(edge.BoundingBox);

        // Compute outlines
        var plane = new Plane(new Point3d(0.0, 0.0, bbox.Min.Z), orientation < 0 ? -Vector3d.ZAxis : Vector3d.ZAxis);
        var polylines2d = mesh.GetOutlines(plane);
        {
          // Clean overlapped edges.
          var breps = Brep.CreatePlanarBreps(new Rhino.Collections.CurveList(polylines2d.Select(x => new PolylineCurve(x))), tol.VertexTolerance);
          if (breps?.Length > 0)
          {
            polylines2d = breps.SelectMany(x => x.Loops.Select(x => x.To3dCurve())).
              Select(x => x.ToPolyline(0.0, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity).ToPolyline()).
              ToArray();
          }
        }

        // Pull back outlines back to edges
        var polylines3d = new List<Polyline>();
        foreach (var polyline2d in polylines2d)
        {
          // Clean out outline
          polyline2d.MergeColinearSegments(tol.AngleTolerance, includeSeam: true);
          polyline2d.CollapseShortSegments(tol.VertexTolerance);

          polylines3d.Add
          (
            new Polyline
            (
              polyline2d.Select
              (
                pt => Intersection.MeshLine(mesh, new Line(new Point3d(pt.X, pt.Y, bbox.Min.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z))).
                //pt => edges.Select
                //(
                //  c => Intersection.LineLine(c, new Line(new Point3d(pt.X, pt.Y, bbox.Min.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z)), out var a, out var b, 0.0, true) & false ?
                //        c.PointAt(a) :
                //        pt
                //).
                OrderByDescending(x => orientation < 0 ? -x.Z : x.Z).
                FirstOrDefault(pt)
              )
            )
          );
        }

        boundaries3d = polylines3d.Select(x => new PolylineCurve(x)).ToArray();
        boundaries2d = polylines2d.Select(x => new PolylineCurve(x)).ToArray();
        return true;
      }

      boundaries2d = default;
      boundaries3d = default;
      return false;
    }

    internal static bool TryGetMeshPoints(Mesh mesh, out IList<Point3d> points, int orientation)
    {
      if (mesh.Faces.Count > 0)
      {
        var bbox = mesh.GetBoundingBox(accurate: true);

        var vertices = mesh.TopologyVertices;
        var count = vertices.Count;
        var vi = new HashSet<Point3d>(count);
        for (int v = 0; v < count; ++v)
        {
          var faces = vertices.ConnectedFaces(v);
          if (faces.Length < 2) continue;

          if (faces.Any(f => mesh.FaceNormals[f].Z > 0.1))
          {
            var vertex = vertices.MeshVertexIndices(v)[0];
            var pt = mesh.Vertices.Point3dAt(vertex);

            if (IsPointOnSurface(mesh, pt, bbox, orientation))
              vi.Add(pt);
          }
        }

        points = vi.ToArray();
        return true;
      }

      points = default;
      return false;
    }

    internal static bool TryGetMeshCreases(Mesh mesh, out IList<Line> creases, int orientation)
    {
      var count = mesh.TopologyEdges.Count;
      if (count > 0)
      {
        var bbox = mesh.GetBoundingBox(accurate: true);

        creases = new List<Line>(count);
        for (int e = 0; e < count; ++e)
        {
          var faces = mesh.TopologyEdges.GetConnectedFaces(e);
          switch (faces.Length)
          {
            case 1:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              if (face0.Z <= 0.0) continue;
            }
            break;
            case 2:
            {
              var face0 = mesh.FaceNormals[faces[0]];
              var face1 = mesh.FaceNormals[faces[1]];
              if (face0.Z <= 0.0 && face1.Z <= 0.0) continue;
              //var angle = Vector3d.VectorAngle(face0, face1);
              //if (angle < RhinoMath.ToRadians(1.0)) continue;
            }
            break;
            default: continue;
          }

          var edge = mesh.TopologyEdges.EdgeLine(e);
          if (IsPointOnSurface(mesh, edge.From, bbox, orientation) && IsPointOnSurface(mesh, edge.To, bbox, orientation))
            creases.Add(edge);
        }

        return true;
      }

      creases = default;
      return false;
    }

    static bool IsPointOnSurface(Mesh mesh, Point3d pt, BoundingBox bbox, int orientation)
    {
      var ray = orientation >= 0 ?
        new Line(new Point3d(pt.X, pt.Y, pt.Z), new Point3d(pt.X, pt.Y, bbox.Max.Z)) :
        new Line(new Point3d(pt.X, pt.Y, bbox.Min.Z), new Point3d(pt.X, pt.Y, pt.Z));

      var points = Intersection.MeshLine(mesh, ray).OrderByDescending(x => orientation < 0 ? -x.Z : x.Z);
      return points.FirstOrDefault(pt) == pt;
    }

    private static Mesh MergeFragments(params Mesh[] fragments)
    {
      switch (fragments.Length)
      {
        case 0: break;
        case 1: return fragments[0];
        default:
          var result = new Mesh();
          result.Append(fragments);
          return result;
      }

      return null;
    }

    private static Mesh ToMesh(IGH_GeometricGoo geometry, MeshingParameters meshing)
    {
      switch (geometry)
      {
        case GH_Mesh mesh: return mesh.Value;
        case GH_Surface surface: return MergeFragments(Mesh.CreateFromBrep(surface.Value, meshing));
        case GH_Brep brep: return MergeFragments(Mesh.CreateFromBrep(brep.Value, meshing));
        case GH_Box box: return box.Mesh();
#if RHINO_8
        case GH_Extrusion extrusion: return Mesh.CreateFromExtrusion(extrusion.Value, meshing);
#endif
        case GH_SubD subD: return Mesh.CreateFromSubD(subD.Value, (int) SubDDisplayParameters.Density.MediumDensity);
      }

      return null;
    }

    private static Brep ToBrep(IGH_GeometricGoo geometry)
    {
      switch (geometry)
      {
        case GH_Surface surface: return surface.Value;
        case GH_Brep brep: return brep.Value;
        case GH_Box box: return box.Brep();
#if RHINO_8
        case GH_Extrusion extrusion: return extrusion.Value.ToBrep(false);
        case GH_SubD subD: return subD.Value.ToBrep(new SubDToBrepOptions(true, SubDToBrepOptions.ExtraordinaryVertexProcessOption.LocalG1xx));
#else
        case GH_SubD subD: return subD.Value.ToBrep(new SubDToBrepOptions(true, SubDToBrepOptions.ExtraordinaryVertexProcessOption.LocalG1x));
#endif
      }

      return null;
    }

    internal static Mesh GetShapeMesh(IGH_GeometricGoo geometry, IGH_Goo parameters, out Brep shell)
    {
      if (parameters?.CastTo(out QuadRemeshParameters quads) is true)
      {
        shell = ToBrep(geometry);
        if (shell is object)
          return Mesh.QuadRemeshBrep(shell, quads);

        if (geometry.CastTo(out Mesh mesh))
          return mesh.QuadRemesh(quads);
      }
      else
      {
        MeshingParameters meshing = null;
        if (parameters?.CastTo(out meshing) is false)
        {
          if (parameters.CastTo(out GH_MeshingParameters mp))
            meshing = mp.Value;
        }

        if (ToMesh(geometry, meshing ?? new MeshingParameters(MeshingParameters.Default) { SimplePlanes = true }) is Mesh mesh)
        {
          shell = ToBrep(geometry);
          return mesh;
        }
      }

      shell = null;
      return null;
    }

  }
}
