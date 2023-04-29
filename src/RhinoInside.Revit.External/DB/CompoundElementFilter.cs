using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;

namespace RhinoInside.Revit.External.DB
{
  using Extensions;

  internal static class CompoundElementFilter
  {
    #region Implementation Details
    /// <summary>
    /// ElementFilter used internaly to skip certain internal elements 
    /// </summary>
    /// <param name="doc">May be useful in the future to exclude certain elements</param>
    /// <returns></returns>
    internal static ElementFilter ElementIsNotInternalFilter(Document doc)
    {
      return Union
      (
        ElementIsElementTypeFilter(),
        ElementHasCategoryFilter
      );
    }

    internal const double BoundingBoxLimits = 1e+9;
    public static ElementFilter ElementHasBoundingBoxFilter { get; } = new BoundingBoxIsInsideFilter(new Outline(new XYZ(-BoundingBoxLimits, -BoundingBoxLimits, -BoundingBoxLimits), new XYZ(+BoundingBoxLimits, +BoundingBoxLimits, +BoundingBoxLimits)));
    public static ElementFilter ElementHasCategoryFilter { get; } = new ElementCategoryFilter(BuiltInCategory.INVALID, inverted: true);
    private static ElementFilter ElementIsElementTypeFilterInstance { get; } = new ElementIsElementTypeFilter(inverted: false);
    private static ElementFilter ElementIsNotElementTypeFilterInstance { get; } = new ElementIsElementTypeFilter(inverted: true);
    private static FilterNumericRuleEvaluator NumericEqualsEvaluator { get; } = new FilterNumericEquals();
    private static ParameterValueProvider SubCategoryParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.FAMILY_ELEM_SUBCATEGORY));
    private static ParameterValueProvider FamilyNameParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_FAMILY_NAME));
    private static ParameterValueProvider TypeNameParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_TYPE_NAME));
    private static ParameterValueProvider ElemTypeParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM));

    internal static FilterStringRule FilterStringRule(FilterableValueProvider valueProvider, FilterStringRuleEvaluator evaluator, string ruleString)
    {
#if REVIT_2023
      return new FilterStringRule(valueProvider, evaluator, ruleString);
#else
      return new FilterStringRule(valueProvider, evaluator, ruleString, caseSensitive: true);
#endif
    }
    #endregion

    #region Logical Filters
    /// <summary>
    /// <see cref="Autodesk.Revit.DB.ElementFilter"/> that returns no element on any Revit <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
#if REVIT_2019
    public static ElementFilter Empty => new LogicalAndFilter
#else
    public static ElementFilter Empty { get; } = new LogicalAndFilter
#endif
    (
      new ElementFilter[]
      {
        ElementIsElementTypeFilter(true),
        ElementIsElementTypeFilter(false)
      }
    );

    public static bool IsEmpty(this ElementFilter filter)
    {
#if REVIT_2019
      if (filter is LogicalAndFilter and)
      {
        bool hasFalse = false, hasTrue = false;
        foreach (var type in and.GetFilters().OfType<ElementIsElementTypeFilter>())
          if (type.Inverted)
            hasTrue = true;
          else
            hasFalse = true;

        return hasTrue && hasFalse;
      }

      return false;
#else
      return ReferenceEquals(filter, Empty);
#endif
    }

    /// <summary>
    /// <see cref="Autodesk.Revit.DB.ElementFilter"/> that returns all elements on any Revit <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
#if REVIT_2019
    public static ElementFilter Universe => new LogicalOrFilter
#else
    public static ElementFilter Universe { get; } = new LogicalOrFilter
