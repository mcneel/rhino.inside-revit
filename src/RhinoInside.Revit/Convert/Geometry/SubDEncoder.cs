using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.Geometry
{
  /// <summary>
  /// Converts <see cref="SubD"/> to be transfered to a <see cref="DB.Solid"/>.
  /// </summary>
  static class SubDEncoder
  {
    #region Encode
    internal static Brep ToRawBrep(/*const*/ SubD subD, double scaleFactor)
    {
      var brep = subD.ToBrep(SubDToBrepOptions.Default);
      return BrepEncoder.EncodeRaw(ref brep, scaleFactor) ? brep : default;
    }
    #endregion
  }
}
