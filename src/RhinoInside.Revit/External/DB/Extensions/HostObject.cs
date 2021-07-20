using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class HostObjectExtension
  {
    public static Sketch GetSketch(this HostObject host)
    {
#if REVIT_2022
      switch (host)
      {
        case Ceiling ceiling: return host.Document.GetElement(ceiling.SketchId) as Sketch;
        case Floor floor: return host.Document.GetElement(floor.SketchId) as Sketch;
        case Wall wall: return host.Document.GetElement(wall.SketchId) as Sketch;
      }
#endif
      return host.GetFirstDependent<Sketch>();
    }
  }
}
