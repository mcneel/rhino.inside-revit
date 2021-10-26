using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(since: "1.2")]
  public class Workset : PersistentParam<Types.Workset>
  {
    public override Guid ComponentGuid => new Guid("5C073F7D-6D31-4063-A943-4152E1A799D1");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    public Workset() : base("Workset", "Workset", "Contains a collection of Revit workset elements", "Params", "Revit Primitives") { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var commandId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Worksets);
      Menu_AppendItem
      (
        menu, $"Open Worksets…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, commandId),
        activeApp.CanPostCommand(commandId), false
      );
    }

    protected override GH_GetterResult Prompt_Singular(ref Types.Workset value)
    {
      return GH_GetterResult.cancel;
    }

    protected override GH_GetterResult Prompt_Plural(ref List<Types.Workset> values)
    {
      return GH_GetterResult.cancel;
    }
    #endregion
  }
}
