using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Components.Views
{
  [ComponentVersion(introduced: "1.18")]
  public class ViewOpen : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("E13FC388-D607-4A62-BEBC-498A9445F91A");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => string.Empty;

    public ViewOpen() : base
    (
      name: "Open View",
      nickname: "V-Open",
      description: "Open-Close a Revit view",
      category: "Revit",
      subCategory: "View"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", string.Empty),
      ParamDefinition.Create<Param_Boolean>("Open", "O", string.Empty, optional: true, relevance: ParamRelevance.Primary)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", string.Empty),
      ParamDefinition.Create<Param_Boolean>("Open", "O", string.Empty, optional: true, relevance: ParamRelevance.Primary)
    };

    private readonly Dictionary<Types.View, bool> ViewStates = new Dictionary<Types.View, bool>();
    protected override void BeforeSolveInstance()
    {
      base.BeforeSolveInstance();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.TryGetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      else Params.TrySetData(DA, "View", () => view);
      if (!Params.TryGetData(DA, "Open", out bool? open)) return;

      var isOpen = view.Value.IsOpen();
      if (open is object)
      {
        if (view.Value.IsTemplate && open is true)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Can't open view template '{view.DisplayName}'");
        else if (view.Value.IsCallout() && open is true)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Can't open callout view'{view.DisplayName}'");
        else
          ViewStates[view] = open.Value;
      }

      Params.TrySetData(DA, "Open", () => isOpen);
    }

    protected override void AfterSolveInstance()
    {
      if (ViewStates.Count > 0)
      {
        try
        {
          Guest.Instance.CommitTransactionGroups();
          var activeView = Revit.ActiveUIDocument.ActiveView;

          try
          {
            foreach (var group in ViewStates.GroupBy(x => x.Key.Document))
            {
              using (var uiDocument = new ARUI.UIDocument(group.Key))
              {
                var openViews = uiDocument.GetOpenUIViews();
                if (openViews.Count == 0)
                {
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Document {uiDocument.Document.GetTitle()} is not open on UI.");
                }
                else
                {
                  var openViewIds = new HashSet<ARDB.ElementId>(openViews.Select(x => x.ViewId));
                  var viewsToClose = group.Where(x => x.Value == false).Select(x => x.Key);
                  var viewsToOpen = group.Where(x => x.Value == true).Select(x => x.Key);

                  foreach (var view in viewsToOpen)
                  {
                    if (!openViewIds.Contains(view.Id))
                      view.Value.Document.SetActiveView(view.Value);
                  }

                  foreach (var view in viewsToClose)
                  {
                    if (view.Value.IsEquivalent(activeView))
                    {
                      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Can't close '{view.DisplayName}' because is the active one.");
                    }
                    else
                    {
                      try { view.Value.Close(); }
                      catch (Exception e) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message); }
                    }
                  }
                }
              }
            }
          }
          finally
          {
            activeView.Document.SetActiveView(activeView);
          }

          // Reconstruct output 'Open' with final values from 'View'.
          var _View_ = Params.IndexOfOutputParam("View");
          var _Open_ = Params.IndexOfOutputParam("Open");
          if (_View_ >= 0 && _Open_ >= 0)
          {
            var viewParam = Params.Output[_View_];
            var openParam = Params.Output[_Open_];
            var openData = new GH_Structure<GH_Boolean>();

            var viewData = viewParam.VolatileData;
            foreach (var path in viewData.Paths)
            {
              var open = viewData.get_Branch(path).Cast<object>().Select(x => (x as Types.View)?.Value.IsOpen());
              openData.AppendRange(open.Select(x => x.HasValue ? new GH_Boolean(x.Value) : null), path);
            }

            openParam.ClearData();
            openParam.AddVolatileDataTree(openData);
          }
        }
        finally
        {
          ViewStates.Clear();
          Guest.Instance.StartTransactionGroups();
        }
      }

      base.AfterSolveInstance();
    }

    #region UI
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalComponentMenuItems(menu);

      var activeApp = Revit.ActiveUIApplication;
#if REVIT_2019
      menu.AppendPostableCommand(ARUI.PostableCommand.CloseInactiveViews, "Close Inactive Views…");
      menu.AppendPostableCommand(ARUI.PostableCommand.TabViews, "Tab Views…");
      menu.AppendPostableCommand(ARUI.PostableCommand.TileViews, "Tile Views…");
#endif
    }
    #endregion
  }
}
