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
      sketch.Document.GetElement(sketch.OwnerId);
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

    public static IList<IList<CurveElement>> GetProfileCurveElements(this Sketch sketch)
    {
      var curveElements = new IList<CurveElement>[sketch.Profile.Size];

      var loopIndex = 0;
      foreach (var profile in sketch.Profile.Cast<CurveArray>())
      {
        curveElements[loopIndex++] = profile.Cast<Curve>().
          Distinct(CurveEqualityComparer.Reference).
          Select(x => sketch.Document.GetElement(x.Reference.ElementId) as CurveElement).
          ToArray();
      }

      return curveElements;
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

#if REVIT_2022
  public static class SketchEditScopeExtension
  {
    public static bool IsSketchEditingSupportedForSketchBasedElement(this SketchEditScope scope, Element element)
    {
#if REVIT_2023
      return scope.IsSketchEditingSupportedForSketchBasedElement(element.Id);
#else
      switch (element)
      {
        case Ceiling ceiling: return true;
        case Floor floor:     return true;
        case Wall wall:       return wall.CanHaveProfileSketch();
        case Opening opening: return true;
      }

      return false;
#endif
    }

#if !REVIT_2023
    public static bool IsElementWithoutSketch(this SketchEditScope scope, ElementId elementId)
    {
      // Revit returns false even on Walls whithout sketch.
      return false;
    }
    public static void StartWithNewSketch(this SketchEditScope scope, ElementId elementId)
    {
      throw new System.NotImplementedException($"{nameof(StartWithNewSketch)} is not implement.");
    }
#endif

    public static bool IsElementWithoutSketch(this SketchEditScope scope, Element element)
    {
      return element.GetSketch() is null;
    }

    public static Sketch StartWithNewSketch(this SketchEditScope scope, Element element)
    {
      switch (element)
      {
        case Wall wall:
          using (var tx = element.Document.CommitScope())
          {
            var sketch = wall.CreateProfileSketch();
            tx.Commit();
            return sketch;
          }
      }

      scope.StartWithNewSketch(element.Id);
      return element.GetSketch();
    }
  }
#endif
}
