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

    #region Transfer
    internal static DB.Solid ToSolid(/*const*/SubD subD, double factor)
    {
      return BrepEncoder.ToSolid(subD.ToBrep(SubDToBrepOptions.Default), factor);
    }

    internal static DB.Mesh ToMesh(/*const*/ SubD subD, double factor)
    {
      using (var mesh = Mesh.CreateFromSubD(subD, 3))
        return MeshEncoder.ToMesh(new Mesh[] { mesh }, factor);
    }
    #endregion
  }
}
