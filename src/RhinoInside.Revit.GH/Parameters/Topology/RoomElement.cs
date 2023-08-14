using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

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
      Menu_AppendPromptNew(menu, Autodesk.Revit.UI.PostableCommand.Room);
    }
    #endregion
  }
}
