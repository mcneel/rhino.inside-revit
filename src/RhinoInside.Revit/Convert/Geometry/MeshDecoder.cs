using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  static class MeshDecoder
  {
    /// <summary>
    /// Replaces <see cref="Raw.RawDecoder.ToRhino(DB.Mesh)"/> to unweld vertices and recreate Ngons
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    internal static Mesh ToRhino(DB.Mesh mesh)
    {
      var result = Raw.RawDecoder.ToRhino(mesh);

      result.Ngons.AddPlanarNgons(Revit.VertexTolerance, 4, 2, true);

      return result;
    }
  }
}
