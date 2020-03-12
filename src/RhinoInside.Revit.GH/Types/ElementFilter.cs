using Grasshopper.Kernel.Types;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class ElementFilter : GH_Goo<DB.ElementFilter>
  {
    public override string TypeName => "Revit Element Filter";
    public override string TypeDescription => "Represents a Revit element filter";
    public override bool IsValid => Value?.IsValidObject ?? false;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public ElementFilter() { }
    public ElementFilter(DB.ElementFilter filter) : base(filter) { }

    public override bool CastFrom(object source)
    {
      if (source is DB.ElementFilter filter)
      {
        Value = filter;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.ElementFilter)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override string ToString()
    {
      if (!IsValid)
        return $"Invalid {TypeName}";

      return $"{Value.GetType().Name}";
    }
  }

  public class FilterRule : GH_Goo<DB.FilterRule>
  {
    public override string TypeName => "Revit Filter Rule";
    public override string TypeDescription => "Represents a Revit filter rule";
    public override bool IsValid => Value?.IsValidObject ?? false;
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public FilterRule() { }
    public FilterRule(DB.FilterRule filter) : base(filter) { }

    public override bool CastFrom(object source)
    {
      if (source is DB.FilterRule rule)
      {
        Value = rule;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(DB.FilterRule)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo<Q>(ref target);
    }

    public override string ToString()
    {
      if (!IsValid)
        return $"Invalid {TypeName}";

      return $"{Value.GetType().Name}";
    }
  }
}
