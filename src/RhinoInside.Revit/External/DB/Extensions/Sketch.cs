using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
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
    static readonly ElementFilter SketchAllElementsFilter = new ElementMulticlassFilter
    (
      new System.Type[]
      {
        typeof(CurveElement),
        typeof(ReferencePlane),
        typeof(Dimension),
      }
    );

    public static IList<ElementId> GetAllElements(this Sketch sketch)
    {
      return sketch.GetDependentElements(SketchAllElementsFilter);
    }
#endif
  }
}
