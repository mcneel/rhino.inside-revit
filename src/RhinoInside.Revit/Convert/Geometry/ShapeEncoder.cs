using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

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
      switch (geometry)
      {
        case Point point:
          return new ARDB.Point[] { point.ToPoint(factor) };

        case PointCloud pointCloud:
          return pointCloud.Select(x => x.ToPoint(factor)).ToArray();

        case Curve curve:
          return curve.ToCurveMany(factor).SelectMany(x => x.ToBoundedCurves()).ToArray();

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

      return new ARDB.GeometryObject[0];
    }

    static ARDB.GeometryObject ToShape(Brep brep, double factor)
    {
      // Try using DB.BRepBuilder
      if (BrepEncoder.ToSolid(brep, factor) is ARDB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the brep.");
      return BrepEncoder.ToMesh(brep, factor);
    }

    static ARDB.GeometryObject ToShape(Extrusion extrusion, double factor)
    {
      // Try using DB.BRepBuilder
      if (ExtrusionEncoder.ToSolid(extrusion, factor) is ARDB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the extrusion.");
      return ExtrusionEncoder.ToMesh(extrusion, factor);
    }

    static ARDB.GeometryObject ToShape(SubD subD, double factor)
    {
      // Try using DB.BRepBuilder
      if (SubDEncoder.ToSolid(subD, factor) is ARDB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the subD.");
      return SubDEncoder.ToMesh(subD, factor);
    }
  };
}