#endif
    (
      new ElementFilter[]
      {
        ElementIsElementTypeFilter(true),
        ElementIsElementTypeFilter(false)
      }
    );

    public static bool IsUniverse(this ElementFilter filter)
    {
#if REVIT_2019
      if (filter is LogicalOrFilter or)
      {
        bool hasFalse = false, hasTrue = false;
        foreach (var type in or.GetFilters().OfType<ElementIsElementTypeFilter>())
          if (type.Inverted)
            hasTrue = true;
          else
            hasFalse = true;

        return hasTrue && hasFalse;
      }

      return false;
#else
      return ReferenceEquals(filter, Universe);
#endif
    }

    internal static ElementFilter InclusionFilter(Element element)
    {
#if REVIT_2021
      return ((ElementFilter) new ElementIdSetFilter(new ElementId[] { element.Id }));
#else
      return ((ElementFilter) new ElementIdSetFilter(new ElementId[] { element.Id })).
             Intersect(ElementClassFilter(element.GetType()));
#endif
    }

    public static ElementFilter ExclusionFilter(ElementId id, bool inverted = false) => inverted ?
      (ElementFilter) new ElementIdSetFilter(new ElementId[] { id }) :
      (ElementFilter) new ExclusionFilter   (new ElementId[] { id }) ;

    public static ElementFilter ExclusionFilter(ICollection<ElementId> ids, bool inverted = false) =>
      ids.Count == 0 ?
      (inverted ? Empty : Universe) :
      (inverted ? (ElementFilter) new ElementIdSetFilter(ids) : (ElementFilter) new ExclusionFilter(ids));
    #endregion

    #region Generic Filters
    public static ElementFilter ElementClassFilter(Type type)
    {
           if (typeof(Area).IsAssignableFrom(type))         return new AreaFilter();
      else if (typeof(AreaTag).IsAssignableFrom(type))      return new AreaTagFilter();
      else if (typeof(Room).IsAssignableFrom(type))         return new RoomFilter();
      else if (typeof(RoomTag).IsAssignableFrom(type))      return new RoomTagFilter();
      else if (typeof(Space).IsAssignableFrom(type))        return new SpaceFilter();
      else if (typeof(SpaceTag).IsAssignableFrom(type))     return new SpaceTagFilter();
      else if (typeof(Mullion) == type)                     return new ElementClassFilter(typeof(FamilyInstance)).Intersect(new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallMullions));
      else if (typeof(Panel) == type)                       return new ElementClassFilter(typeof(FamilyInstance)).Intersect(new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallPanels));
      else if (typeof(CurveElement).IsAssignableFrom(type)) return new ElementClassFilter(typeof(CurveElement));
      else if (typeof(ElementType) == type)                 return new ElementIsElementTypeFilter();
      else if (typeof(Element) != type)                     return new ElementClassFilter(type);

      return Universe;
    }

    public static ElementFilter ElementClassFilter(params Type[] types)
    {
      bool allTypesIncluded = false;
      bool specialTypesIncluded = false;
      var set = new HashSet<Type>(types);
      var filters = new List<ElementFilter>();

      if (set.Remove(typeof(Element)))      return Universe;
      if (set.Remove(typeof(ElementType)))  { filters.Add(new ElementIsElementTypeFilter());  specialTypesIncluded = allTypesIncluded = true; }
      if (set.Remove(typeof(Area)))         { filters.Add(new AreaFilter());                  specialTypesIncluded = true; }
      if (set.Remove(typeof(AreaTag)))      { filters.Add(new AreaTagFilter());               specialTypesIncluded = true; }
      if (set.Remove(typeof(Room)))         { filters.Add(new RoomFilter());                  specialTypesIncluded = true; }
      if (set.Remove(typeof(RoomTag)))      { filters.Add(new RoomTagFilter());               specialTypesIncluded = true; }
      if (set.Remove(typeof(Space)))        { filters.Add(new SpaceFilter());                 specialTypesIncluded = true; }
      if (set.Remove(typeof(SpaceTag)))     { filters.Add(new SpaceTagFilter());              specialTypesIncluded = true; }

      types = !specialTypesIncluded ? types:
        allTypesIncluded ?
        set.Where(x => !x.IsSubclassOf(typeof(ElementType))).ToArray() :
        set.ToArray();

      switch (types.Length)
      {
        case 0: break;
        case 1: filters.Add(new ElementClassFilter(types[0]));    break;
        default: filters.Add(new ElementMulticlassFilter(types)); break;
      }

      return Union(filters);
    }

    public static ElementFilter ElementKindFilter(ElementKind kind, bool? elementType, bool inverted = false)
    {
      var filters = new List<ElementFilter>();

      if (inverted)
      {
        kind = (kind.HasFlag(ElementKind.System) ?    ElementKind.None : ElementKind.System) |
               (kind.HasFlag(ElementKind.Component) ? ElementKind.None : ElementKind.Component) |
               (kind.HasFlag(ElementKind.Direct) ?    ElementKind.None : ElementKind.Direct);
      }

      if (kind.HasFlag(ElementKind.Component) != kind.HasFlag(ElementKind.System))
      {
        if (elementType != true)
          filters.Add(new ElementClassFilter(typeof(FamilyInstance), kind.HasFlag(ElementKind.System)));

        if (elementType != false)
          filters.Add(new ElementClassFilter(typeof(FamilySymbol), kind.HasFlag(ElementKind.System)));
      }

      if (kind.HasFlag(ElementKind.Direct) != kind.HasFlag(ElementKind.System))
      {
        if (elementType != true)
          filters.Add(new ElementClassFilter(typeof(DirectShape), kind.HasFlag(ElementKind.System)));

        if (elementType != false)
          filters.Add(new ElementClassFilter(typeof(DirectShapeType), kind.HasFlag(ElementKind.System)));
      }

      return filters.Count == 0 ? default :
        kind.HasFlag(ElementKind.System) ?
        Intersect(filters) :
        Union(filters);
    }

    public static ElementFilter ElementIsElementTypeFilter(bool inverted = false) => inverted ?
      ElementIsNotElementTypeFilterInstance : ElementIsElementTypeFilterInstance;

    public static ElementFilter ElementCategoryFilter(IList<ElementId> categoryIds, bool inverted = false, bool includeSubCategories = false)
    {
      if (categoryIds.Count == 0) return Empty;
      if (categoryIds.Count == 1 && !includeSubCategories) return new ElementCategoryFilter(categoryIds[0], inverted);

      var filters = new List<ElementFilter>();

      if (categoryIds.Count == 1) filters.Add(new ElementCategoryFilter(categoryIds[0], inverted));
      else if (categoryIds.Count > 1) filters.Add(new ElementMulticategoryFilter(categoryIds, inverted));

      if (includeSubCategories)
      {
        foreach (var id in categoryIds)
        {
          using (var rule = new FilterElementIdRule(SubCategoryParamProvider, NumericEqualsEvaluator, id))
            filters.Add(new ElementParameterFilter(rule, inverted));
        }
      }

      return inverted ? Intersect(filters) : Union(filters);
    }

    public static ElementFilter ElementSubCategoryFilter(IList<ElementId> categoryIds, bool inverted = false)
    {
      var filters = categoryIds.Select
      (
        x =>
        {
          using (var rule = new FilterElementIdRule(SubCategoryParamProvider, NumericEqualsEvaluator, x))
            return new ElementParameterFilter(rule, inverted);
        }
      ).ToArray();

      return inverted ? Intersect(filters) : Union(filters);
    }

    internal static ElementFilter ElementFamilyNameFilter(string familyName, bool inverted = false)
    {
      using (var evaluator = new FilterStringEquals())
      using (var rule = FilterStringRule(FamilyNameParamProvider, evaluator, familyName))
      {
        return new ElementParameterFilter(rule, inverted);
      }
    }

    internal static ElementFilter ElementTypeNameFilter(string typeName, bool inverted = false)
    {
      using (var evaluator = new FilterStringEquals())
      using (var rule = FilterStringRule(TypeNameParamProvider, evaluator, typeName))
      {
        return new ElementParameterFilter(rule, inverted);
      }
    }

    static ElementFilter ElementTypeFilter(ElementType elementType, bool inverted = false)
    {
      using (var rule = new FilterElementIdRule(ElemTypeParamProvider, NumericEqualsEvaluator, elementType?.Id ?? ElementIdExtension.Invalid))
      {
        var parameterFilter = new ElementParameterFilter(rule, inverted);

        // Add Category hint to the query.
        if (elementType?.Category is Category category && category.Parent is null)
        {
          return inverted ?
            parameterFilter.Union    (new ElementCategoryFilter(category.Id, inverted)) :
            parameterFilter.Intersect(new ElementCategoryFilter(category.Id, inverted)) ;
        }

        return parameterFilter;
      }
    }

    public static ElementFilter ElementTypeFilter(IList<ElementType> elementTypes, bool inverted = false)
    {
      if (elementTypes.Count == 0) return Empty;
      if (elementTypes.Count == 1) return ElementTypeFilter(elementTypes[0], inverted);

      if (inverted)
      {
        var rules = elementTypes.
          Distinct(ElementEqualityComparer.SameDocument).
          Select(x => new FilterInverseRule(new FilterElementIdRule(ElemTypeParamProvider, NumericEqualsEvaluator, x?.Id ?? ElementIdExtension.Invalid)));
        return new ElementParameterFilter(rules.ToArray());
      }
      else
      {
        var filters = elementTypes.Select(x => ElementTypeFilter(x, inverted: false));
        return Union(filters.ToArray());
      }
    }
    #endregion

    #region Operators
    enum FilterCost
    {
      Null = -1,
      Empty = 0,
      Quick = 1,
      Logical = 2,
      Slow = 4,
      Universe = int.MaxValue
    }

    private static FilterCost GetFilterCost(this ElementFilter filter)
    {
      if (ReferenceEquals(filter, null))      return FilterCost.Null;
      if (ReferenceEquals(filter, Empty))     return FilterCost.Empty;
      if (ReferenceEquals(filter, Universe))  return FilterCost.Universe;

      switch (filter)
      {
        case ElementQuickFilter _:            return FilterCost.Quick;
        case ElementLogicalFilter logical:    return logical.GetFilterCost();
        default:                              return FilterCost.Slow;
      }
    }

    private static FilterCost GetFilterCost(this ElementLogicalFilter logical)
    {
      return FilterCost.Logical;

      // Documentation says Revit is already reordering operators
      // So we don't need a more complex implementation
      //
      //var filters = logical.GetFilters();
      //int cost = 0;
      //for (int f = 0; f < filters.Count; ++f)
      //  cost += (int) filters[f].GetFilterCost();

      //return (FilterCost) cost;
    }

    public static ElementFilter Union(this ElementFilter self, ElementFilter other)
    {
      var selfCost = self.GetFilterCost();
      var otherCost = other.GetFilterCost();

      if (selfCost  == FilterCost.Universe  || otherCost == FilterCost.Universe) return Universe;
      if (selfCost  == FilterCost.Empty     || selfCost  == FilterCost.Null) return other;
      if (otherCost == FilterCost.Empty     || otherCost == FilterCost.Null) return self;

      return selfCost < otherCost ?
        new LogicalOrFilter(self, other) :
        new LogicalOrFilter(other, self);
    }

    public static ElementFilter Union(params ElementFilter[] filters) => Union(filters as IList<ElementFilter>);

    public static ElementFilter Union(IList<ElementFilter> filters)
    {
      if (filters.Count == 0) return Empty;
      if (filters.Count == 1) return filters[0] ?? Empty;

      var list = new List<ElementFilter>(filters.Count);
      foreach (var filter in filters.Distinct())
      {
        if (ReferenceEquals(filter, Universe)) return Universe;
        if (ReferenceEquals(filter, Empty)) continue;
        if (ReferenceEquals(filter, null)) continue;
        list.Add(filter);
      }

      if (list.Count == 0) return Empty;
      if (list.Count == 1) return list[0];
      return new LogicalOrFilter(list);
    }

    public static ElementFilter Intersect(this ElementFilter self, ElementFilter other)
    {
      var selfCost = self.GetFilterCost();
      var otherCost = other.GetFilterCost();

      if (selfCost  == FilterCost.Empty     || otherCost == FilterCost.Empty) return Empty;
      if (selfCost  == FilterCost.Universe  || selfCost  == FilterCost.Null) return other;
      if (otherCost == FilterCost.Universe  || otherCost == FilterCost.Null) return self;

      return selfCost < otherCost ?
        new LogicalAndFilter(self, other) :
        new LogicalAndFilter(other, self);
    }

    public static ElementFilter Intersect(params ElementFilter[] filters) => Intersect(filters as IList<ElementFilter>);

    public static ElementFilter Intersect(IList<ElementFilter> filters)
    {
      if (filters.Count == 0) return Empty;
      if (filters.Count == 1) return filters[0] ?? Empty;

      var list = new List<ElementFilter>(filters.Count);
      foreach (var filter in filters.Distinct())
      {
        if (ReferenceEquals(filter, Empty)) return Empty;
        if (ReferenceEquals(filter, Universe)) continue;
        if (ReferenceEquals(filter, null)) continue;
        list.Add(filter);
      }

      if (list.Count == 0) return Empty;
      if (list.Count == 1) return list[0];
      return new LogicalAndFilter(list);
    }

    public static ElementFilter ThatIncludes(this ElementFilter self, params ElementId[] idsToInclude) =>
      self.Intersect(ExclusionFilter(idsToInclude, inverted: true));

    public static ElementFilter ThatExcludes(this ElementFilter self, params ElementId[] idsToExclude) =>
      self.Intersect(ExclusionFilter(idsToExclude, inverted: false));
    #endregion

    #region Geometry
    public static ElementFilter BoundingBoxIntersectsFilter(Outline outline, double tolerance, bool inverted)
    {
      if (outline.IsEmpty)
        return inverted ? Universe : Empty;

      return new BoundingBoxIntersectsFilter(outline, tolerance, inverted);
    }
    #endregion
  }
}

