using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  public class PointCloudInstance : GraphicalElement<Types.PointCloudInstance, ARDB.PointCloudInstance>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("4326C4AA-3D84-46E1-B3CE-829CF74A664B");
    protected override string IconTag => "☁";

    public PointCloudInstance() : base
    (
      name: "Point Cloud",
      nickname: "Point Cloud",
      description: "Contains a collection of Revit point cloud elements",
      category: "Params",
      subcategory: "Revit"
    )
    { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var PointCloudId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.PointCloud);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(PointCloudId),
        Revit.ActiveUIApplication.CanPostCommand(PointCloudId),
        false
      );
    }
    #endregion
  }

  public class PointCloudFilter : Param<Types.PointCloudFilter>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("F64607F0-0351-4869-9459-620E58FBDFAA");
    protected override string IconTag => string.Empty;

    public PointCloudFilter() : base
    (
      name: "Point Cloud Filter",
      nickname: "Cloud Filter",
      description: "Contains a collection of Revit point cloud filters",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}
