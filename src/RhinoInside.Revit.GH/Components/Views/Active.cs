using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Views
{
  public class ViewActive : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("7CCF350C-80CC-42D0-85BA-78544FD59F4A");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "A";

    public ViewActive() : base
    (
      name: "Active Graphical View",
      nickname: "Active",
      description: "Gets the active graphical view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>("Active View", "V", "Active graphical view", GH_ParamAccess.item)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc))
        return;

      DA.SetData("Active View", doc.Value?.GetActiveGraphicalView());
    }

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
#if REVIT_2019
      var CloseInactiveViewsId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.CloseInactiveViews);
      Menu_AppendItem
      (
        menu, "Close Inactive Views…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, CloseInactiveViewsId),
        activeApp.CanPostCommand(CloseInactiveViewsId), false
      );
#endif
    }
    #endregion
  }
}
