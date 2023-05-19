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
  public class SpaceElement : GraphicalElement<Types.SpaceElement, ARDB.Mechanical.Space>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("30473B1D-6226-45CE-90A7-5F8E1E1DCBE3");

    public SpaceElement() : base
    (
      name: "Space",
      nickname: "Space",
      description: "Contains a collection of Revit space elements",
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
      var SpaceId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Space);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(SpaceId),
        Revit.ActiveUIApplication.CanPostCommand(SpaceId),
        false
      );
    }
    #endregion
  }
}
