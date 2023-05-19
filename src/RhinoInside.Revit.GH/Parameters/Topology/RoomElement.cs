using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.7")]
  public class RoomElement : GraphicalElement<Types.RoomElement, ARDB.Architecture.Room>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("1E6825B6-4A7A-44EA-BC70-A9A110963E17");

    public RoomElement() : base
    (
      name: "Room",
      nickname: "Room",
      description: "Contains a collection of Revit room elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Surface", "Brep" }
    );

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var RoomId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Room);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(RoomId),
        Revit.ActiveUIApplication.CanPostCommand(RoomId),
        false
      );
    }
    #endregion
  }
}
