using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  using Extensions;

  class ElementEnumerator : IEnumerator<Element>
  {
    public ElementEnumerator(FilteredElementCollector collector, Predicate<Element> predicate)
    {
      Collector = collector;
      Pass = predicate;
    }
    readonly FilteredElementCollector Collector;
    readonly Predicate<Element> Pass;

    IEnumerator<Element> Iterator;

    object IEnumerator.Current => Current;
    public Element Current => Iterator?.Current;

    public bool MoveNext()
    {
      var iterator = (Iterator ?? (Iterator = Collector.GetElementIterator()));
      do
      {
        if (!iterator.MoveNext())
          return false;
      }
      while (!Pass(iterator.Current));

      return true;
    }

    public void Reset() => Iterator.Reset();

    public void Dispose()
    {
      Iterator.Dispose(); Iterator = null;
      Collector.Dispose();
    }
  }

  abstract class ElementCollector : IEnumerable<Element>
  {
    internal abstract FilteredElementCollector GetCollector();
    internal abstract Predicate<Element> Pass { get; }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<Element> GetEnumerator() => new ElementEnumerator(GetCollector(), Pass);
  }

  class DocumentCollector : ElementCollector
  {
    private readonly Document Document;
    private readonly ElementId ViewId;
    private readonly ElementId LinkId;

    public DocumentCollector(Document document) : this(document, null, null) { }
    public DocumentCollector(Document document, ElementId viewId) : this(document, viewId, null) { }
    public DocumentCollector(Document document, ElementId viewId, ElementId linkId)
    {
      Document = document;
      ViewId = viewId;
      LinkId = linkId;
    }

    internal override FilteredElementCollector GetCollector()
    {
      if (ViewId is null)
      {
        if (LinkId is null)
        {
          return new FilteredElementCollector(Document);
        }
        else if (Document.GetElement(LinkId) is RevitLinkInstance link && link.GetLinkDocument() is Document linkDocument)
        {
          return new FilteredElementCollector(linkDocument);
        }
        else
        {
          // This is here to fire an Autodesk.Revit.Exceptions.ArgumentException.
          return new FilteredElementCollector(Document, ElementIdExtension.Invalid);
        }
      }
      else if (Document.GetElement(ViewId) is View view && view.IsModelView())
      {
        if (LinkId is null)
        {
          return new FilteredElementCollector(Document, ViewId);
        }
        else if
        (
          view.CollectElements().WherePassFilter
          (
            CompoundElementFilter.Intersect
            (
              CompoundElementFilter.ElementClassFilter(typeof(RevitLinkInstance)),
              CompoundElementFilter.ExclusionFilter(new ElementId[] { LinkId }, inverted: true)
            )
          ).FirstOrDefault() is RevitLinkInstance
        )
        {
          return view.GetVisibleElementsCollector(LinkId);
        }
      }

      return new FilteredElementCollector(Document).WherePasses(CompoundElementFilter.Empty);
    }

    internal override Predicate<Element> Pass
    {
      get
      {
        if (ViewId is object && Document.GetElement(ViewId) is View view)
        {
          var modelClipBox = view.GetModelClipBox();
          if (modelClipBox.GetPlaneEquations(out var modelClipPlanes, Numerical.Tolerance.Default))
          {
            if (LinkId is object)
            {
              if
              (
                Document.GetElement(LinkId) is RevitLinkInstance link &&
                link.GetLinkDocument() is Document linkDocument &&
                link.GetTransform().TryGetInverse(out var inverse)
              )
              {
                if (modelClipPlanes.X.Min.HasValue) modelClipPlanes.X.Min = inverse.OfPlaneEquation(modelClipPlanes.X.Min.Value);
                if (modelClipPlanes.X.Max.HasValue) modelClipPlanes.X.Max = inverse.OfPlaneEquation(modelClipPlanes.X.Max.Value);
                if (modelClipPlanes.Y.Min.HasValue) modelClipPlanes.Y.Min = inverse.OfPlaneEquation(modelClipPlanes.Y.Min.Value);
                if (modelClipPlanes.Y.Max.HasValue) modelClipPlanes.Y.Max = inverse.OfPlaneEquation(modelClipPlanes.Y.Max.Value);
                if (modelClipPlanes.Z.Min.HasValue) modelClipPlanes.Z.Min = inverse.OfPlaneEquation(modelClipPlanes.Z.Min.Value);
                if (modelClipPlanes.Z.Max.HasValue) modelClipPlanes.Z.Max = inverse.OfPlaneEquation(modelClipPlanes.Z.Max.Value);
              }
              else return x => false;
            }

            return element =>
            {
              if (!element.ViewSpecific)
              {
                if (element.get_BoundingBox(view) is BoundingBoxXYZ bbox)
                {
                  var bboxMin = bbox.Transform.OfPoint(bbox.Min);
                  var bboxMax = bbox.Transform.OfPoint(bbox.Max);

                  if (modelClipPlanes.X.Min?.IsAboveOutline(bboxMin, bboxMax) is true) return false;
                  if (modelClipPlanes.X.Max?.IsAboveOutline(bboxMin, bboxMax) is true) return false;
                  if (modelClipPlanes.Y.Min?.IsAboveOutline(bboxMin, bboxMax) is true) return false;
                  if (modelClipPlanes.Y.Max?.IsAboveOutline(bboxMin, bboxMax) is true) return false;
                  if (modelClipPlanes.Z.Min?.IsAboveOutline(bboxMin, bboxMax) is true) return false;
                  if (modelClipPlanes.Z.Max?.IsAboveOutline(bboxMin, bboxMax) is true) return false;
                }
                else return false;
              }

              return true;
            };
          }
        }

        return x => true;
      }
    }
  }

  class WherePassesCollector : ElementCollector
  {
    readonly ElementCollector Source;
    readonly ElementFilter Filter;
    public WherePassesCollector(ElementCollector source, ElementFilter filter)
    {
      Source = source;
      Filter = filter;
    }

    internal override FilteredElementCollector GetCollector() => Source.GetCollector().WherePasses(Filter);
    internal override Predicate<Element> Pass => Source.Pass;
  }

  class WherePassesEnumerable : IEnumerable<Element>
  {
    readonly IEnumerable<Element> Source;
    readonly ElementFilter Filter;
    public WherePassesEnumerable(IEnumerable<Element> source, ElementFilter filter)
    {
      Source = source;
      Filter = filter;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<Element> GetEnumerator()
    {
      foreach (var element in Source)
      {
        if (!Filter.PassesFilter(element)) continue;
        yield return element;
      }
    }
  }

  public static class ElementEnumerable
  {
    public static IEnumerable<Element> CollectElements(this Document document) => new DocumentCollector(document);
    public static IEnumerable<Element> CollectElements(this View view) => new DocumentCollector(view.Document, view.Id);
    public static IEnumerable<Element> CollectElements(this View view, ElementId linkId) => new DocumentCollector(view.Document, view.Id, linkId);

    public static IEnumerable<Element> WherePassFilter(this IEnumerable<Element> source, ElementFilter filter)
    {
      if (source is ElementCollector collector)
        return new WherePassesCollector(collector, filter);
      else
        return new WherePassesEnumerable(source, filter);
    }
  }
}
