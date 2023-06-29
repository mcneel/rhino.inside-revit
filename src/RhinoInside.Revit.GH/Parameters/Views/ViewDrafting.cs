using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.10")]
  public class ViewDrafting : View<Types.ViewDrafting, ARDB.ViewDrafting>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("50979E85-0735-4FB2-9F4B-4ACB72F49344");
    protected override string IconTag => string.Empty;

    public ViewDrafting() : base("Drafting View", "Drafting View", "Contains a collection of Revit drafting views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.Drafting;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      Menu_AppendPromptNew(menu, Autodesk.Revit.UI.PostableCommand.DraftingView);
    }
    #endregion
  }
}