#if !REVIT_2021
namespace Autodesk.Revit.DB
{
  using System.Diagnostics;
  using RhinoInside.Revit.External.DB;
  using RhinoInside.Revit.External.DB.Extensions;

  class ElementIdSetFilter : IDisposable
  {
    static readonly FilterNumericRuleEvaluator NumericEqualsEvaluator = new FilterNumericEquals();
    static readonly ParameterValueProvider IdParamProvider = new ParameterValueProvider(new ElementId(BuiltInParameter.ID_PARAM));

    readonly HashSet<ElementId> IdsToInclude;
    public ElementIdSetFilter(ICollection<ElementId> idsToInclude) => IdsToInclude = new HashSet<ElementId>(idsToInclude);
    public ICollection<ElementId> GetIdsToInclude() => IdsToInclude;

    public bool IsValidObject => true;
    public bool Inverted { get; protected set; }
    public virtual void Dispose() { }

    public bool PassesFilter(Document document, ElementId id) => IdsToInclude.Contains(id);
    public bool PassesFilter(Element element) => IdsToInclude.Contains(element.Id);

    public static implicit operator ElementFilter(ElementIdSetFilter filter) => CompoundElementFilter.Union
    (
      filter.IdsToInclude.
      Select(x => new ElementParameterFilter(new FilterElementIdRule(IdParamProvider, NumericEqualsEvaluator, x))).
      ToArray()
    );
  }

