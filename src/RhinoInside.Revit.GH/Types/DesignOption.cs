using System;
using System.Collections.Generic;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Design Option Set")]
  public class DesignOptionSet : Element
  {
    protected override bool SetValue(DB.Element element) =>
      IsValidElement(element) && base.SetValue(element);

    public static bool IsValidElement(DB.Element element) => IsValidElementFilter.PassesFilter(element);

    internal static readonly DB.ElementFilter IsValidElementFilter = new DB.LogicalAndFilter
    (
      new DB.ElementCategoryFilter(DB.BuiltInCategory.OST_DesignOptionSets),
#if REVIT_2022
      new DB.ElementParameterFilter(new DB.HasValueFilterRule(new DB.ElementId(DB.BuiltInParameter.OPTION_SET_NAME)))
#else
      new DB.ElementParameterFilter
      (
        new DB.FilterStringRule
        (
          new DB.ParameterValueProvider(new DB.ElementId(DB.BuiltInParameter.OPTION_SET_NAME)),
          new DB.FilterStringEquals(), string.Empty, true
        ),
        inverted: true
      )
#endif
    );

    public DesignOptionSet() { }
    public DesignOptionSet(DB.Element value) : base(value) { }
    public DesignOptionSet(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public IList<DesignOption> Options
    {
      get
      {
        if (Value is DB.Element set)
          return set.GetDependents<DB.DesignOption>().ConvertAll(x => new DesignOption(x));

        return default;
      }
    }
  }

  [Kernel.Attributes.Name("Design Option")]
  public class DesignOption : Element
  {
    protected override Type ScriptVariableType => typeof(DB.DesignOption);
    public new DB.DesignOption Value => base.Value as DB.DesignOption;

    public override string DisplayName
    {
      get
      {
        if (Value is DB.DesignOption option)
        {
          var set = Document.GetElement(option.get_Parameter(DB.BuiltInParameter.OPTION_SET_ID).AsElementId());
          return $"{set.Name}\\{Name}";
        }
        else if (Document is null && Id == DB.ElementId.InvalidElementId)
        {
          return "Main Model";
        }

        return base.DisplayName;
      }
    }

    public DesignOption() { }
    public DesignOption(DB.DesignOption value) : base(value) { }
    public DesignOption(DB.Document doc, DB.ElementId id) : base(doc, id) { }

    public override string Name
    {
      get
      {
        if (Value is DB.DesignOption option)
          return option.get_Parameter(DB.BuiltInParameter.OPTION_NAME).AsString();
        else if (Document is null && Id == DB.ElementId.InvalidElementId)
          return "Main Model";

        return default;
      }
      set
      {
        if (value is object && Name != value)
        {
          if (Value is DB.DesignOption option)
            option.get_Parameter(DB.BuiltInParameter.OPTION_NAME).Set(value);
          else if (Document is null && Id == DB.ElementId.InvalidElementId)
            throw new InvalidOperationException($"Design option 'Main Model' does not support assignment of a user-specified name.");
        }
      }
    }

    public bool? IsPrimary
    {
      get
      {
        if (Value is DB.DesignOption option)
          return option.IsPrimary;
        else if (Document is null && Id == DB.ElementId.InvalidElementId)
          return true;

        return default;
      }
    }

    public DesignOptionSet OptionSet
    {
      get
      {
        if (Value is DB.DesignOption option)
          return new DesignOptionSet(Document, option.get_Parameter(DB.BuiltInParameter.OPTION_SET_ID).AsElementId());
        else if (Document is null && Id == DB.ElementId.InvalidElementId)
          return new DesignOptionSet();

        return default;
      }
    }
  }
}
