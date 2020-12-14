using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  /// <summary>
  /// Converts <see cref="Extrusion"/> to be transfered to a <see cref="DB.Solid"/>.
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
    internal static DB.Solid ToSolid(/*const*/ Extrusion extrusion, double factor)
    {
      return BrepEncoder.ToSolid(extrusion.ToBrep(), factor);
    }

    internal static DB.Mesh ToMesh(/*const*/ Extrusion extrusion, double factor)
    {
      using (var mp = MeshingParameters.Default)
      {
        mp.MinimumEdgeLength = Revit.ShortCurveTolerance * factor;
        mp.ClosedObjectPostProcess = extrusion.IsSolid;
        mp.JaggedSeams = false;

        using (var mesh = Mesh.CreateFromSurface(extrusion, mp))
          return MeshEncoder.ToMesh(new Mesh[] { mesh }, factor);
      }
    }
    #endregion
  }
}
