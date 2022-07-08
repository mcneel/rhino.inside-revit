using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.7")]
  public class View3D : View<Types.View3D, ARDB.View3D>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("F3C35FB2-B43B-4D80-9F9C-AEE26ACC7649");
    protected override string IconTag => "3D";

    public View3D() : base("3D View", "3D View", "Contains a collection of Revit 3D views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.ThreeDimensional;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      base.Menu_AppendPromptNew(menu);

      var activeApp = Revit.ActiveUIApplication;
      {
        var exist = activeApp.ActiveUIDocument.Document.GetDefault3DView() is object;
        var commandId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Default3DView);
        Menu_AppendItem
        (
          menu, "Create Default 3D View…", Menu_PromptNew(commandId),
          !exist && activeApp.CanPostCommand(commandId), false
        );
      }
    }
    #endregion
  }
}
