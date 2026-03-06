using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using Numerical;

  static class MeshExtension
  {
    public static bool TryGetLocation(this Mesh mesh, out XYZ origin, out UnitXYZ basisX, out UnitXYZ basisY)
    {
      origin = mesh.ComputeCentroid();
      var cov = Transform.Identity;
      cov.SetCovariance(mesh.Vertices);

      basisX = cov.GetPrincipalComponent(0D);

      var basisZ = cov.TryGetInverse(out var inverse) ?
                 inverse.GetPrincipalComponent(0D) :
                 mesh.ComputeNetNormal().ToUnitXYZ();

      return UnitXYZ.Orthonormalize(basisZ, basisX, out basisZ, out basisX, out basisY);
    }

    /// <summary>
    /// Returns the Centroid of this mesh.
    /// </summary>
    /// <remarks>
    /// Calculates the centroid of the mesh using an approximation, with an accuracy
    /// suitable for architectural purposes. This will correspond only with the center
    /// of gravity if the mesh is closed.
    /// </remarks>
    /// <param name="mesh"></param>
    /// <param name="dimension"></param>
    /// <returns>The XYZ point of the Centroid of this mesh.</returns>
    public static XYZ ComputeCentroid(this Mesh mesh, int dimension)
    {
      if (0 > dimension || dimension > 3)
        throw new System.ArgumentOutOfRangeException(nameof(dimension));

      if (dimension == 0)
        return XYZExtension.ComputeMeanPoint(mesh.Vertices);

      Numerical.Sum weights = default;
      Numerical.Sum centroidX = default, centroidY = default, centroidZ = default;
      var factor = dimension + 1.0;

      var numTriangles = mesh.NumTriangles;
      for (int t = 0; t < numTriangles; ++t)
      {
        var triangle = mesh.get_Triangle(t);
        var v0 = triangle.get_Vertex(0);
        var v1 = triangle.get_Vertex(1);
        var v2 = triangle.get_Vertex(2);

        Numerical.Sum vX = default, vY = default, vZ = default;
        vX.Add(v0.X, v1.X, v2.X);
        vY.Add(v0.Y, v1.Y, v2.Y);
        vZ.Add(v0.Z, v1.Z, v2.Z);

        var w = 0.0;
        switch (dimension)
        {
          case 1:
            w = XYZExtension.Norm(v1 - v0, 0D) + XYZExtension.Norm(v2 - v1, 0D) + XYZExtension.Norm(v0 - v2, 0D);
            break;

          case 2:
            w = XYZExtension.Norm(XYZExtension.CrossProduct(v1 - v0, v2 - v0), 0D);
            break;

          case 3:
            w = XYZExtension.TripleProduct(v0, v1, v2);
            break;
        }

        weights += w;
        w /= factor;

        centroidX.Add(vX.Value * w);
        centroidY.Add(vY.Value * w);
        centroidZ.Add(vZ.Value * w);
      }

      var weightsSum = weights.Value;
      return new XYZ(centroidX.Value / weightsSum, centroidY.Value / weightsSum, centroidZ.Value / weightsSum);
    }

    /// <summary>
    /// Returns the Centroid of this mesh.
    /// </summary>
    /// <remarks>
    /// Calculates the centroid of the mesh using an approximation, with an accuracy
    /// suitable for architectural purposes. This will correspond only with the center
    /// of gravity if the mesh represents a homogeneous structure of a single material.
    /// </remarks>
    /// <param name="mesh"></param>
    /// <returns>The XYZ point of the Centroid of this mesh.</returns>
    public static XYZ ComputeCentroid(this Mesh mesh) => ComputeCentroid(mesh, 3);

    /// <summary>
    /// Returns net normal of this mesh.
    /// </summary>
    /// <remarks>
    /// The length of the resulting normal is a good approximation
    /// of the signed area of this mesh when projected to this normal.
    /// </remarks>
    /// <param name="mesh"></param>
    /// <returns>The the sum of all triangle normals divided by 2.</returns>
    public static XYZ ComputeNetNormal(this Mesh mesh)
    {
      if (mesh.NumTriangles < 1)
        return XYZExtension.Zero;

      Numerical.Sum normalX = default, normalY = default, normalZ = default;
      var numTriangles = mesh.NumTriangles;

      for (int t = 0; t < numTriangles; ++t)
      {
        var triangle = mesh.get_Triangle(t);
        var v0 = triangle.get_Vertex(0);
        var v1 = triangle.get_Vertex(1);
        var v2 = triangle.get_Vertex(2);

        var normal = XYZExtension.CrossProduct(v1 - v0, v2 - v0);
        normalX.Add(normal.X);
        normalY.Add(normal.Y);
        normalZ.Add(normal.Z);
      }

      return new XYZ(normalX.Value * 0.5, normalY.Value * 0.5, normalZ.Value * 0.5);
    }

    /// <summary>
    /// Returns the surface area of the mesh.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="centroid">Area centroid</param>
    /// <param name="normal">Area net normal</param>
    /// <returns>The sum of the areas of the constituent facets of the mesh.</returns>
    internal static double ComputeSurfaceArea(this Mesh mesh, out XYZ centroid, out XYZ normal)
    {
      centroid = default;
      normal = XYZExtension.Zero;
      if (mesh is null) return double.NaN;
      var numTriangles = mesh.NumTriangles;
      if (numTriangles == 0) return 0.0;

      const int dimension = 2;
      var factor = 1.0 / (dimension + 1.0);
      Sum area = default;
      Sum normalX = default, normalY = default, normalZ = default;
      Sum centroidX = default, centroidY = default, centroidZ = default;

      for (int t = 0; t < numTriangles; ++t)
      {
        var triangle = mesh.get_Triangle(t);
        var v0 = triangle.get_Vertex(0);
        var v1 = triangle.get_Vertex(1);
        var v2 = triangle.get_Vertex(2);

        Sum vX = default, vY = default, vZ = default;
        vX.Add(v0.X, v1.X, v2.X);
        vY.Add(v0.Y, v1.Y, v2.Y);
        vZ.Add(v0.Z, v1.Z, v2.Z);

        var cross = XYZExtension.CrossProduct(v1 - v0, v2 - v0);
        var A = Euclidean.Norm(cross.X, cross.Y, cross.Z);
        area.Add(A);

        var weight = A * factor;
        centroidX.Add(vX.Value * weight);
        centroidY.Add(vY.Value * weight);
        centroidZ.Add(vZ.Value * weight);

        normalX.Add(cross.X);
        normalY.Add(cross.Y);
        normalZ.Add(cross.Z);
      }

      var areaValue = area.Value;
      centroid = new XYZ(centroidX.Value / areaValue, centroidY.Value / areaValue, centroidZ.Value / areaValue);
      normal = new XYZ(normalX.Value * 0.5, normalY.Value * 0.5, normalZ.Value * 0.5);
      return areaValue * 0.5;
    }

#if !REVIT_2024
    /// <summary>
    /// Returns the surface area of the mesh.
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns>The sum of the areas of the constituent facets of the mesh.</returns>
    public static double ComputeSurfaceArea(this Mesh mesh)
    {
      Numerical.Sum area = default;

      var numTriangles = mesh.NumTriangles;
      for (int t = 0; t < numTriangles; ++t)
      {
        var triangle = mesh.get_Triangle(t);
        var v0 = triangle.get_Vertex(0);
        var v1 = triangle.get_Vertex(1);
        var v2 = triangle.get_Vertex(2);

        area.Add(XYZExtension.Norm(XYZExtension.CrossProduct(v1 - v0, v2 - v0), 0D));
      }

      return area.Value * 0.5;
    }
#endif

    #region Naked Edges
    public static bool TryGetNakedEdges(this Mesh mesh, out PolyLine[] edges)
    {
      if (mesh.NumTriangles > 0)
      {
        var polylines = JoinLines(GetNakedEdges(mesh));
        if (polylines.Count > 0)
        {
          edges = new PolyLine[polylines.Count];

          var vertices = mesh.Vertices;
          var index = 0;
          foreach (var pline in polylines)
            edges[index++] = PolyLine.Create(pline.Select(x => vertices[x]).ToArray());
        }
        else edges = Array.Empty<PolyLine>(); // A totally closed mesh with no naked edges.

        return true;
      }

      edges = Array.Empty<PolyLine>();
      return false;
    }

    static ISet<(int V0, int V1)> GetNakedEdges(this Mesh mesh)
    {
      static (int V0, int V1) Edge(int v0, int v1) => v0 < v1 ? (v0, v1) : (v1, v0);
      var edges = new SortedSet<(int V0, int V1)>();

      var numTriangles = mesh.NumTriangles;
      for (int t = 0; t < numTriangles; ++t)
      {
        var triangle = mesh.get_Triangle(t);
        var v0 = (int) triangle.get_Index(0);
        var v1 = (int) triangle.get_Index(1);
        var v2 = (int) triangle.get_Index(2);

        var edge0 = Edge(v0, v1);
        if (!edges.Remove(edge0)) edges.Add(edge0);
        var edge1 = Edge(v1, v2);
        if (!edges.Remove(edge1)) edges.Add(edge1);
        var edge2 = Edge(v2, v0);
        if (!edges.Remove(edge2)) edges.Add(edge2);
      }

      return edges;
    }

    static List<List<int>> JoinLines(ISet<(int A, int B)> segments)
    {
      var adjacency = new Dictionary<int, List<(int A, int B)>>();
      {
        foreach (var segment in segments)
        {
          if (!adjacency.ContainsKey(segment.A)) adjacency[segment.A] = new List<(int A, int B)>();
          if (!adjacency.ContainsKey(segment.B)) adjacency[segment.B] = new List<(int A, int B)>();

          adjacency[segment.A].Add(segment);
          adjacency[segment.B].Add(segment);
        }
      }

      var polylines = new List<List<int>>();

      // Open polylines
      {
        foreach (int start in adjacency.Where(x => x.Value.Count != 2).Select(x => x.Key))
        {
          if (adjacency[start].All(segments.Contains))
            continue;

          var poly = new List<int>();

          int current = start;
          while (true)
          {
            poly.Add(current);

            var next = adjacency[current].FirstOrDefault(segments.Contains);
            if (next.Equals(default) && !segments.Contains(next))
              break;

            segments.Remove(next);

            current = (next.A == current) ? next.B : next.A;
            if (adjacency[current].Count != 2)
            {
              poly.Add(current);
              break;
            }
          }

          polylines.Add(poly);
        }
      }

      // Closed polylines
      {
        while (segments.Count > 0)
        {
          var first = segments.First();
          segments.Remove(first);
          int start = first.A;
          int current = first.B;
          var loop = new List<int> { start, current };
          while (current != start)
          {
            var next = adjacency[current].First(segments.Contains);
            segments.Remove(next);
            current = (next.A == current) ? next.B : next.A;
            loop.Add(current);
          }
          polylines.Add(loop);
        }
      }

      return polylines;
    }
    #endregion
  }
}
