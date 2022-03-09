using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class SketchExtension
  {
    static readonly ElementFilter ElementSketchFilter = new ElementClassFilter(typeof(Sketch));
    public static Sketch GetSketch(this Element owner)
    {
      switch (owner)
      {
        case null:
        case Sketch _: return null;

#if REVIT_2022
        case Ceiling ceiling: return owner.Document.GetElement(ceiling.SketchId) as Sketch;
        case Floor floor:     return owner.Document.GetElement(floor.SketchId) as Sketch;
        case Wall wall:       return owner.Document.GetElement(wall.SketchId) as Sketch;
        case Opening opening: return owner.Document.GetElement(opening.SketchId) as Sketch;
#endif
        case FabricArea  area : return owner.Document.GetElement(area.SketchId) as Sketch;
        case FabricSheet sheet: return owner.Document.GetElement(sheet.SketchId) as Sketch;
      }

      return owner.GetDependentElements(ElementSketchFilter).Select(owner.Document.GetElement).FirstOrDefault() as Sketch;
    }

#if REVIT_2022
    public static Element GetOwner(this Sketch sketch) =>
      return sketch.Document.GetElement(sketch.OwnerId);
#else
    static readonly ElementFilter SketchOwnerFilter = new ElementMulticlassFilter
    (
      new System.Type[]
      {
        typeof(TopographySurface),
        typeof(SketchPlane),
        typeof(Sketch),
      }, inverted: true
    );

    public static Element GetOwner(this Sketch sketch)
    {
      var document = sketch.Document;
      var id = sketch.Id;

      var dependents = sketch.GetDependentElements(SketchOwnerFilter).Select(document.GetElement);
      if (dependents.Where(x => x.GetSketch()?.Id == id).FirstOrDefault() is Element owner)
        return owner;

      using (var collector = new FilteredElementCollector(document))
      {
        var elementCollector = collector.
          WherePasses(SketchOwnerFilter).
          WherePasses(new BoundingBoxIntersectsFilter(sketch.GetOutline()));

        return elementCollector.Cast<Element>().Where(x => x.GetSketch()?.Id == id).FirstOrDefault();
      }
    }
#endif

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
