using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
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
                 mesh.ComputeMeanNormal().ToUnitXYZ();

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

      Sum weights = default;
      Sum centroidX = default, centroidY = default, centroidZ = default;
      var factor = dimension + 1.0;

      var numTriangles = mesh.NumTriangles;
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
    /// Return the mean of all triangle normals
    /// </summary>
    /// <remarks>
    /// In case the mesh is almost planar this will correspond to
    /// a good approximation of the normal.
    /// </remarks>
    /// <param name="mesh"></param>
    /// <returns>The XYZ vector of the mean normal of this mesh.</returns>
    public static XYZ ComputeMeanNormal(this Mesh mesh)
    {
      if (mesh.NumTriangles < 1)
        return XYZExtension.Zero;

      Sum normalX = default, normalY = default, normalZ = default;
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

      return new XYZ(normalX.Value / numTriangles, normalY.Value / numTriangles, normalZ.Value / numTriangles);
    }
  }
}
