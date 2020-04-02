using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

using DB = Autodesk.Revit.DB;


namespace RhinoInside.Revit.GH.Components.Element.Wall
{
  public class WallStructuralUsage_ValueList : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("1F3053C0-BD22-49F9-BE93-635E0BD24864");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    public WallStructuralUsage_ValueList()
    {
      Category = "Revit";
      SubCategory = "Input";
      Name = "Wall Structural Usage";
      NickName = "WSU";
      Description = "Picker for builtin Wall structural usage options";

      ListItems.Clear();
      ListItems.Add(
        new GH_ValueListItem("Non-Bearing", ((int) DB.Structure.StructuralWallUsage.NonBearing).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Bearing", ((int) DB.Structure.StructuralWallUsage.Bearing).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Shear", ((int) DB.Structure.StructuralWallUsage.Shear).ToString())
        );
      ListItems.Add(
        new GH_ValueListItem("Combined", ((int) DB.Structure.StructuralWallUsage.Combined).ToString())
        );
    }
  }
}
