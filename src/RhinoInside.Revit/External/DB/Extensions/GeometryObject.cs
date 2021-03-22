using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class GeometryObjectExtension
  {
    public static IEnumerable<GeometryObject> ToDirectShapeGeometry(this GeometryObject geometry)
    {
      switch (geometry)
      {
        case Point p: yield return p; yield break;
        case Curve c:
          foreach (var unbounded in c.ToBoundedCurves())
            yield return unbounded;
          yield break;
        case Solid s: yield return s; yield break;
        case Mesh m: yield return m; yield break;
        case GeometryInstance i: yield return i; yield break;
        default: throw new ArgumentException("DirectShape only supports Point, Curve, Solid, Mesh and GeometryInstance.");
      }
    }

    /// <summary>
    /// Computes an arbitrary object oriented coord system for <paramref name="geometry"/>.
    /// </summary>
    /// <param name="geometry"></param>
    /// <param name="origin"></param>
    /// <param name="basisX"></param>
    /// <param name="basisY"></param>
    /// <returns></returns>
    public static bool TryGetLocation(this GeometryObject geometry, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      switch (geometry)
      {
        case GeometryElement element:
          foreach (var geo in element)
            if (TryGetLocation(geo, out origin, out basisX, out basisY)) return true;
          break;

        case GeometryInstance instance:
          origin = instance.Transform.Origin;
          basisX = instance.Transform.BasisX;
          basisY = instance.Transform.BasisY;
          return true;

        case Point point:
          origin = point.Coord;
          basisX = XYZ.BasisX;
          basisY = XYZ.BasisY;
          return true;

        case PolyLine polyline:
          return polyline.TryGetLocation(out origin, out basisX, out basisY);

        case Curve curve:
          return curve.TryGetLocation(out origin, out basisX, out basisY);

        case Edge edge:
          return edge.AsCurve().TryGetLocation(out origin, out basisX, out basisY);

        case Face face:
          using (var derivatives = face.ComputeDerivatives(new UV(0.5, 0.5), true))
          {
            origin = derivatives.Origin;
            basisX = derivatives.BasisX;
            basisY = derivatives.BasisY;
          }
          return true;

        case Solid solid:
          if(!solid.Faces.IsEmpty)
            return TryGetLocation(solid.Faces.get_Item(0), out origin, out basisX, out basisY);
          break;

        case Mesh mesh:
          return mesh.TryGetLocation(out origin, out basisX, out basisY);
      }

      origin = basisX = basisY = default;
      return false;
    }

    #region Mesh
    public static bool TryGetLocation(this Mesh mesh, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      origin = mesh.ComputeCentroid();
      var cov = XYZExtension.ComputeCovariance(mesh.Vertices);

      basisX = cov.GetPrincipalComponent(0D);

      var basisZ = cov.TryGetInverse(out var inverse) ?
                 inverse.GetPrincipalComponent(0D) :
                 mesh.ComputeMeanNormal();

      basisY = basisZ.CrossProduct(basisX).Normalize(0D);

      return true;
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
    public static XYZ ComputeCentroid(this Mesh mesh)
    {
      Sum weights = default;
      Sum centroidX = default, centroidY = default, centroidZ = default;
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

        var w = (v1 - v0).CrossProduct(v2 - v0, 0D).GetLength(0D);
        weights.Add(w);
        w /= 3.0;

        centroidX.Add(vX.Value * w);
        centroidY.Add(vY.Value * w);
        centroidZ.Add(vZ.Value * w);
      }

      var weightsSum = weights.Value;
      return new XYZ(centroidX.Value / weightsSum, centroidY.Value / weightsSum, centroidZ.Value / weightsSum);
    }

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
        return XYZ.Zero;

      Sum normalX = default, normalY = default, normalZ = default;
      var numTriangles = mesh.NumTriangles;

      for (int t = 0; t < numTriangles; ++t)
      {
        var triangle = mesh.get_Triangle(t);
        var v0 = triangle.get_Vertex(0);
        var v1 = triangle.get_Vertex(1);
        var v2 = triangle.get_Vertex(2);

        var normal = (v1 - v0).CrossProduct(v2 - v0, 0D);
        normalX.Add(normal.X);
        normalY.Add(normal.Y);
        normalZ.Add(normal.Z);
      }

      return new XYZ(normalX.Value / numTriangles, normalY.Value / numTriangles, normalZ.Value / numTriangles);
    }

    #endregion
  }
}
