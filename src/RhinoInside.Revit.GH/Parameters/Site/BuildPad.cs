using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class BuildingPad : GraphicalElement<Types.BuildingPad, ARDB.Architecture.BuildingPad>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("0D0AFE5F-4578-493E-8374-C6BD1C5395BE");

    public BuildingPad() : base("Building Pad", "Building Pad", "Contains a collection of Revit building pad elements", "Params", "Revit Elements") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var BuildingPad = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.BuildingPad);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(BuildingPad),
        Revit.ActiveUIApplication.CanPostCommand(BuildingPad),
        false
      );
    }
    #endregion
  }
}
