using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.0")]
  public class Default3DView : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F2277265-8845-403B-83A9-EF670FA036C8");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => "3D";

    public Default3DView() : base
    (
      name: "Default 3D View",
      nickname: "3D View",
      description: "Gets the default 3D view",
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
      ParamDefinition.Create<Parameters.View3D>("3D View", "V", "Default 3D view")
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc))
        return;

      var view = doc.Value.GetDefault3DView();
      DA.SetData("3D View", view);
    }

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
      var Default3DViewId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.Default3DView);
      Menu_AppendItem
      (
        menu, "Open Default 3D View…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, Default3DViewId, () => ExpireSolution(true)),
        activeApp.CanPostCommand(Default3DViewId), false
      );

#if REVIT_2019
      var CloseInactiveViewsId = Autodesk.Revit.UI.RevitCommandId.LookupPostableCommandId(Autodesk.Revit.UI.PostableCommand.CloseInactiveViews);
      Menu_AppendItem
      (
        menu, "Close Inactive Views…",
        (sender, arg) => External.UI.EditScope.PostCommand(activeApp, CloseInactiveViewsId),
        activeApp.CanPostCommand(CloseInactiveViewsId), false
      );
    }
#endif
    #endregion
  }
}
