using System;
using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using External.DB.Extensions;

  /// <summary>
  /// This class is used to convert geometry to be stored in a <see cref="ARDB.DirectShape"/>.
  /// </summary>
  static class ShapeEncoder
  {
    public static ARDB.GeometryObject[] ToShape(this GeometryBase geometry) => ToShape(geometry, GeometryEncoder.ModelScaleFactor);
    internal static ARDB.GeometryObject[] ToShape(this GeometryBase geometry, double factor)
    {
      if (AuditGeometry(geometry))
      {
        switch (geometry)
        {
          case Point point:
            return new ARDB.Point[] { point.ToPoint(factor) };

          case PointCloud pointCloud:
            return pointCloud.Select(x => x.ToPoint(factor)).ToArray();

          case Curve curve:
            if (curve.SpanCount > 1 && curve.TryGetPolyline(out var polyline))
              return new ARDB.PolyLine[] { polyline.ToPolyLine(factor) };

            return curve.TryGetPolyCurve(out var polyCurve, GeometryTolerance.Internal.AngleTolerance) ?
              polyCurve.ToCurveMany(factor).Select(ToShape).ToArray() :
              new ARDB.Curve[] { curve.ToCurve(factor).ToShape() };

          case Brep brep:
            if (ToShape(brep, factor) is ARDB.GeometryObject brepShape)
              return new ARDB.GeometryObject[] { brepShape };
            break;

          case Extrusion extrusion:
            if (ToShape(extrusion, factor) is ARDB.GeometryObject extrusionShape)
              return new ARDB.GeometryObject[] { extrusionShape };
            break;

          case SubD subD:
            if (ToShape(subD, factor) is ARDB.GeometryObject subDShape)
              return new ARDB.GeometryObject[] { subDShape };
            break;

          case Mesh mesh:
            if (MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(mesh, factor)) is ARDB.GeometryObject meshShape)
              return new ARDB.GeometryObject[] { meshShape };
            break;

          default:
            if (geometry.HasBrepForm)
            {
              var brepForm = Brep.TryConvertBrep(geometry);
              if (brepForm is object && ToShape(brepForm, factor) is ARDB.GeometryObject geometryShape)
                return new ARDB.GeometryObject[] { geometryShape };
            }
            break;
        }
      }

      return Array.Empty<ARDB.GeometryObject>();
    }

    static bool AuditGeometry(GeometryBase geometry)
    {
      var bbox = geometry.GetBoundingBox(false);
      if (!bbox.IsValid)
        return false;

      var tol = GeometryTolerance.Model;
      switch (geometry)
      {
        case Point _:
          return true;

        default:
          return !bbox.Diagonal.EpsilonEquals(Vector3d.Zero, 2.0 * tol.VertexTolerance);
      }
    }

    static ARDB.Curve ToShape(this ARDB.Curve curve)
    {
      if (!curve.IsBound)
      {
        curve.GetRawParameters(out var min, out var max);
        curve = curve.CreateBounded(min, max);
      }

      {
        var length = curve.Length;
        if (length < GeometryTolerance.Internal.ShortCurveTolerance)
        {
          GeometryEncoder.Context.Peek.RuntimeMessage
          (
            10,
            "Curve is too short for Revit's tolerance. " +
            "Curve length should be greater than short-curve tolerance.",
            default
          );
        }

        var distance = curve.GetEndPoint(ERDB.CurveEnd.Start).DistanceTo(curve.GetEndPoint(ERDB.CurveEnd.End));
        if (distance > 30_000)
        {
          GeometryEncoder.Context.Peek.RuntimeMessage
          (
            10,
            "Curve is too long for Revit's tolerance. " +
            "Length between curve end points should be less than 30,000 ft.",
            default
          );
        }
      }

      return curve;
    }

    static ARDB.GeometryObject ToShape(Brep brep, double factor)
    {
      if (BrepEncoder.ToSolid(brep, factor) is ARDB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the brep.");
      return BrepEncoder.ToMesh(brep, factor);
    }

    static ARDB.GeometryObject ToShape(Extrusion extrusion, double factor)
    {
      if (ExtrusionEncoder.ToSolid(extrusion, factor) is ARDB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the extrusion.");
      return ExtrusionEncoder.ToMesh(extrusion, factor);
    }

    static ARDB.GeometryObject ToShape(SubD subD, double factor)
    {
      if (SubDEncoder.ToSolid(subD, factor) is ARDB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the subD.");
      return SubDEncoder.ToMesh(subD, factor);
    }
  };
}
