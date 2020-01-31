using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ElementFilter : GH_Param<Types.ElementFilter>
  {
    public override Guid ComponentGuid => new Guid("BFCFC49C-747E-40D9-AAEE-93CE06EAAF2B");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                                     ImageBuilder.BuildIcon("Y");

    public ElementFilter() : base("ElementFilter", "ElementFilter", "Represents a Revit element filter.", "Params", "Revit", GH_ParamAccess.item) { }
  }

  public class FilterRule : GH_Param<Types.FilterRule>
  {
    public override Guid ComponentGuid => new Guid("F08E1292-F855-48C7-9921-BD12EF0F67D2");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                                     ImageBuilder.BuildIcon("R");

    public FilterRule() : base("FilterRule", "FilterRule", "Represents a Revit filter rule.", "Params", "Revit", GH_ParamAccess.item) { }
  }
}
