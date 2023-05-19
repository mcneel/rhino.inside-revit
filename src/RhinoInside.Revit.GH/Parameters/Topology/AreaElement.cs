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
  public class AreaElement : GraphicalElement<Types.AreaElement, ARDB.Area>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("66AAAE96-BA85-4DC7-A188-AC213FAD3176");

    public AreaElement() : base
    (
      name: "Area",
      nickname: "Area",
      description: "Contains a collection of Revit area elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Append("Surface");

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var AreaId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Area);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(AreaId),
        Revit.ActiveUIApplication.CanPostCommand(AreaId),
        false
      );
    }
    #endregion
  }
}
