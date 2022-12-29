using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class OverrideGraphicSettings : Param<Types.OverrideGraphicSettings>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("3AE08A20-8360-4ECF-ACB0-5FD42A715545");
    public OverrideGraphicSettings() : base("Graphic Overrides", "Overrides", "Contains a collection of Revit graphic overrides", "Params", "Revit") { }
  }
}
