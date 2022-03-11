using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Group : GraphicalElementT<Types.Group, ARDB.Group>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("2674B9FF-E463-426B-8A8C-CCB5A7F4C84E");

    public Group() : base("Group", "Group", "Contains a collection of Revit group elements", "Params", "Revit Primitives") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var CreateGroup = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.CreateGroup);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(CreateGroup),
        Revit.ActiveUIApplication.CanPostCommand(CreateGroup),
        false
      );
    }
    #endregion
  }
}
