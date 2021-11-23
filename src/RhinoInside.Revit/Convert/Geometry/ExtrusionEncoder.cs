using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  /// <summary>
  /// Converts <see cref="Extrusion"/> to be transfered to a <see cref="ARDB.Solid"/>.
  /// </summary>
  static class ExtrusionEncoder
  {
    #region Encode
    internal static Brep ToRawBrep(/*const*/ Extrusion extrusion, double scaleFactor)
    {
      var brep = extrusion.ToBrep();
      return BrepEncoder.EncodeRaw(ref brep, scaleFactor) ? brep : default;
    }
    #endregion

    #region Transfer
    internal static ARDB.Solid ToSolid(/*const*/ Extrusion extrusion, double factor)
    {
      return BrepEncoder.ToSolid(extrusion.ToBrep(), factor);
    }

    internal static ARDB.Mesh ToMesh(/*const*/ Extrusion extrusion, double factor)
    {
      using (var mp = MeshingParameters.Default)
      {
        mp.Tolerance = 0.0;// Revit.VertexTolerance / factor;
        mp.MinimumTolerance = 0.0;
        mp.RelativeTolerance = 0.0;

        mp.RefineGrid = false;
        mp.GridAspectRatio = 0.0;
        mp.GridAngle = 0.0;
        mp.GridMaxCount = 0;
        mp.GridMinCount = 0;
        mp.MinimumEdgeLength = MeshEncoder.ShortEdgeTolerance / factor;
        mp.MaximumEdgeLength = 0.0;

        mp.ClosedObjectPostProcess = extrusion.CapCount == 2;
        mp.JaggedSeams = true;
        mp.SimplePlanes = true;

        using (var mesh = Mesh.CreateFromSurface(extrusion, mp))
          return MeshEncoder.ToMesh(new Mesh[] { mesh }, factor);
      }
    }
    #endregion
  }
}
