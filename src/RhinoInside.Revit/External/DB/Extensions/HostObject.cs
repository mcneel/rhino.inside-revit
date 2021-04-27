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

  public static class SketchExtension
  {
    public static HostObject GetHostObject(this Sketch sketch)
    {
#if REVIT_2022
      return sketch.Document.GetElement(sketch.OwnerId) as HostObject;
#else
      return sketch.GetFirstDependent<HostObject>();
#endif
    }

    public static IList<IList<ModelCurve>> GetAllModelCurves(this Sketch sketch)
    {
      var modelCurves = new IList<ModelCurve>[sketch.Profile.Size];

      var loopIndex = 0;
      foreach (var profile in sketch.Profile.Cast<CurveArray>())
      {
        var loop = modelCurves[loopIndex++] = new ModelCurve[profile.Size];

        var edgeIndex = 0;
        foreach (var edge in profile.Cast<Curve>())
          loop[edgeIndex++] = sketch.Document.GetElement(edge.Reference.ElementId) as ModelCurve;
      }

      return modelCurves;
    }

#if !REVIT_2022
    public static IList<ElementId> GetAllElements(this Sketch sketch)
    {
      var filter = new LogicalOrFilter
      (
        new ElementFilter[]
        {
          new ElementClassFilter(typeof(CurveElement)),
          new ElementClassFilter(typeof(ReferencePlane)),
          new ElementClassFilter(typeof(Dimension)),
        }
      );

      return sketch.GetDependentElements(filter);
    }
#endif
  }
}
