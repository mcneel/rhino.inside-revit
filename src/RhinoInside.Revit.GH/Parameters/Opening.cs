using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.6")]
  public class Opening : GraphicalElementT<Types.Opening, ARDB.Opening>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("18D46E90-46BA-47DF-B11B-AE78748BBDA7");

    public Opening() : base("Opening", "Opening", "Contains a collection of Revit opening elements", "Params", "Revit Elements") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

      //var OpeningId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Opening);
      //Menu_AppendItem
      //(
      //  create.DropDown, "Opening",
      //  Menu_PromptNew(OpeningId),
      //  Revit.ActiveUIApplication.CanPostCommand(OpeningId),
      //  false
      //);

      var OpeningByFaceId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.OpeningByFace);
      Menu_AppendItem
      (
        create.DropDown, "Opening by Face",
        Menu_PromptNew(OpeningByFaceId),
        Revit.ActiveUIApplication.CanPostCommand(OpeningByFaceId),
        false
      );

      var ShaftOpeningId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.ShaftOpening);
      Menu_AppendItem
      (
        create.DropDown, "Shaft Opening",
        Menu_PromptNew(ShaftOpeningId),
        Revit.ActiveUIApplication.CanPostCommand(ShaftOpeningId),
        false
      );

      var WallOpeningId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.WallOpening);
      Menu_AppendItem
      (
        create.DropDown, "Wall Opening",
        Menu_PromptNew(WallOpeningId),
        Revit.ActiveUIApplication.CanPostCommand(WallOpeningId),
        false
      );

      var VerticalOpeningId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.VerticalOpening);
      Menu_AppendItem
      (
        create.DropDown, "Vertical Opening",
        Menu_PromptNew(VerticalOpeningId),
        Revit.ActiveUIApplication.CanPostCommand(VerticalOpeningId),
        false
      );

      var DormerOpeningId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.DormerOpening);
      Menu_AppendItem
      (
        create.DropDown, "Dormer Opening",
        Menu_PromptNew(DormerOpeningId),
        Revit.ActiveUIApplication.CanPostCommand(DormerOpeningId),
        false
      );
    }
    #endregion
  }
}
