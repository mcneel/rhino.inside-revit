using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace RhinoInside.Revit.GH.Parameters.Input
{
  public class CurtainMullionSystemFamily_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("4BBE14F2-F46B-4D2E-9E44-51E20AA32E59");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public CurtainMullionSystemFamily_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Curtain Mullion System Family";
      NickName = "CMSF";
      Description = "Picker for curtain mullion system family types";

      ListItems.Clear();

      ListItems.Add(new GH_ValueListItem("Unknown", ((int) External.DB.CurtainMullionSystemFamily.Unknown).ToString()));
      ListItems.Add(new GH_ValueListItem("Rectangular", ((int) External.DB.CurtainMullionSystemFamily.Rectangular).ToString()));
      ListItems.Add(new GH_ValueListItem("Circular", ((int) External.DB.CurtainMullionSystemFamily.Circular).ToString()));
      ListItems.Add(new GH_ValueListItem("L Corner", ((int) External.DB.CurtainMullionSystemFamily.LCorner).ToString()));
      ListItems.Add(new GH_ValueListItem("Trapezoid Corner", ((int) External.DB.CurtainMullionSystemFamily.TrapezoidCorner).ToString()));
      ListItems.Add(new GH_ValueListItem("Quad Corner", ((int) External.DB.CurtainMullionSystemFamily.QuadCorner).ToString()));
      ListItems.Add(new GH_ValueListItem("V Corner", ((int) External.DB.CurtainMullionSystemFamily.VCorner).ToString()));
    }
  }
}
