using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using DBX = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Parameters
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

      ListItems.Add(new GH_ValueListItem("Unknown", ((int) DBX.CurtainMullionSystemFamily.Unknown).ToString()));
      ListItems.Add(new GH_ValueListItem("Rectangular", ((int) DBX.CurtainMullionSystemFamily.Rectangular).ToString()));
      ListItems.Add(new GH_ValueListItem("Circular", ((int) DBX.CurtainMullionSystemFamily.Circular).ToString()));
      ListItems.Add(new GH_ValueListItem("L Corner", ((int) DBX.CurtainMullionSystemFamily.LCorner).ToString()));
      ListItems.Add(new GH_ValueListItem("Trapezoid Corner", ((int) DBX.CurtainMullionSystemFamily.TrapezoidCorner).ToString()));
      ListItems.Add(new GH_ValueListItem("Quad Corner", ((int) DBX.CurtainMullionSystemFamily.QuadCorner).ToString()));
      ListItems.Add(new GH_ValueListItem("V Corner", ((int) DBX.CurtainMullionSystemFamily.VCorner).ToString()));
    }
  }
}
