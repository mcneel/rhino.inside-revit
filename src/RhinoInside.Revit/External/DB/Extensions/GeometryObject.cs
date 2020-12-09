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
    /// Computes an arbitrary object oriented plane of <paramref name="geometry"/>.
    /// </summary>
    /// <param name="geometry"></param>
    /// <param name="origin"></param>
    /// <param name="basisX"></param>
    /// <param name="basisY"></param>
    /// <returns></returns>
    public static bool TryGetLocation(this GeometryObject geometry, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      origin = XYZ.Zero;
      basisX = XYZ.BasisX;
      basisY = XYZ.BasisY;

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
          return true;
        case PolyLine polyline:
          switch(polyline.NumberOfCoordinates)
          {
            case 0: return false;
            case 1: origin = polyline.GetCoordinate(0); return true;
            default:
              var start = polyline.GetCoordinate(0);
              var end = polyline.GetCoordinate(polyline.NumberOfCoordinates - 1);
              if (start.IsAlmostEqualTo(end))
              {
                var coordinates = polyline.GetCoordinates();
                for (int c = 0; c < coordinates.Count; ++c)
                  origin += coordinates[c];

                origin /= coordinates.Count;
                var axis = start - origin;
                basisX = axis.Normalize();
                basisY = basisX.PerpVector();
              }
              else
              {
                var axis = end - start;
                origin = start + (axis * 0.5);
                basisX = axis.Normalize();
                basisY = basisX.PerpVector();
              }
              return true;
          }
        case CylindricalHelix helix:
          origin = helix.BasePoint;
          basisX = helix.XVector;
          basisY = helix.YVector;
          return true;
        case Curve curve:
          if (curve.IsBound)
          {
            var start = curve.Evaluate(0.0, normalized: true);
            var end = curve.Evaluate(1.0, normalized: true);
            var axis = end - start;
            origin = start + (axis * 0.5);
            basisX = axis.Normalize();
            basisY = axis.PerpVector();
          }
          else if (curve is Arc arc)
          {
            origin = arc.Center;
            basisX = arc.XDirection;
            basisY = arc.YDirection;
          }
          else if (curve is Ellipse ellipse)
          {
            origin = ellipse.Center;
            basisX = ellipse.XDirection;
            basisY = ellipse.YDirection;
          }
          return true;
        case Edge edge:
          return TryGetLocation(edge.AsCurve(), out origin, out basisX, out basisY);
        case Face face:
          using (var bboxUV = face.GetBoundingBox())
          {
            var centerUV = new UV
            (
              bboxUV.Min.U + (bboxUV.Max.U - bboxUV.Min.U) * 0.5,
              bboxUV.Min.V + (bboxUV.Max.V - bboxUV.Min.V) * 0.5
            );
            using (var derivatives = face.ComputeDerivatives(centerUV))
            {
              origin = derivatives.Origin;
              basisX = derivatives.BasisX;
              basisY = derivatives.BasisY;
            }
          }

          return true;
        case Solid solid:
          return !solid.Faces.IsEmpty && TryGetLocation(solid.Faces.get_Item(0), out origin, out basisX, out basisY);

        case Mesh mesh:
          if (mesh.NumTriangles > 0)
          {
            var vertices = mesh.Vertices;
            for (int c = 0; c < vertices.Count; ++c)
              origin += vertices[c];

            origin /= vertices.Count;

            var triangle = mesh.get_Triangle(0);
            var A = triangle.get_Vertex(0);
            var B = triangle.get_Vertex(1);
            var C = triangle.get_Vertex(2);

            basisX = (B - A).Normalize();
            basisY = (C - A).Normalize();
            var normal = basisX.CrossProduct(basisY);
            basisY = normal.CrossProduct(basisX);
          }
          break;
      }

      return false;
    }
  }
}
