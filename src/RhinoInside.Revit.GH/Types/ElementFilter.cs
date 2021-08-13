using System;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Selection Filter")]
  public class SelectionFilterElement : Element
  {
    protected override Type ScriptVariableType => typeof(DB.SelectionFilterElement);
    public new DB.SelectionFilterElement Value => base.Value as DB.SelectionFilterElement;

    public SelectionFilterElement() { }
    public SelectionFilterElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public SelectionFilterElement(DB.SelectionFilterElement value) : base(value) { }
  }

  [Kernel.Attributes.Name("Parameter Filter")]
  public class ParameterFilterElement : Element
  {
    protected override Type ScriptVariableType => typeof(DB.ParameterFilterElement);
    public new DB.ParameterFilterElement Value => base.Value as DB.ParameterFilterElement;

    public ParameterFilterElement() { }
    public ParameterFilterElement(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public ParameterFilterElement(DB.ParameterFilterElement value) : base(value) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo<Q>(out target))
        return true;

      if (Value is DB.ParameterFilterElement value)
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

  public class ElementFilter : GH_Goo<DB.ElementFilter>
  {
    public override string TypeName => "Revit Element Filter";
    public override string TypeDescription => "Represents a Revit element filter";
    public override bool IsValid => Value?.IsValidObject ?? false;
    public sealed override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

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
    public sealed override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

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
