using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Display
{
  using Geometry;

  public static class PreviewConverter
  {
    static bool SkipGeometryObject(DB.GeometryObject geometryObject, DB.Document doc)
    {
      if (doc.GetElement(geometryObject.GraphicsStyleId) is DB.GraphicsStyle style)
        return style.GraphicsStyleCategory.Id.IntegerValue == (int) DB.BuiltInCategory.OST_LightingFixtureSource;

      return false;
    }

    #region GetPreviewMaterials
    internal static Dictionary<DB.Material, Rhino.Geometry.Mesh> ZipByMaterial
    (
      DB.Material[] materialElements,
      Rhino.Geometry.Mesh[] meshes,
      Rhino.Geometry.Mesh outMesh = default
    )
    {
      if (materialElements is null || meshes is null) return null;

      var dictionary = new Dictionary<DB.Material, Mesh>(External.DB.Extensions.ElementEqualityComparer.SameDocument);

      for (int index = 0; index < materialElements.Length && index < meshes.Length; ++index)
      {
        if (materialElements[index] is null)
        {
          if (!(outMesh is null))
            outMesh.Append(meshes[index]);
        }
        else
        {
          if (!dictionary.TryGetValue(materialElements[index], out var mesh0))
            dictionary.Add(materialElements[index], mesh0 = new Rhino.Geometry.Mesh());

          mesh0.Append(meshes[index]);
        }
      }

      return dictionary;
    }

    /// <summary>
    /// Extracts a sequence of <see cref="DB.Material"/> from a sequence of <see cref="DB.GeometryObject"/>.
    /// </summary>
    /// <remarks>
    /// Empty <see cref="DB.Mesh"/> and empty <see cref="DB.Solid"/> will be skipped,
    /// so output <see cref="IEnumerable{T}"/> may be shorter than the input.
    /// Output is warranted to be free of nulls.
    /// </remarks>
    /// <param name="geometries"></param>
    /// <param name="doc"></param>
    /// <param name="currentMaterial"></param>
    /// <returns>An <see cref="IEnumerable{DB.Material}"/></returns>
    /// <seealso cref="GetPreviewMeshes(IEnumerable{DB.GeometryObject}, MeshingParameters)"/>
    internal static IEnumerable<DB.Material> GetPreviewMaterials
    (
      this IEnumerable<DB.GeometryObject> geometries,
      DB.Document doc,
      DB.Material currentMaterial
    )
    {
      foreach (var geometry in geometries)
      {
        if (geometry.Visibility != DB.Visibility.Visible)
          continue;

        switch (geometry)
        {
          case DB.GeometryInstance instance:
            foreach (var g in instance.SymbolGeometry.GetPreviewMaterials(doc, instance.SymbolGeometry.MaterialElement ?? currentMaterial))
              yield return g;
            break;

          case DB.Mesh mesh:
            if (mesh.NumTriangles <= 0)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            yield return doc.GetElement(mesh.MaterialElementId) as DB.Material ?? currentMaterial;
            break;

          case DB.Face face:
            if (SkipGeometryObject(geometry, doc))
              continue;

            yield return doc.GetElement(face.MaterialElementId) as DB.Material ?? currentMaterial;
            break;

          case DB.Solid solid:
            if (solid.Faces.IsEmpty)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            var solidFaces = solid.Faces.OfType<DB.Face>();
            foreach (var face in solidFaces)
              yield return doc.GetElement(face.MaterialElementId) as DB.Material ?? currentMaterial;

            break;
        }
      }
    }
    #endregion

    #region GetPreviewMeshes
    static double LevelOfDetail(this MeshingParameters value) => value?.RelativeTolerance ?? 0.15;

    /// <summary>
    /// Extracts a sequence of <see cref="Mesh"/> from a sequence of <see cref="DB.GeometryObject"/>.
    /// </summary>
    /// <remarks>
    /// Empty <see cref="DB.Mesh"/> and empty <see cref="DB.Solid"/> will be skipped,
    /// so output <see cref="IEnumerable{T}"/> may be shorter than the input.
    /// Output is warranted to be free of nulls, an empty <see cref="Mesh"/> is returned in case of error.
    /// </remarks>
    /// <param name="geometries"></param>
    /// <param name="meshingParameters"></param>
    /// <returns>An <see cref="IEnumerable{Mesh}"/></returns>
    /// <seealso cref="GetPreviewMaterials(IEnumerable{DB.GeometryObject}, DB.Document, DB.Material)"/>
    internal static IEnumerable<Mesh> GetPreviewMeshes
    (
      this IEnumerable<DB.GeometryObject> geometries,
      DB.Document doc,
      MeshingParameters meshingParameters
    )
    {
      foreach (var geometry in geometries)
      {
        if (geometry.Visibility != DB.Visibility.Visible)
          continue;

        switch (geometry)
        {
          case DB.GeometryInstance instance:
          {
            var xform = instance.Transform.ToTransform();
            foreach (var g in instance.SymbolGeometry.GetPreviewMeshes(doc, meshingParameters))
            {
              g.Transform(xform);
              yield return g;
            }
            break;
          }
          case DB.Mesh mesh:
          {
            if (mesh.NumTriangles <= 0)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            var f = Geometry.Raw.RawDecoder.ToRhino(mesh);
            UnitConverter.Scale(f, UnitConverter.ToRhinoUnits);

            yield return f ?? new Rhino.Geometry.Mesh();
            break;
          }
          case DB.Face face:
          {
            if (SkipGeometryObject(geometry, doc))
              continue;

            var faceMesh = face.Triangulate(meshingParameters.LevelOfDetail());
            var f = Geometry.Raw.RawDecoder.ToRhino(faceMesh);
            UnitConverter.Scale(f, UnitConverter.ToRhinoUnits);

            yield return f ?? new Rhino.Geometry.Mesh();
            break;
          }
          case DB.Solid solid:
          {
            if (solid.Faces.IsEmpty)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            var solidFaces = solid.Faces.OfType<DB.Face>();
            foreach (var face in solidFaces)
            {
              var faceMesh = face.Triangulate(meshingParameters.LevelOfDetail());
              var f = Geometry.Raw.RawDecoder.ToRhino(faceMesh);
              UnitConverter.Scale(f, UnitConverter.ToRhinoUnits);

              yield return f ?? new Rhino.Geometry.Mesh();
            }
            break;
          }
        }
      }
    }
    #endregion

    #region GetPreviewWires
    internal static IEnumerable<Curve> GetPreviewWires
    (
      this IEnumerable<DB.GeometryObject> geometries
    )
    {
      foreach (var geometry in geometries)
      {
        if (geometry?.Visibility != DB.Visibility.Visible)
          continue;

        switch (geometry)
        {
          case DB.GeometryInstance instance:
          {
            var xform = instance.Transform.ToTransform();
            foreach (var g in instance.SymbolGeometry.GetPreviewWires())
            {
              g?.Transform(xform);
              yield return g;
            }
            break;
          }
          case DB.Solid solid:
          {
            if (solid.Faces.IsEmpty)
              continue;

            foreach (var wire in solid.Edges.Cast<DB.Edge>().Select(x => x.AsCurve()).GetPreviewWires())
              yield return wire;
            break;
          }
          case DB.Face face:
          {
            foreach (var wire in face.GetEdgesAsCurveLoops().SelectMany(x => x.GetPreviewWires()))
              yield return wire;
            break;
          }
          case DB.Edge edge:
          {
            yield return edge.AsCurve().ToCurve();
            break;
          }
          case DB.Curve curve:
          {
            yield return curve.ToCurve();
            break;
          }
          case DB.PolyLine polyline:
          {
            if (polyline.NumberOfCoordinates <= 0)
              continue;

            yield return polyline.ToPolylineCurve();
            break;
          }
        }
      }
    }
    #endregion
  };
}
