using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class HostObjectExtension
  {
    public static SlabShapeEditor GetSlabShapeEditor(this HostObject hostObject)
    {
#if REVIT_2024
      switch (hostObject)
      {
        case Floor floor: return floor.GetSlabShapeEditor();
        case RoofBase roof: return roof.GetSlabShapeEditor();
        case Toposolid toposolid: return toposolid.GetSlabShapeEditor();
      }
#else
      switch (hostObject)
      {
        case Floor floor: return floor.SlabShapeEditor;
        case RoofBase roof: return roof.SlabShapeEditor;
      }
#endif

      return null;
    }
  }
}
