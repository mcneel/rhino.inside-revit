using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  internal static class CompoundElementFilter
  {
    #region Implementation Details
    private static ElementFilter ElementIsElementTypeFilterInstance { get; } = new ElementIsElementTypeFilter(inverted: false);
    private static ElementFilter ElementIsNotElementTypeFilterInstance { get; } = new ElementIsElementTypeFilter(inverted: true);
    private static FilterNumericRuleEvaluator NumericEqualsEvaluator { get; } = new FilterNumericEquals();
    private static ParameterValueProvider SubCategoryParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.FAMILY_ELEM_SUBCATEGORY));
    private static ParameterValueProvider FamilyNameParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_FAMILY_NAME));
    private static ParameterValueProvider TypeNameParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_TYPE_NAME));
    private static ParameterValueProvider ElemTypeParamProvider { get; } = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM));
    #endregion

    #region Logical Filters
    public static ElementFilter Empty { get; } = new LogicalAndFilter
    (
      new ElementFilter[]
      {
        ElementIsElementTypeFilter(true),
        ElementIsElementTypeFilter(false)
      }
    );

    public static ElementFilter Full { get; } = new LogicalOrFilter
    (
      new ElementFilter[]
      {
        ElementIsElementTypeFilter(true),
        ElementIsElementTypeFilter(false)
      }
    );

    public static ElementFilter ExclusionFilter(ICollection<ElementId> ids, bool inverted = false) =>
      ids.Count == 0 ?
      (inverted ? Empty : Full) :
      (inverted ? (ElementFilter) new ElementIdSetFilter(ids) : (ElementFilter) new ExclusionFilter(ids));
    #endregion

    #region Generic Filters
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
      using (var rule = new FilterStringRule(FamilyNameParamProvider, evaluator, familyName, caseSensitive: true))
      {
        return new ElementParameterFilter(rule, inverted);
      }
    }

    internal static ElementFilter ElementTypeNameFilter(string typeName, bool inverted = false)
    {
      using (var evaluator = new FilterStringEquals())
      using (var rule = new FilterStringRule(TypeNameParamProvider, evaluator, typeName, caseSensitive: true))
      {
        return new ElementParameterFilter(rule, inverted);
      }
    }

    static ElementFilter ElementTypeFilter(ElementType elementType, bool inverted = false)
    {
      using (var rule = new FilterElementIdRule(ElemTypeParamProvider, NumericEqualsEvaluator, elementType.Id))
      {
        if (!inverted && elementType.Category is Category category)
        {
          var filters = new ElementFilter[]
          {
            new ElementCategoryFilter(category.Id, inverted: false),
            new ElementParameterFilter(rule,       inverted: false)
          };

          return Intersect(filters);
        }
        else
        {
          return new ElementParameterFilter(rule, inverted);
        }
      }
    }

    public static ElementFilter ElementTypeFilter(IList<ElementType> elementTypes, bool inverted = false)
    {
      if (elementTypes.Count == 0) return Empty;
      if (elementTypes.Count == 1) return ElementTypeFilter(elementTypes[0]);

      if (inverted)
      {
        var rules = elementTypes.Select(x => new FilterInverseRule(new FilterElementIdRule(ElemTypeParamProvider, NumericEqualsEvaluator, x.Id)));
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
      Empty = 0,
      Quick = 1,
      Logical = 2,
      Slow = 4,
      All = int.MaxValue
    }
    private static FilterCost GetFilterCost(this ElementFilter filter)
    {
      if (ReferenceEquals(filter, Empty)) return FilterCost.Empty;
      if (ReferenceEquals(filter, Full)) return FilterCost.All;

      switch (filter)
      {
        case ElementQuickFilter _: return FilterCost.Quick;
        case ElementLogicalFilter logical: return logical.GetFilterCost();
        default: return FilterCost.Slow;
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

      if (selfCost == FilterCost.All || otherCost == FilterCost.All)
        return Full;

      if (selfCost == FilterCost.Empty) return other;
      if (otherCost == FilterCost.Empty) return self;

      return selfCost < otherCost ?
        new LogicalOrFilter(self, other) :
        new LogicalOrFilter(other, self);
    }

    public static ElementFilter Union(IList<ElementFilter> filters)
    {
      if (filters.Count == 0) return Empty;
      if (filters.Count == 1) return filters[0];

      var list = new List<ElementFilter>(filters.Count);
      foreach (var filter in filters.Distinct())
      {
        if (ReferenceEquals(filter, Full)) return Full;
        if (ReferenceEquals(filter, Empty)) continue;
        list.Add(filter);
      }

      if (list.Count == 1) return list[0];
      return new LogicalOrFilter(list);
    }

    public static ElementFilter Intersect(this ElementFilter self, ElementFilter other)
    {
      var selfCost = self.GetFilterCost();
      var otherCost = other.GetFilterCost();

      if (selfCost == FilterCost.Empty || otherCost == FilterCost.Empty)
        return Empty;

      if (selfCost == FilterCost.All) return other;
      if (otherCost == FilterCost.All) return self;

      return selfCost < otherCost ?
        new LogicalAndFilter(self, other) :
        new LogicalAndFilter(other, self);
    }

    public static ElementFilter Intersect(IList<ElementFilter> filters)
    {
      if (filters.Count == 0) return Empty;

      var list = new List<ElementFilter>(filters.Count);
      foreach (var filter in filters.Distinct())
      {
        if (ReferenceEquals(filter, Empty)) return Empty;
        if (ReferenceEquals(filter, Full)) continue;
        list.Add(filter);
      }

      if (list.Count == 1) return list[0];
      return new LogicalAndFilter(list);
    }

    public static ElementFilter ThatIncludes(this ElementFilter self, params ElementId[] idsToInclude) =>
      self.Intersect(ExclusionFilter(idsToInclude, inverted: true));

    public static ElementFilter ThatExcludes(this ElementFilter self, params ElementId[] idsToExclude) =>
      self.Intersect(ExclusionFilter(idsToExclude, inverted: false));
    #endregion
  }
}

