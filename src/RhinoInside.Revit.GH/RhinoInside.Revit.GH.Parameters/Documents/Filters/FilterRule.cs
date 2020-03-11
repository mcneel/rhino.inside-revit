using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters.Documents.Filters
{
  public class FilterRule : GH_Param<Types.Documents.Filters.FilterRule>
  {
    public override Guid ComponentGuid => new Guid("F08E1292-F855-48C7-9921-BD12EF0F67D2");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                                     ImageBuilder.BuildIcon("R");

    public FilterRule() : base("FilterRule", "FilterRule", "Represents a Revit filter rule.", "Params", "Revit", GH_ParamAccess.item) { }
  }
}
