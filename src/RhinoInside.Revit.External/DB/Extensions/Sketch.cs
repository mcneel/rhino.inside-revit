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
    public static ElementId GetSketchId(this Element owner)
    {
      switch (owner)
      {
        case null:
        case Sketch _:          return ElementIdExtension.InvalidElementId;

#if REVIT_2024
        case Toposolid topo:    return topo.SketchId;
#endif

#if REVIT_2022
        case Ceiling ceiling:   return ceiling.SketchId;
        case Floor floor:       return floor.SketchId;
        case Wall wall:         return wall.SketchId;
        case Opening opening:   return opening.SketchId;
#endif
        case FabricArea area:   return area.SketchId;
        case FabricSheet sheet: return sheet.SketchId;
      }

      return owner.GetDependentElements(ElementSketchFilter).FirstOrDefault() ?? ElementIdExtension.InvalidElementId;
    }
    public static Sketch GetSketch(this Element owner) => owner?.Document.GetElement(GetSketchId(owner)) as Sketch;

#if REVIT_2022
    public static Element GetOwner(this Sketch sketch) => sketch.Document.GetElement(sketch.OwnerId);
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
      if (dependents.FirstOrDefault(x => x.GetSketchId() == id) is Element owner)
        return owner;

      using (var collector = new FilteredElementCollector(document))
      {
        var elementCollector = collector.
          WherePasses(SketchOwnerFilter).
          WherePasses(new BoundingBoxIntersectsFilter(sketch.GetOutline()));

        return elementCollector.Cast<Element>().FirstOrDefault(x => x.GetSketchId() == id);
      }
    }
#endif

    public static IList<IList<CurveElement>> GetProfileCurveElements(this Sketch sketch)
    {
      var document = sketch.Document;
      var curveElements = new IList<CurveElement>[sketch.Profile.Size];
      var loopIndex = 0;
      foreach (CurveArray profile in sketch.Profile)
      {
        curveElements[loopIndex++] = profile.Cast<Curve>().
          Distinct(CurveEqualityComparer.Reference).
          Select(x => document.GetElement(x.Reference.ElementId)).
          OfType<CurveElement>().
          ToList();
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
            scope.Start(sketch.Id);
            return sketch;
          }
      }

      scope.StartWithNewSketch(element.Id);
      return element.GetSketch();
    }
  }
#endif

  public static class SketchPlaneExtension
  {
    public static Element GetHost(this SketchPlane sketchPlane, out Reference hostFace)
    {
      var document = sketchPlane.Document;
      using (var scope = document.CommitScope())
      {
        var symbol = document.EnsureWorkPlaneBasedSymbol();
        if (!document.IsLinked) scope.Commit();

        using (document.RollBackScope())
        {
          using (var create = document.Create())
          {
            var instance = create.NewFamilyInstance(XYZExtension.Zero, symbol, sketchPlane, StructuralType.NonStructural);
            hostFace = instance.HostFace;
            return instance.Host;
          }
        }
      }
    }
  }
}
