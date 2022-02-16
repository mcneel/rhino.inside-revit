using System;
using System.Collections.Generic;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Design Option Set")]
  public class DesignOptionSet : Element
  {
    protected override bool SetValue(ARDB.Element element) =>
      IsValidElement(element) && base.SetValue(element);

    public static bool IsValidElement(ARDB.Element element) => IsValidElementFilter.PassesFilter(element);

    internal static readonly ARDB.ElementFilter IsValidElementFilter = new ARDB.LogicalAndFilter
    (
      new ARDB.ElementCategoryFilter(ARDB.BuiltInCategory.OST_DesignOptionSets),
#if REVIT_2021
      new ARDB.ElementParameterFilter(new ARDB.HasValueFilterRule(new ARDB.ElementId(ARDB.BuiltInParameter.OPTION_SET_NAME)))
#else
      new ARDB.ElementParameterFilter
      (
        new ARDB.FilterStringRule
        (
          new ARDB.ParameterValueProvider(new ARDB.ElementId(ARDB.BuiltInParameter.OPTION_SET_NAME)),
          new ARDB.FilterStringEquals(), string.Empty, true
        ),
        inverted: true
      )
#endif
    );

    public DesignOptionSet() { }
    public DesignOptionSet(ARDB.Element value) : base(value) { }
    public DesignOptionSet(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

    public IList<DesignOption> Options
    {
      get
      {
        if (Value is ARDB.Element set)
          return set.GetDependents<ARDB.DesignOption>().ConvertAll(x => new DesignOption(x));

        return default;
      }
    }
  }

  [Kernel.Attributes.Name("Design Option")]
  public class DesignOption : Element
  {
    protected override Type ValueType => typeof(ARDB.DesignOption);
    public new ARDB.DesignOption Value => base.Value as ARDB.DesignOption;

    public override string DisplayName
    {
      get
      {
        if (Value is ARDB.DesignOption option)
        {
          var set = Document.GetElement(option.get_Parameter(ARDB.BuiltInParameter.OPTION_SET_ID).AsElementId());
          return $"{set.Name}\\{Nomen}";
        }
        else if (Document is null && Id == ARDB.ElementId.InvalidElementId)
        {
          return "Main Model";
        }

        return base.DisplayName;
      }
    }

    public DesignOption() { }
    public DesignOption(ARDB.DesignOption value) : base(value) { }
    public DesignOption(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

    public override string Nomen
    {
      get
      {
        if (Document is null && Id == ARDB.ElementId.InvalidElementId)
          return "Main Model";

        return base.Nomen;
      }
      set
      {
        if (value is object)
        {
          if (Document is null && Id == ARDB.ElementId.InvalidElementId && value != "Main Model")
            throw new InvalidOperationException("Design option 'Main Model' does not support assignment of a user-specified name.");

          base.Nomen = Nomen;
        }
      }
    }

    public bool? IsPrimary
    {
      get
      {
        if (Value is ARDB.DesignOption option)
          return option.IsPrimary;
        else if (Document is null && Id == ARDB.ElementId.InvalidElementId)
          return true;

        return default;
      }
    }

    public DesignOptionSet OptionSet
    {
      get
      {
        if (Value is ARDB.DesignOption option)
          return new DesignOptionSet(Document, option.get_Parameter(ARDB.BuiltInParameter.OPTION_SET_ID).AsElementId());
        else if (Document is null && Id == ARDB.ElementId.InvalidElementId)
          return new DesignOptionSet();

        return default;
      }
    }
  }
}
