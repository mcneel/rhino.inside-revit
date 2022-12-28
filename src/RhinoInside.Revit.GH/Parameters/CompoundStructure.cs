using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class CompoundStructure : Param<Types.CompoundStructure>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("4762C0A4-4902-48D7-AAA4-A344A79238AB");
    public CompoundStructure() : base("Compound Structure", "CStructure", "Contains a collection of Revit compound structures", "Params", "Revit") { }
  }

  public class CompoundStructureLayer : Param<Types.CompoundStructureLayer>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("A5172644-3F23-4F6F-82DF-0F61D46BA6B9");
    public CompoundStructureLayer() : base("Compound Structure Layer", "CSLayer", "Contains a collection of Revit compound structure layers", "Params", "Revit") { }
  }

  public class OverrideGraphicSettings : Param<Types.OverrideGraphicSettings>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("3AE08A20-8360-4ECF-ACB0-5FD42A715545");
    public OverrideGraphicSettings() : base("Graphic Settings", "GSettings", "Contains a collection of Revit graphic settings", "Params", "Revit") { }
  }
}