#if !REVIT_2021
namespace Autodesk.Revit.DB
{
  using RhinoInside.Revit.External.DB;
  using RhinoInside.Revit.External.DB.Extensions;

  abstract class ElementExternalFilter : IDisposable
  {
    protected static readonly FilterNumericRuleEvaluator NumericEqualsEvaluator = new FilterNumericEquals();
    protected static readonly ParameterValueProvider IdParamProvider = new ParameterValueProvider(new ElementId(BuiltInParameter.ID_PARAM));

    public virtual bool IsValidObject => true;
    public bool Inverted { get; protected set; }
    public virtual void Dispose() { }

    public abstract bool PassesFilter(Document document, ElementId id);
    public abstract bool PassesFilter(Element element);
  }

  class ElementIdSetFilter : ElementExternalFilter
  {
    readonly HashSet<ElementId> IdsToInclude;
    public ElementIdSetFilter(ICollection<ElementId> idsToInclude) => IdsToInclude = new HashSet<ElementId>(idsToInclude);
    public ICollection<ElementId> GetIdsToInclude() => IdsToInclude;

    public override bool PassesFilter(Document document, ElementId id) => IdsToInclude.Contains(id);
    public override bool PassesFilter(Element element) => IdsToInclude.Contains(element.Id);

    public static implicit operator ElementFilter(ElementIdSetFilter filter) => CompoundElementFilter.Union
    (
      filter.IdsToInclude.
      Select(x => new ElementParameterFilter(new FilterElementIdRule(IdParamProvider, NumericEqualsEvaluator, x))).
      ToArray()
    );
  }

  class VisibleInViewFilter : ElementExternalFilter
  {
    readonly Document Document;
    readonly ElementId ViewId;
    public VisibleInViewFilter(Document document, ElementId viewId)
    {
      Document = document;
      ViewId = viewId;
    }

    public VisibleInViewFilter(Document document, ElementId viewId, bool inverted) :
      this(document, viewId)
    {
      Inverted = inverted;
    }

    public override bool PassesFilter(Document document, ElementId id)
    {
      if (!document.IsEquivalent(Document)) return false;

      using (var collector = new FilteredElementCollector(Document, ViewId))
      {
        var visible = collector.WherePasses
        (
          new ElementIdSetFilter(new ElementId[] { id })
        );

        return (id == visible.FirstElementId()) != Inverted;
      }
    }

    public override bool PassesFilter(Element element)
    {
      if (!element.Document.IsEquivalent(Document)) return false;

      using (var collector = new FilteredElementCollector(Document, ViewId))
      {
        var visible = collector.WherePasses
        (
          new ElementIdSetFilter(new ElementId[] { element.Id })
        );

        return element.IsEquivalent(visible.FirstElement()) != Inverted;
      }
    }

    public static implicit operator ElementFilter(VisibleInViewFilter filter) => CompoundElementFilter.ExclusionFilter
    (
      new FilteredElementCollector(filter.Document, filter.ViewId).ToElementIds(),
      inverted: !filter.Inverted
    );
  }
}
#endif