  class VisibleInViewFilter : IDisposable
  {
    readonly Document Document;
    readonly ElementId ViewId;
    readonly ICollection<ElementId> VisibleElementIds;
    readonly ICollection<ElementId> VisibleCategoryIds;

    public VisibleInViewFilter(Document document, ElementId viewId) : this(document, viewId, inverted: false) { }

    public VisibleInViewFilter(Document document, ElementId viewId, bool inverted)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (viewId is null) throw new ArgumentNullException(nameof(viewId));

      Inverted = inverted;
      Document = document;
      ViewId = viewId;

      using (var collector = new FilteredElementCollector(document, viewId))
      {
        VisibleElementIds = collector.ToReadOnlyElementIdCollection();
        VisibleCategoryIds = new HashSet<ElementId>(collector.Select(x => x.Category).OfType<Category>().Select(x => x.Id));
      }

#if DEBUG
      foreach (var element in VisibleElementIds.Select(x => Document.GetElement(x)))
      {
        Debug.Assert(!(element is ElementType), $"Casting operator may need to be adjusted to accept {element}");
        Debug.Assert
        (
          element.OwnerViewId == viewId ||
          CompoundElementFilter.ElementHasBoundingBoxFilter.PassesFilter(element) ||
          (element.Category is object && VisibleCategoryIds.Contains(element.Category.Id)),
          "casting operator needs to be adjusted"
        );
      }
#endif
    }

