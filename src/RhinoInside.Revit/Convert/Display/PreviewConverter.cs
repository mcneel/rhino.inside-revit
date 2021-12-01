using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Display
{
  using Geometry;

  static class PreviewConverter
  {
    static bool SkipGeometryObject(ARDB.GeometryObject geometryObject, ARDB.Document doc)
    {
      if (doc.GetElement(geometryObject.GraphicsStyleId) is ARDB.GraphicsStyle style)
        return style.GraphicsStyleCategory.Id.IntegerValue == (int) ARDB.BuiltInCategory.OST_LightingFixtureSource;

      return false;
    }

    #region GetPreviewMaterials
    internal static Dictionary<ARDB.Material, Mesh> ZipByMaterial
    (
      ARDB.Material[] materialElements,
      Mesh[] meshes,
      Mesh outMesh = default
    )
    {
      if (materialElements is null || meshes is null) return null;

      var dictionary = new Dictionary<ARDB.Material, Mesh>(External.DB.Extensions.ElementEqualityComparer.SameDocument);

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
            dictionary.Add(materialElements[index], mesh0 = new Mesh());

          mesh0.Append(meshes[index]);
        }
      }

      return dictionary;
    }

    /// <summary>
    /// Extracts a sequence of <see cref="ARDB.Material"/> from a sequence of <see cref="ARDB.GeometryObject"/>.
    /// </summary>
    /// <remarks>
    /// Empty <see cref="ARDB.Mesh"/> and empty <see cref="ARDB.Solid"/> will be skipped,
    /// so output <see cref="IEnumerable{T}"/> may be shorter than the input.
    /// Output is warranted to be free of nulls.
    /// </remarks>
    /// <param name="geometries"></param>
    /// <param name="doc"></param>
    /// <param name="currentMaterial"></param>
    /// <returns>An <see cref="IEnumerable{ARDB.Material}"/></returns>
    /// <seealso cref="GetPreviewMeshes(IEnumerable{ARDB.GeometryObject}, MeshingParameters)"/>
    internal static IEnumerable<ARDB.Material> GetPreviewMaterials
    (
      this IEnumerable<ARDB.GeometryObject> geometries,
      ARDB.Document doc,
      ARDB.Material currentMaterial
    )
    {
      foreach (var geometry in geometries)
      {
        if (geometry.Visibility != ARDB.Visibility.Visible)
          continue;

        switch (geometry)
        {
          case ARDB.GeometryInstance instance:
            foreach (var g in instance.SymbolGeometry.GetPreviewMaterials(doc, instance.SymbolGeometry.MaterialElement ?? currentMaterial))
              yield return g;
            break;

          case ARDB.Mesh mesh:
            if (mesh.NumTriangles <= 0)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            yield return doc.GetElement(mesh.MaterialElementId) as ARDB.Material ?? currentMaterial;
            break;

          case ARDB.Face face:
            if (SkipGeometryObject(geometry, doc))
              continue;

            yield return doc.GetElement(face.MaterialElementId) as ARDB.Material ?? currentMaterial;
            break;

          case ARDB.Solid solid:
            if (solid.Faces.IsEmpty)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            var solidFaces = solid.Faces.OfType<ARDB.Face>();
            foreach (var face in solidFaces)
              yield return doc.GetElement(face.MaterialElementId) as ARDB.Material ?? currentMaterial;

            break;
        }
      }
    }
    #endregion

    #region GetPreviewMeshes
    static double LevelOfDetail(this MeshingParameters value) => value?.RelativeTolerance ?? 0.15;

    /// <summary>
    /// Extracts a sequence of <see cref="Mesh"/> from a sequence of <see cref="ARDB.GeometryObject"/>.
    /// </summary>
    /// <remarks>
    /// Empty <see cref="ARDB.Mesh"/> and empty <see cref="ARDB.Solid"/> will be skipped,
    /// so output <see cref="IEnumerable{T}"/> may be shorter than the input.
    /// Output is warranted to be free of nulls, an empty <see cref="Mesh"/> is returned in case of error.
    /// </remarks>
    /// <param name="geometries"></param>
    /// <param name="meshingParameters"></param>
    /// <returns>An <see cref="IEnumerable{Mesh}"/></returns>
    /// <seealso cref="GetPreviewMaterials(IEnumerable{ARDB.GeometryObject}, ARDB.Document, ARDB.Material)"/>
    internal static IEnumerable<Mesh> GetPreviewMeshes
    (
      this IEnumerable<ARDB.GeometryObject> geometries,
      ARDB.Document doc,
      MeshingParameters meshingParameters
    )
    {
      foreach (var geometry in geometries)
      {
        if (geometry.Visibility != ARDB.Visibility.Visible)
          continue;

        switch (geometry)
        {
          case ARDB.GeometryInstance instance:
          {
            var xform = instance.Transform.ToTransform();
            foreach (var g in instance.SymbolGeometry.GetPreviewMeshes(doc, meshingParameters))
            {
              g.Transform(xform);
              yield return g;
            }
            break;
          }
          case ARDB.Mesh mesh:
          {
            if (mesh.NumTriangles <= 0)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            var f = Geometry.Raw.RawDecoder.ToRhino(mesh);
            f.Scale(UnitConverter.ToRhinoUnits);

            yield return f ?? new Rhino.Geometry.Mesh();
            break;
          }
          case ARDB.Face face:
          {
            if (SkipGeometryObject(geometry, doc))
              continue;

            var faceMesh = face.Triangulate(meshingParameters.LevelOfDetail());
            var f = Geometry.Raw.RawDecoder.ToRhino(faceMesh);
            f.Scale(UnitConverter.ToRhinoUnits);

            yield return f ?? new Rhino.Geometry.Mesh();
            break;
          }
          case ARDB.Solid solid:
          {
            if (solid.Faces.IsEmpty)
              continue;

            if (SkipGeometryObject(geometry, doc))
              continue;

            var solidFaces = solid.Faces.OfType<ARDB.Face>();
            foreach (var face in solidFaces)
            {
              var faceMesh = face.Triangulate(meshingParameters.LevelOfDetail());
              var f = Geometry.Raw.RawDecoder.ToRhino(faceMesh);
              f.Scale(UnitConverter.ToRhinoUnits);

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
      this IEnumerable<ARDB.GeometryObject> geometries
    )
    {
      foreach (var geometry in geometries)
      {
        if (geometry?.Visibility != ARDB.Visibility.Visible)
          continue;

        switch (geometry)
        {
          case ARDB.GeometryInstance instance:
          {
            var xform = instance.Transform.ToTransform();
            foreach (var g in instance.SymbolGeometry.GetPreviewWires())
            {
              g?.Transform(xform);
              yield return g;
            }
            break;
          }
          case ARDB.Solid solid:
          {
            if (solid.Faces.IsEmpty)
              continue;

            foreach (var wire in solid.Edges.Cast<ARDB.Edge>().Select(x => x.AsCurve()).GetPreviewWires())
              yield return wire;
            break;
          }
          case ARDB.Face face:
          {
            foreach (var wire in face.GetEdgesAsCurveLoops().SelectMany(x => x.GetPreviewWires()))
              yield return wire;
            break;
          }
          case ARDB.Edge edge:
          {
            yield return edge.AsCurve().ToCurve();
            break;
          }
          case ARDB.Curve curve:
          {
            yield return curve.ToCurve();
            break;
          }
          case ARDB.PolyLine polyline:
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
