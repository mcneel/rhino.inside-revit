using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.10")]
  public class ViewDrafting : View<Types.ViewDrafting, ARDB.ViewDrafting>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("50979E85-0735-4FB2-9F4B-4ACB72F49344");
    protected override string IconTag => string.Empty;

    public ViewDrafting() : base("Drafting View", "Drafting View", "Contains a collection of Revit drafting views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.Drafting;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var AreaPlanId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.DraftingView);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(AreaPlanId),
        Revit.ActiveUIApplication.CanPostCommand(AreaPlanId)
      );
    }
    #endregion
  }
}
