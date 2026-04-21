using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Views
{
  public class ActiveView : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("7CCF350C-80CC-42D0-85BA-78544FD59F4A");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "A";

    public ActiveView() : base
    (
      name: "Active View",
      nickname: "A-View",
      description: "Gets the active view",
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
      ParamDefinition.Create<Parameters.View>("Active View", "V", "Active view", GH_ParamAccess.item)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDocumentOrCurrent(this, DA, out var doc))
        return;

      DA.SetData("Active View", doc.Value?.GetActiveGraphicalView());
    }

    #region UI
    private class ButtonAttributes : ExpireButtonAttributes
    {
      public ButtonAttributes(ZuiComponent owner) : base(owner) { }
      protected override string DisplayText => "Query";
      protected override bool Visible => Owner.Params.Input.Count == 0;
    }

    public override void CreateAttributes() => m_attributes = new ButtonAttributes(this);

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
#if REVIT_2019
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.CloseInactiveViews, "Close Inactive Views…");
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.TabViews, "Tab Views…");
      menu.AppendPostableCommand(Autodesk.Revit.UI.PostableCommand.TileViews, "Tile Views…");
#endif
    }
    #endregion
  }
}
