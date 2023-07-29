using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.16")]
  public class ViewSchedule : View<Types.ViewSchedule, ARDB.ViewSchedule>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("1F65705E-4E32-473F-81BF-F7536BB62FF3");
    protected override string IconTag => string.Empty;

    public ViewSchedule() : base("Schedule", "Schedule", "Contains a collection of Revit schedule views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.Schedule;

    protected override bool PassFilter(ARDB.ViewSchedule view) => base.PassFilter(view) && !view.IsTitleblockRevisionSchedule;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      Menu_AppendPromptNew(menu, Autodesk.Revit.UI.PostableCommand.ScheduleOrQuantities);
    }
    #endregion
  }
}
