using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Revision : Element<Types.Revision, ARDB.Revision>
  {
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("82A7462C-67EC-43EB-A520-F35E2360DC43");

    public Revision() : base
    (
      name: "Revision",
      nickname: "Revision",
      description: "Contains a collection of Revit revision elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;

      var SheetIssuesOrRevisionsId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.SheetIssuesOrRevisions);
      Menu_AppendItem
      (
        menu, "Open Sheet Issues/Revisions…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, SheetIssuesOrRevisionsId),
        activeApp.CanPostCommand(SheetIssuesOrRevisionsId), false
      );
    }
    #endregion
  }
}
