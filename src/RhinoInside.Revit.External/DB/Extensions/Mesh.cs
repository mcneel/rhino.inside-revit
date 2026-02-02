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
  }
}
