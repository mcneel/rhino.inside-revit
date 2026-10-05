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

  abstract class ElementCollector
  {
    internal virtual IEnumerable<Element> GetSource() => Array.Empty<Element>();
    internal virtual ElementFilter[] GetFilters() => Array.Empty<ElementFilter>();
    internal virtual Func<Element, bool> GetPredicate() => default;

    internal abstract int Count { get; }
  }

  abstract class ElementCollector<T> : ElementCollector, IEnumerable<T> where T : Element
  {
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<T> GetEnumerator()
    {
      var source = GetSource();
      var filters = GetFilters();
      var filter = filters.Length > 0 ? ElementFilters.Intersect(filters) : null;

      if (source is FilteredElementCollector collector)
      {
        collector = collector.WherePasses(ElementFilters.ElementClassFilter(typeof(T)));
        if (filter?.IsUniverse() is false)
          collector = collector.WherePasses(ElementFilters.Intersect(filters));

        if (GetPredicate() is Func<Element, bool> predicate)
          source = collector.Where(predicate);
        else
          source = collector;
      }
      else
      {
        if (GetPredicate() is Func<Element, bool> predicate)
          source = source.Where(predicate);

        if (filter?.IsUniverse() is false)
          source = source.Where(x => filter.PassesFilter(x));
      }

      return source.OfType<T>().GetEnumerator();
    }

    internal override int Count => Enumerable.Count(this);
  }

  class DocumentCollector<T> : ElementCollector<T> where T : Element
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

    internal override IEnumerable<Element> GetSource()
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

    internal override Func<Element, bool> GetPredicate()
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

      return default;
    }

    internal override int Count
    {
      get
      {
        if (ViewId is null)
        {
          var source = GetSource() as FilteredElementCollector;
          using (source) return source.GetElementCount();
        }
        if (Document.GetElement(ViewId) is View view)
        {
          if (LinkId is object && view.GetVisibleLink<RevitLinkInstance>(LinkId, out var _) is null) return 0;
        }
        else return 0;

        return base.Count;
      }
    }
  }

  class EnumerableCollector<T> : ElementCollector<T> where T : Element
  {
    readonly IEnumerable<Element> Source;
    public EnumerableCollector(IEnumerable<Element> source) => Source = source;

    internal override IEnumerable<Element> GetSource() => Source;
  }

  class FilteredCollector<T> : ElementCollector<T> where T : Element
  {
    protected readonly ElementCollector Collector;
    protected readonly ElementFilter[] Filters;

    public FilteredCollector(ElementCollector collector)
    {
      Collector = collector;
      Filters = default;
    }

    public FilteredCollector(ElementCollector collector, ElementFilter filter)
    {
      Collector = collector;
      Filters = collector.GetFilters().Append(filter).ToArray();
    }

    internal override IEnumerable<Element> GetSource() => Collector.GetSource();
    internal override ElementFilter[] GetFilters() => Filters ?? Collector.GetFilters();
    internal override Func<Element, bool> GetPredicate() => Collector.GetPredicate();
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

    internal static void AssertIsValidFiler(this ElementFilter filter, bool links)
    {
      if (links)
      {
        if (!IsDocumentAgnosticFilter(filter))
          throw new System.ComponentModel.WarningException("Complex filtering is not supported on linked models.");
      }
    }

    internal static void AssertIsValidFiler(this ElementFilter filter, RevitLinkInstance instance)
    {
      AssertIsValidFiler(filter, instance is object);
    }

    internal static FilteredElementCollector WherePasses(this FilteredElementCollector source, ElementFilter filter, RevitLinkInstance instance)
    {
      AssertIsValidFiler(filter, instance);
      return source.WherePasses(filter);
    }

    internal static IEnumerable<T> WherePasses<T>(this IEnumerable<T> source, ElementFilter filter, RevitLinkInstance instance) where T : Element
    {
      AssertIsValidFiler(filter, instance);
      return source.WherePasses(filter);
    }

    public static IEnumerable<Element> CollectElements(this Document document) => new DocumentCollector<Element>(document);
    public static IEnumerable<Element> CollectElements(this View view) => new DocumentCollector<Element>(view.Document, view.Id);
    public static IEnumerable<Element> CollectElements(this View view, ElementId linkId) => new DocumentCollector<Element>(view.Document, view.Id, linkId);

    public static int Count<T>(this IEnumerable<T> source) where T : Element
    {
      switch (source)
      {
        case FilteredElementCollector collector: return collector.GetElementCount();
        case ElementCollector<T> collector: return collector.Count;
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

    public static IEnumerable<T> OfClass<T>(this IEnumerable<Element> source) where T : Element
    {
      switch (source)
      {
        case IEnumerable<T> enumerable:          return enumerable;
        case ElementCollector elementCollector:  return new FilteredCollector<T>(elementCollector);
        default: return new EnumerableCollector<T>(source);
      }
    }

    public static IEnumerable<T> WherePasses<T>(this IEnumerable<T> source, ElementFilter filter) where T : Element
    {
      switch (source)
      {
        case ElementCollector<T> collector: return new FilteredCollector<T>(collector, filter);
        default: return new FilteredCollector<T>(new EnumerableCollector<T>(source), filter);
      }
    }
  }
}
