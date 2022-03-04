using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class SketchExtension
  {
    public static Sketch GetSketch(this Element owner)
    {
#if REVIT_2022
      switch (owner)
      {
        case Ceiling ceiling: return owner.Document.GetElement(ceiling.SketchId) as Sketch;
        case Floor floor:     return owner.Document.GetElement(floor.SketchId) as Sketch;
        case Wall wall:       return owner.Document.GetElement(wall.SketchId) as Sketch;
        case Opening opening: return owner.Document.GetElement(opening.SketchId) as Sketch;
      }
#endif

      return owner.Category?.CategoryType == CategoryType.Model ?
        owner.GetFirstDependent<Sketch>() : default;
    }

    public static T GetOwner<T>(this Sketch sketch) where T : Element
    {
#if REVIT_2022
      return sketch.Document.GetElement(sketch.OwnerId) as T;
#else
      return sketch.GetDependents<T>().Where(x => x.Category?.CategoryType == CategoryType.Model).FirstOrDefault();
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
