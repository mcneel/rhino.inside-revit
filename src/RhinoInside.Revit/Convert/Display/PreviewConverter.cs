using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Display
{
  using Geometry;

  public static class PreviewConverter
  {
    #region GetPreviewMaterials
    static bool HasMultipleMaterials(this IEnumerable<DB.Face> faces)
    {
      if (faces.Any())
      {
        var materialId = faces.First()?.MaterialElementId ?? DB.ElementId.InvalidElementId;
        foreach (var face in faces.Skip(1))
        {
          if (face.MaterialElementId != materialId)
            return true;
        }
      }

      return false;
    }

    /*internal*/
    public static IEnumerable<Rhino.Display.DisplayMaterial> GetPreviewMaterials
(
this IEnumerable<DB.GeometryObject> geometries,
DB.Document doc,
Rhino.Display.DisplayMaterial defaultMaterial
)
    {
      var scaleFactor = Revit.ModelUnits;
      foreach (var geometry in geometries)
      {
        if (geometry.Visibility != DB.Visibility.Visible)
          continue;

        switch (geometry)
        {
          case DB.GeometryInstance instance:
            foreach (var g in instance.GetInstanceGeometry().GetPreviewMaterials(doc, instance.GetInstanceGeometry().MaterialElement.ToDisplayMaterial(defaultMaterial)))
              yield return g;
            break;
          case DB.Mesh mesh:
            if (mesh.NumTriangles <= 0)
              continue;

            var sm = doc.GetElement(mesh.MaterialElementId) as DB.Material;
            yield return sm.ToDisplayMaterial(defaultMaterial);
            break;
          case DB.Solid solid:
            if (solid.Faces.IsEmpty)
              continue;

            var solidFaces = solid.Faces.OfType<DB.Face>();
            bool useMultipleMaterials = solidFaces.HasMultipleMaterials();

            foreach (var face in solidFaces)
            {
              var fm = doc.GetElement(face.MaterialElementId) as DB.Material;
              yield return fm.ToDisplayMaterial(defaultMaterial);

              if (!useMultipleMaterials)
                break;
            }
            break;
        }
      }
    }
    #endregion

    #region GetPreviewMeshes
    static double LevelOfDetail(this MeshingParameters value) => value?.RelativeTolerance ?? 0.15;

    /*internal*/
    public static IEnumerable<Mesh> GetPreviewMeshes
    (
      this IEnumerable<DB.GeometryObject> geometries,
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
            foreach (var g in instance.SymbolGeometry.GetPreviewMeshes(meshingParameters))
            {
              g?.Transform(xform);
              yield return g;
            }
            break;
          }
          case DB.Mesh mesh:
          {
            if (mesh.NumTriangles <= 0)
              continue;

            var f = Geometry.Raw.RawDecoder.ToRhino(mesh);
            UnitConverter.Scale(f, UnitConverter.ToRhinoUnits);

            yield return f;
            break;
          }
          case DB.Face face:
          {
            var faceMesh = face.Triangulate(meshingParameters.LevelOfDetail());
            var f = Geometry.Raw.RawDecoder.ToRhino(faceMesh);
            UnitConverter.Scale(f, UnitConverter.ToRhinoUnits);

            yield return f;
            break;
          }
          case DB.Solid solid:
          {
            if (solid.Faces.IsEmpty)
              continue;

            var solidFaces = solid.Faces.OfType<DB.Face>();
            bool useMultipleMaterials = solidFaces.HasMultipleMaterials();
            var facesMeshes = useMultipleMaterials ? null : new List<Mesh>(solid.Faces.Size);
            foreach (var face in solidFaces)
            {
              var faceMesh = face.Triangulate(meshingParameters.LevelOfDetail());
              var f = Geometry.Raw.RawDecoder.ToRhino(faceMesh);
              UnitConverter.Scale(f, UnitConverter.ToRhinoUnits);

              if (facesMeshes is null)
                yield return f;
              else if (f is object)
                facesMeshes.Add(f);
            }

            if (facesMeshes is object)
            {
              if (facesMeshes.Count > 0)
              {
                var mesh = new Mesh();

                mesh.Append(facesMeshes);
                yield return mesh;
              }
              else yield return null;
            }
            break;
          }
        }
      }
    }
    #endregion

    #region GetPreviewWires
    /*internal*/
    public static IEnumerable<Curve> GetPreviewWires
    (
      this IEnumerable<DB.GeometryObject> geometries
    )
    {
      var scaleFactor = Revit.ModelUnits;
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