    public bool IsValidObject => Document.IsValidObject;
    public bool Inverted { get; protected set; }
    public void Dispose() { }

    public bool PassesFilter(Document document, ElementId id)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (id is null) throw new ArgumentNullException(nameof(id));
      if (!Document.IsEquivalent(document)) throw new ArgumentException("Invalid document", nameof(document));

      return VisibleElementIds.Contains(id) != Inverted;
    }

    public bool PassesFilter(Element element)
    {
      if (element is null) throw new ArgumentNullException(nameof(element));
      if (!Document.IsEquivalent(element.Document)) throw new ArgumentException("Invalid element document", nameof(element));

      return VisibleElementIds.Contains(element.Id) != Inverted;
    }

    static readonly ElementCategoryFilter CamerasCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Cameras);

    public static implicit operator ElementFilter(VisibleInViewFilter filter) => filter.Inverted ?
    CompoundElementFilter.ExclusionFilter(filter.VisibleElementIds, inverted: false):
    CompoundElementFilter.Intersect
    (
    #region Quick exclusion
      // Types are never visible on views.
      CompoundElementFilter.ElementIsElementTypeFilter(inverted: true),
      CompoundElementFilter.Union
      (
        // Elements should have a bbox
        CompoundElementFilter.ElementHasBoundingBoxFilter,
        // or be owned by this view
        new ElementOwnerViewFilter(filter.ViewId),
        // or be on one of those categories
        new ElementMulticategoryFilter(filter.VisibleCategoryIds)
      ),
    #endregion
    #region Slow inclusion
      CompoundElementFilter.Union
      (
        // Cameras do not have parameter Id, so we select all except... ->
        CamerasCategoryFilter,
        CompoundElementFilter.ExclusionFilter(filter.VisibleElementIds, inverted: true)
      ),
      // -> ... the one related to this view.
      CompoundElementFilter.ExclusionFilter
      (
        new ElementId[]
        {
          filter.Document.GetElement(filter.ViewId).
          GetDependentElements(CamerasCategoryFilter).
          FirstOrDefault() ?? ElementIdExtension.Invalid
        }
      )
    #endregion
    );
  }
}
#endif
