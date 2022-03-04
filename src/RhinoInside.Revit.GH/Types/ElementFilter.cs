using System;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Selection Filter")]
  public class SelectionFilterElement : Element
  {
    protected override Type ValueType => typeof(ARDB.SelectionFilterElement);
    public new ARDB.SelectionFilterElement Value => base.Value as ARDB.SelectionFilterElement;

    public SelectionFilterElement() { }
    public SelectionFilterElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public SelectionFilterElement(ARDB.SelectionFilterElement value) : base(value) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (Value is ARDB.SelectionFilterElement value)
      {
        if (typeof(Q).IsAssignableFrom(typeof(ElementFilter)))
        {
          target = (Q) (object) new ElementFilter(CompoundElementFilter.ExclusionFilter(value.GetElementIds(), inverted: true));
          return true;
        }
      }

      return false;
    }
  }

  [Kernel.Attributes.Name("Parameter Filter")]
  public class ParameterFilterElement : Element
  {
    protected override Type ValueType => typeof(ARDB.ParameterFilterElement);
    public new ARDB.ParameterFilterElement Value => base.Value as ARDB.ParameterFilterElement;

    public ParameterFilterElement() { }
    public ParameterFilterElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ParameterFilterElement(ARDB.ParameterFilterElement value) : base(value) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (Value is ARDB.ParameterFilterElement value)
      {
        if (typeof(Q).IsAssignableFrom(typeof(ElementFilter)))
        {
          target = (Q) (object) new ElementFilter(value.GetElementFilter());
          return true;
        }
      }

      return false;
    }
  }

  public class ElementFilter : GH_Goo<ARDB.ElementFilter>
  {
    public override string TypeName => "Revit Element Filter";
    public override string TypeDescription => "Represents a Revit element filter";
    public override bool IsValid => Value?.IsValidObject ?? false;
    public sealed override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public ElementFilter() { }
    public ElementFilter(ARDB.ElementFilter filter) : base(filter) { }

    public override bool CastFrom(object source)
    {
      if (source is ARDB.ElementFilter filter)
      {
        Value = filter;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.ElementFilter)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo(ref target);
    }

    public override string ToString()
    {
      if (!IsValid)         return $"Invalid {TypeName}";
      if (Value.IsEmpty())  return "<empty>";
      if (Value.IsAll())    return "<all>";

      return Value.GetType().Name;
    }
  }

  public class FilterRule : GH_Goo<ARDB.FilterRule>
  {
    public override string TypeName => "Revit Filter Rule";
    public override string TypeDescription => "Represents a Revit filter rule";
    public override bool IsValid => Value?.IsValidObject ?? false;
    public sealed override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public FilterRule() { }
    public FilterRule(ARDB.FilterRule filter) : base(filter) { }

    public override bool CastFrom(object source)
    {
      if (source is ARDB.FilterRule rule)
      {
        Value = rule;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.FilterRule)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo(ref target);
    }

    public override string ToString()
    {
      if (!IsValid)
        return $"Invalid {TypeName}";

      return $"{Value.GetType().Name}";
    }
  }
}
