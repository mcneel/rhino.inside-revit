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
  }
}
