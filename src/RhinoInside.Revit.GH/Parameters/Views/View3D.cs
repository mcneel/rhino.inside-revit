using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.7")]
  public class View3D : View<Types.View3D, ARDB.View3D>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("F3C35FB2-B43B-4D80-9F9C-AEE26ACC7649");
    protected override string IconTag => "3D";

    public View3D() : base("3D View", "3D View", "Contains a collection of Revit 3D views", "Params", "Revit") { }

    #region UI
    public override void Menu_AppendActions(ToolStripDropDown menu)
    {
      base.Menu_AppendActions(menu);

      var activeApp = Revit.ActiveUIApplication;
      {
        var commandId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Default3DView);
        Menu_AppendItem
        (
          menu, "Default 3D View…",
          (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
          activeApp.CanPostCommand(commandId), false
        );
      }
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu)
    {
      if (SourceCount != 0) return;
      if (Revit.ActiveUIDocument?.Document is null) return;

      Menu_AppendPromptNew(menu);

      var listBox = new ListBox
      {
        Sorted = true,
        BorderStyle = BorderStyle.FixedSingle,
        Width = (int) (200 * GH_GraphicsUtil.UiScale),
        Height = (int) (100 * GH_GraphicsUtil.UiScale)
      };
      listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

      RefreshViewsList(listBox, ARDB.ViewFamily.ThreeDimensional);

      Menu_AppendCustomItem(menu, listBox);
    }
    #endregion
  }
}
