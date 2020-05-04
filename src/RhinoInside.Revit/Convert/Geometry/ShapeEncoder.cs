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
          return ToGeometryObjectMany(MeshEncoder.ToRawMesh(mesh, factor)).OfType<DB.GeometryObject>().ToArray(); ;

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
        mp.MinimumEdgeLength = Revit.VertexTolerance;
        mp.ClosedObjectPostProcess = true;
        mp.JaggedSeams = false;

        var brepMesh = new Mesh();
        if (Mesh.CreateFromBrep(brep, mp) is Mesh[] meshes)
          brepMesh.Append(meshes);

        foreach (var g in ToGeometryObjectMany(brepMesh))
          yield return g;
      }
    }

    internal static IEnumerable<DB.GeometryObject> ToGeometryObjectMany(Mesh mesh)
    {
      DB.XYZ ToHost(Point3d p) => new DB.XYZ(p.X, p.Y, p.Z);

      var faceVertices = new List<DB.XYZ>(4);

      try
      {
        using
        (
          var builder = new DB.TessellatedShapeBuilder()
          {
            GraphicsStyleId = GeometryEncoder.Context.Peek.GraphicsStyleId,
            Target = DB.TessellatedShapeBuilderTarget.Mesh,
            Fallback = DB.TessellatedShapeBuilderFallback.Salvage
          }
        )
        {
          var pieces = mesh.DisjointMeshCount > 1 ?
                       mesh.SplitDisjointPieces() :
                       new Mesh[] { mesh };

          foreach (var piece in pieces)
          {
            piece.Faces.ConvertNonPlanarQuadsToTriangles(Revit.VertexTolerance, RhinoMath.UnsetValue, 5);

            var vertices = piece.Vertices.ToPoint3dArray();

            builder.OpenConnectedFaceSet(piece.SolidOrientation() != 0);
            foreach (var face in piece.Faces)
            {
              faceVertices.Add(ToHost(vertices[face.A]));
              faceVertices.Add(ToHost(vertices[face.B]));
              faceVertices.Add(ToHost(vertices[face.C]));
              if (face.IsQuad)
                faceVertices.Add(ToHost(vertices[face.D]));

              builder.AddFace(new DB.TessellatedFace(faceVertices, GeometryEncoder.Context.Peek.MaterialId));
              faceVertices.Clear();
            }
            builder.CloseConnectedFaceSet();
          }

          builder.Build();
          using (var result = builder.GetBuildResult())
          {
            if (result.Outcome != DB.TessellatedShapeBuilderOutcome.Nothing)
              return result.GetGeometricalObjects();
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException e)
      {
        Debug.Fail(e.Source, e.Message);
      }

      return Enumerable.Empty<DB.GeometryObject>();
    }
  };
}
