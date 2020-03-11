using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters.Documents.Filters
{
  public class ElementFilter : GH_Param<Types.Documents.Filters.ElementFilter>
  {
    public override Guid ComponentGuid => new Guid("BFCFC49C-747E-40D9-AAEE-93CE06EAAF2B");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override System.Drawing.Bitmap Icon => ((System.Drawing.Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name)) ??
                                                     ImageBuilder.BuildIcon("Y");

    public ElementFilter() : base("ElementFilter", "ElementFilter", "Represents a Revit element filter.", "Params", "Revit", GH_ParamAccess.item) { }
  }
}
