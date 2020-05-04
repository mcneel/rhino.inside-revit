using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit
{
  /// <summary>
  /// This code is here to help port code that was previous calling Convert class extension methods
  /// </summary>
  //public static partial class Convert_OBSOLETE
  //{
  //  [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Display.PreviewConverter extension methods")]
  //  public static IEnumerable<Rhino.Display.DisplayMaterial> GetPreviewMaterials
  //  (
  //    this IEnumerable<DB.GeometryObject> geometries,
  //    DB.Document doc,
  //    Rhino.Display.DisplayMaterial defaultMaterial
  //  )
  //  => Convert.Display.PreviewConverter.GetPreviewMaterials(geometries, doc, defaultMaterial);

  //  [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Display.PreviewConverter extension methods")]
  //  public static IEnumerable<Mesh> GetPreviewMeshes
  //  (
  //    this IEnumerable<DB.GeometryObject> geometries,
  //    MeshingParameters meshingParameters
  //  )
  //  => Convert.Display.PreviewConverter.GetPreviewMeshes(geometries, meshingParameters);

  //  [Obsolete("\r - For previous behaviour use RhinoInside.Revit.Convert.Display.PreviewConverter extension methods")]
  //  public static IEnumerable<Curve> GetPreviewWires
  //  (
  //    this IEnumerable<DB.GeometryObject> geometries
  //  )
  //  => Convert.Display.PreviewConverter.GetPreviewWires(geometries);
  //};
}
