using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ElementFilter : Param<Types.ElementFilter>
  {
    public override Guid ComponentGuid => new Guid("BFCFC49C-747E-40D9-AAEE-93CE06EAAF2B");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "Y";

    public ElementFilter() : base
    (
      name: "Element Filter",
      nickname: "Element Filter",
      description: "Contains a collection of Revit element filters",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }

  public class FilterRule : Param<Types.FilterRule>
  {
    public override Guid ComponentGuid => new Guid("F08E1292-F855-48C7-9921-BD12EF0F67D2");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override string IconTag => "R";

    public FilterRule() : base
    (
      name: "Filter Rule",
      nickname: "Filter Rule",
      description: "Contains a collection of Revit filter rules",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}
