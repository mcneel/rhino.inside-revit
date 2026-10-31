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

    internal static double GetOffsetForLocationLine(this HostObjAttributes type, WallLocationLine location)
    {
      if (type.GetCompoundStructure() is CompoundStructure compoundStructure)
      {
        if (!compoundStructure.IsVerticallyHomogeneous())
          compoundStructure = CompoundStructure.CreateSimpleCompoundStructure(compoundStructure.GetLayers());

        return compoundStructure.GetOffsetForLocationLine(location);
      }
      else if (type is WallType wallType)
      {
        switch (location)
        {
          case WallLocationLine.WallCenterline:
          case WallLocationLine.CoreCenterline:
            return 0.0;
          case WallLocationLine.FinishFaceExterior:
          case WallLocationLine.CoreExterior:
            return wallType.Width / +2.0;
          case WallLocationLine.FinishFaceInterior:
          case WallLocationLine.CoreInterior:
            return wallType.Width / -2.0;
        }
      }

      return 0.0;
    }
  }
}
