using System.Diagnostics;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using External.DB.Extensions;

  /// <summary>
  /// This class is used to convert geometry to be stored in a <see cref="DB.DirectShape"/>.
  /// </summary>
  public static class ShapeEncoder
  {
    public static DB.GeometryObject[] ToShape(this GeometryBase geometry) => ToShape(geometry, UnitConverter.ToHostUnits);
    public static DB.GeometryObject[] ToShape(this GeometryBase geometry, double factor)
    {
      switch (geometry)
      {
        case Point point:
          return new DB.Point[] { point.ToPoint(factor) };

        case PointCloud pointCloud:
          return pointCloud.ToPoints(factor);

        case Curve curve:
          return curve.ToCurveMany(factor).SelectMany(x => x.ToBoundedCurves()).ToArray();

        case Brep brep:
          if (ToShape(brep, factor) is DB.GeometryObject brepShape)
            return new DB.GeometryObject[] { brepShape };
          break;

        case Extrusion extrusion:
          if (ToShape(extrusion, factor) is DB.GeometryObject extrusionShape)
            return new DB.GeometryObject[] { extrusionShape };
          break;

        case SubD subD:
          if (ToShape(subD, factor) is DB.GeometryObject subDShape)
            return new DB.GeometryObject[] { subDShape };
          break;

        case Mesh mesh:
          if (MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(mesh, factor)) is DB.GeometryObject meshShape)
            return new DB.GeometryObject[] { meshShape };
          break;

        default:
          if (geometry.HasBrepForm)
          {
            var brepForm = Brep.TryConvertBrep(geometry);
            if (brepForm is object && ToShape(brepForm, factor) is DB.GeometryObject geometryShape)
              return new DB.GeometryObject[] { geometryShape };
          }
          break;
      }

      return new DB.GeometryObject[0];
    }

    static DB.GeometryObject ToShape(Brep brep, double factor)
    {
      // Try using DB.BRepBuilder
      if (BrepEncoder.ToSolid(brep, factor) is DB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the brep.");
      return BrepEncoder.ToMesh(brep, factor);
    }

    static DB.GeometryObject ToShape(Extrusion extrusion, double factor)
    {
      // Try using DB.BRepBuilder
      if (ExtrusionEncoder.ToSolid(extrusion, factor) is DB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the extrusion.");
      return ExtrusionEncoder.ToMesh(extrusion, factor);
    }

    static DB.GeometryObject ToShape(SubD subD, double factor)
    {
      // Try using DB.BRepBuilder
      if (SubDEncoder.ToSolid(subD, factor) is DB.Solid solid)
        return solid;

      Debug.WriteLine("Try meshing the subD.");
      return SubDEncoder.ToMesh(subD, factor);
    }
  };
}
