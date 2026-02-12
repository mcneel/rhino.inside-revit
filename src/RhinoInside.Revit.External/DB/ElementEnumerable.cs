using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;

namespace RhinoInside.Revit.External.DB
{
  using Extensions;

  class FilteredElementEnumerator : IEnumerator<Element>
  {
    public FilteredElementEnumerator(ElementCollector source, Predicate<Element> predicate)
    {
      Source = source;
      Predicate = predicate;
    }

    readonly ElementCollector Source;
    readonly Predicate<Element> Predicate;

    FilteredElementCollector Collector;
    FilteredElementIterator Iterator;

    object IEnumerator.Current => Current;
    public Element Current => Iterator?.Current;

    public bool MoveNext()
    {
      Collector ??= Source.GetCollector();
      Iterator ??= Collector.GetElementIterator();

      while (Iterator.MoveNext())
      {
        if (Predicate(Iterator.Current))
          return true;
      }

      Dispose();
      return false;
    }

    public void Reset() => Iterator.Reset();

    public void Dispose()
    {
      using (Iterator) Iterator = null;
      using (Collector) Collector = null;
    }
  }

  abstract class ElementCollector : IEnumerable<Element>
  {
    internal abstract FilteredElementCollector GetCollector();
    internal virtual Predicate<Element> GetPredicate() => x => true;
    internal virtual int Count
    {
      get
      {
        using (var collector = GetCollector())
          return collector.GetElementCount();
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<Element> GetEnumerator() => new FilteredElementEnumerator(this, GetPredicate());
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
      }
      else if (Document.GetElement(ViewId) is View view)
      {
        if (LinkId is null)
        {
          return view.GetVisibleElementsCollector();
        }
        else
        {
          return view.GetVisibleElementsCollector(LinkId);
        }
      }

      return FilteredElementCollectorExtension.Invalid(Document);
    }

    internal override Predicate<Element> GetPredicate()
    {
      if (ViewId is object)
      {
        if (Document.GetElement(ViewId) is View view)
        {
          var modelClipBox = view.GetModelClipBox();
          if (modelClipBox.GetPlaneEquations(out var modelClipPlanes, Numerical.Tolerance.Default))
          {
            if (LinkId.IsValid())
            {
              if
              (
                view.GetVisibleLink<RevitLinkInstance>(LinkId, out var _) is RevitLinkInstance link &&
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
        else return x => false;
      }

      return x => true;
    }

    internal override int Count
    {
      get
      {
        if (ViewId is null) return base.Count;
        if (Document.GetElement(ViewId) is View view)
        {
          if (LinkId is object && view.GetVisibleLink<RevitLinkInstance>(LinkId, out var _) is null) return 0;
        }
        else return 0;

        return Enumerable.Count(this);
      }
    }
  }

  class FilteredCollector : ElementCollector
  {
    readonly FilteredElementCollector Source;
    public FilteredCollector(FilteredElementCollector source) => Source = source;
    internal override FilteredElementCollector GetCollector() => Source;
  }

  class WherePassesCollector : ElementCollector
  {
    readonly ElementCollector Source;
    readonly ElementFilter[] Filters;
    public WherePassesCollector(ElementCollector source)
    {
      Source = source;
      Filters = Array.Empty<ElementFilter>();
    }

    public WherePassesCollector(ElementCollector source, ElementFilter filter)
    {
      Source = source;
      Filters = new ElementFilter[] { filter };
    }

    public WherePassesCollector(WherePassesCollector source, ElementFilter filter)
    {
      Source = source.Source;
      Filters = source.Filters.Append(filter).ToArray();
    }

    internal override FilteredElementCollector GetCollector() => Source.GetCollector().WherePasses(ElementFilters.Intersect(Filters));
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
    private static bool IsDocumentAgnosticFilter(ElementFilter filter)
    {
      switch (filter)
      {
        case ElementIsElementTypeFilter _: return true;
        case ElementClassFilter _: return true;
        case ElementMulticlassFilter _: return true;
        case AreaFilter _: return true;
        case AreaTagFilter _: return true;
        case RoomFilter _: return true;
        case RoomTagFilter _: return true;
        case SpaceFilter _: return true;
        case SpaceTagFilter _: return true;
        case ElementCategoryFilter category: return !category.CategoryId.IsValid() || category.CategoryId.IsBuiltInId();
        case ElementMulticategoryFilter category: return category.GetCategoryIds().All(x => !x.IsValid() || x.IsBuiltInId());
        case ElementIsCurveDrivenFilter _: return true;
        case ElementParameterFilter parameter: return parameter.GetRules().All
        (
          x =>
          x is FilterStringRule ||
          x is FilterInverseRule ||
          x is FilterIntegerRule ||
          (x is FilterCategoryRule category && category.GetCategories().All(x => !x.IsValid() || x.IsBuiltInId()))
        );
#if REVIT_2019
        case ElementLogicalFilter logical: return logical.GetFilters().All(IsDocumentAgnosticFilter);
#endif
      }

      return false;
    }

    internal static void AssertIsValidFiler(this ElementFilter filter, RevitLinkInstance instance)
    {
      if (instance is object)
      {
        if (!IsDocumentAgnosticFilter(filter))
          throw new System.ComponentModel.WarningException("Complex filtering is not supported on linked models.");
      }
    }

    internal static FilteredElementCollector WherePasses(this FilteredElementCollector source, ElementFilter filter, RevitLinkInstance instance)
    {
      AssertIsValidFiler(filter, instance);
      return source.WherePasses(filter);
    }

    internal static IEnumerable<Element> WherePasses(this IEnumerable<Element> source, ElementFilter filter, RevitLinkInstance instance)
    {
      AssertIsValidFiler(filter, instance);
      return source.WherePasses(filter);
    }

    public static IEnumerable<Element> CollectElements(this Document document) => new DocumentCollector(document);
    public static IEnumerable<Element> CollectElements(this View view) => new DocumentCollector(view.Document, view.Id);
    public static IEnumerable<Element> CollectElements(this View view, ElementId linkId) => new DocumentCollector(view.Document, view.Id, linkId);

    public static int Count<T>(this IEnumerable<T> source) where T : Element
    {
      switch (source)
      {
        case FilteredElementCollector collector: return collector.GetElementCount();
        case ElementCollector collector: return collector.Count;
        default: return Enumerable.Count(source);
      }
    }

    public static IEnumerable<T> Take<T>(this IEnumerable<T> source, int count) where T : Element
    {
      if (count < 0) source = source.Reverse();

      switch (count)
      {
        case int.MinValue: return source;
        case -int.MaxValue: return source;
        case 0: return Array.Empty<T>();
        case int.MaxValue: return source;
        default: return Enumerable.Take(source, Math.Abs(count));
      }
    }

    public static IEnumerable<Element> WherePasses(this IEnumerable<Element> source, ElementFilter filter)
    {
      switch (source)
      {
        case FilteredElementCollector collector: return new WherePassesCollector(new FilteredCollector(collector), filter);
        case WherePassesCollector collector: return new WherePassesCollector(collector, filter);
        case ElementCollector collector: return new WherePassesCollector(collector, filter);
        default: return new WherePassesEnumerable(source, filter);
      }
    }
  }
}
