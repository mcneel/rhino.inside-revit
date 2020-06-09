using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhino;
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
          return new DB.GeometryObject[] { point.ToPoint(factor) };

        case PointCloud pointCloud:
          return pointCloud.ToPoints(factor);

        case Curve curve:
          return curve.ToCurveMany(factor).SelectMany(x => x.ToBoundedCurves()).OfType<DB.GeometryObject>().ToArray();

        case Brep brep:
          return ToGeometryObjectMany(BrepEncoder.ToRawBrep(brep, factor)).OfType<DB.GeometryObject>().ToArray();

        case Extrusion extrusion:
          return ToGeometryObjectMany(ExtrusionEncoder.ToRawBrep(extrusion, factor)).OfType<DB.GeometryObject>().ToArray();

        case SubD subD:
          return ToGeometryObjectMany(SubDEncoder.ToRawBrep(subD, factor)).OfType<DB.GeometryObject>().ToArray(); ;

        case Mesh mesh:
          return new DB.GeometryObject[] { MeshEncoder.ToMesh(MeshEncoder.ToRawMesh(mesh, factor)) };

        default:
          if (geometry.HasBrepForm)
          {
            var brepForm = Brep.TryConvertBrep(geometry);
            if (BrepEncoder.EncodeRaw(ref brepForm, factor))
              return ToGeometryObjectMany(brepForm).OfType<DB.GeometryObject>().ToArray();
          }

          return new DB.GeometryObject[0];
      }
    }

    internal static IEnumerable<DB.GeometryObject> ToGeometryObjectMany(Brep brep)
    {
      var solid = BrepEncoder.ToSolid(brep);
      if (solid is object)
      {
        yield return solid;
        yield break;
      }

      if (brep.Faces.Count > 1)
      {
        Debug.WriteLine("Try exploding the brep and converting face by face.");

        var breps = brep.UnjoinEdges(brep.Edges.Select(x => x.EdgeIndex));
        foreach (var face in breps.SelectMany(x => ToGeometryObjectMany(x)))
          yield return face;
      }
      else
      {
        Debug.WriteLine("Try meshing the brep.");

        // Emergency result as a mesh
        var mp = MeshingParameters.Default;
        mp.MinimumEdgeLength = Revit.ShortCurveTolerance;
        mp.ClosedObjectPostProcess = true;
        mp.JaggedSeams = false;

        var brepMesh = new Mesh();
        if (Mesh.CreateFromBrep(brep, mp) is Mesh[] meshes)
          brepMesh.Append(meshes);

        yield return brepMesh.ToMesh(UnitConverter.NoScale);
      }
    }
  };
}
