using System.Diagnostics;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  using Units;

  /// <summary>
  /// Converts <see cref="Mesh"/> to be transfered to a <see cref="DB.Mesh"/>.
  /// </summary>
  public static class MeshEncoder
  {
    #region Encode
    public static Mesh ToRawMesh(Mesh mesh) => ToRawMesh(mesh, UnitConverter.ToHostUnits);
    internal static Mesh ToRawMesh(Mesh mesh, double scaleFactor)
    {
      mesh = mesh.DuplicateShallow() as Mesh;
      return EncodeRaw(ref mesh, scaleFactor) ?
        mesh :
        default;
    }

    public static Brep ToRawBrep(Mesh mesh) => ToRawBrep(mesh, UnitConverter.ToHostUnits);
    internal static Brep ToRawBrep(Mesh mesh, double scaleFactor)
    {
      mesh = mesh.DuplicateShallow() as Mesh;
      return EncodeRaw(ref mesh, scaleFactor, Revit.ShortCurveTolerance) ?
        Brep.CreateFromMesh(mesh, true) :
        default;
    }

    /// <summary>
    /// Scales <paramref name="mesh"/> by <paramref name="scaleFactor"/>,
    /// splits non planar quads and removes faces with edges under tolerance.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="scaleFactor"></param>
    /// <returns>false if <paramref name="mesh"/> is not valid or too small</returns>
    internal static bool EncodeRaw(ref Mesh mesh, double scaleFactor) =>
      EncodeRaw(ref mesh, scaleFactor, Revit.VertexTolerance);
    
    static bool EncodeRaw(ref Mesh mesh, double scaleFactor, double tolerance)
    {
      if (scaleFactor != 1.0 && !mesh.Scale(scaleFactor))
        return default;

      var bbox = mesh.GetBoundingBox(false);
      if (!bbox.IsValid || bbox.Diagonal.Length < tolerance)
        return default;

      // Revit needs strictly planar faces
      if (mesh.Faces.Where(x => x.IsQuad).Any())
        mesh.Faces.ConvertNonPlanarQuadsToTriangles(tolerance, RhinoMath.UnsetValue, 5);

      // Revit needs edges to be greater than VertexTolerance length
      while (mesh.CollapseFacesByEdgeLength(false, tolerance) > 0) ;

      // Combine identical vertices to produce a close mesh
      mesh.Vertices.CombineIdentical(true, true);

      return true;
    }
    #endregion

    #region Transfer
    /// <summary>
    /// Replaces <see cref="Raw.RawEncoder.ToHost(Mesh)"/> to catch Revit Exceptions
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    public static DB.Mesh ToMesh(/*const*/ Mesh mesh)
    {
      try
      {
        return Raw.RawEncoder.ToHost(mesh);
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException e)
      {
        Debug.Fail(e.Source, e.Message);
      }

      return default;
    }
    #endregion
  }
}
