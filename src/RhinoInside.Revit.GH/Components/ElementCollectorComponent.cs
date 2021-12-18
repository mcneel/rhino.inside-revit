using System.Collections.Generic;
using System.Linq;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public abstract class ElementCollectorComponent : ZuiComponent
  {
    protected ElementCollectorComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory) { }

    protected virtual ARDB.ElementFilter ElementFilter { get; } = default;
    public override bool NeedsToBeExpired
    (
      ARDB.Document document,
      ICollection<ARDB.ElementId> added,
      ICollection<ARDB.ElementId> deleted,
      ICollection<ARDB.ElementId> modified
    )
    {
      var elementFilter = ElementFilter;
      var _Filter_ = Params.IndexOfInputParam("Filter");
      var filters = _Filter_ < 0 ?
                    Enumerable.Empty<ARDB.ElementFilter>() :
                    Params.Input[_Filter_].VolatileData.AllData(true).
                    OfType<Types.ElementFilter>().
                    Select(x => new ARDB.LogicalAndFilter(elementFilter, x.Value));

      foreach (var filter in filters.Any() ? filters : Enumerable.Repeat(elementFilter, 1))
      {
        if (added.Where(x => filter?.PassesFilter(document, x) ?? true).Any())
          return true;

        if (modified.Where(x => filter?.PassesFilter(document, x) ?? true).Any())
          return true;

        if (deleted.Count > 0)
        {
          var empty = new ARDB.ElementId[0];
          foreach (var param in Params.Output.OfType<Kernel.IGH_ElementIdParam>())
          {
            if (param.NeedsToBeExpired(document, empty, deleted, empty))
              return true;
          }
        }
      }

      return false;
    }

    protected static bool TryGetFilterIntegerParam(ARDB.BuiltInParameter paramId, int pattern, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterIntegerRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterDoubleParam(ARDB.BuiltInParameter paramId, double pattern, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterDoubleRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern,
        1e-6
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }

    protected static bool TryGetFilterDoubleParam(ARDB.BuiltInParameter paramId, double pattern, double tolerance, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterDoubleRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern,
        tolerance
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }

    protected internal static bool TryGetFilterStringParam(ARDB.BuiltInParameter paramId, ref string pattern, out ARDB.ElementFilter filter)
    {
      if (pattern is string subPattern)
      {
        var inverted = false;
        var method = Operator.CompareMethodFromPattern(ref subPattern, ref inverted);
        if (Operator.CompareMethod.Nothing < method && method < Operator.CompareMethod.Wildcard)
        {
          var evaluator = default(ARDB.FilterStringRuleEvaluator);
          switch (method)
          {
            case Operator.CompareMethod.Equals: evaluator = new ARDB.FilterStringEquals(); break;
            case Operator.CompareMethod.StartsWith: evaluator = new ARDB.FilterStringBeginsWith(); break;
            case Operator.CompareMethod.EndsWith: evaluator = new ARDB.FilterStringEndsWith(); break;
            case Operator.CompareMethod.Contains: evaluator = new ARDB.FilterStringContains(); break;
          }

          var rule = new ARDB.FilterStringRule
          (
            new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
            evaluator,
            subPattern,
            true
          );

          filter = new ARDB.ElementParameterFilter(rule, inverted);
          pattern = default;
          return true;
        }
      }

      filter = default;
      return false;
    }

    protected static bool TryGetFilterElementIdParam(ARDB.BuiltInParameter paramId, ARDB.ElementId pattern, out ARDB.ElementFilter filter)
    {
      var rule = new ARDB.FilterElementIdRule
      (
        new ARDB.ParameterValueProvider(new ARDB.ElementId(paramId)),
        new ARDB.FilterNumericEquals(),
        pattern
      );

      filter = new ARDB.ElementParameterFilter(rule, false);
      return true;
    }
  }
}
