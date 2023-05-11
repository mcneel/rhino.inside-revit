using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class FilterElementExtension
  {
    public static ElementFilter ToElementFilter(this FilterElement filterElement)
    {
      switch (filterElement)
      {
        case null: return CompoundElementFilter.Empty;
        case ParameterFilterElement parameterFilterElement: return parameterFilterElement.ToElementFilter();
        case SelectionFilterElement selectionFilterElement: return selectionFilterElement.ToElementFilter();
        default: throw new System.NotImplementedException($"{nameof(ToElementFilter)} is not implemented for {filterElement.GetType()}");
      }
    }
  }

  public static class ParameterFilterElementExtension
  {
#if !REVIT_2019
    public static ElementFilter GetElementFilter(this ParameterFilterElement self)
    {
      var rules = self.GetRules();
      var filters = new System.Collections.Generic.List<ElementFilter>(rules.Count);

      foreach(var rule in rules)
        filters.Add(new ElementParameterFilter(rule));

      return new LogicalAndFilter(filters);
    }

    public static bool SetElementFilter(this ParameterFilterElement self, ElementFilter elementFilter)
    {
      throw new System.NotSupportedException("Parameter Filter is partially supported on Revit 2018.");
    }

    public static bool ElementFilterIsAcceptableForParameterFilterElement(this ParameterFilterElement self, ElementFilter elementFilter)
    {
      return false;
    }
#endif

    public static ElementFilter ToElementFilter(this ParameterFilterElement filterElement)
    {
      return CompoundElementFilter.ElementCategoryFilter(filterElement.GetCategories()).Intersect(filterElement.GetElementFilter());
    }
  }

  public static class SelectionFilterElementExtension
  {
    public static ElementFilter ToElementFilter(this SelectionFilterElement filterElement)
    {
      return CompoundElementFilter.ExclusionFilter(filterElement.GetElementIds(), inverted: true);
    }
  }
}
