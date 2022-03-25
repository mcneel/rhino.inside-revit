using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.7")]
  public class SectionView : View<Types.SectionView, ARDB.ViewSection>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("0744D339-3693-4849-936A-20E061E62B58");

    public SectionView() : base("Section", "Section", "Contains a collection of Revit section views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.Section;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var SectionId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.Section);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(SectionId),
        Revit.ActiveUIApplication.CanPostCommand(SectionId)
      );
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class ElevationView : View<Types.ElevationView, ARDB.ViewSection>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("6EFFB4B8-3517-463E-8583-A51269ABC4FD");

    public ElevationView() : base("Elevation", "Elevation", "Contains a collection of Revit elevation views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.Elevation;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var SectionId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.BuildingElevation);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(SectionId),
        Revit.ActiveUIApplication.CanPostCommand(SectionId)
      );
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class DetailView : View<Types.DetailView, ARDB.ViewSection>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("CA537732-BE4B-41EC-A2D3-49210D8CCCF6");

    public DetailView() : base("Detail", "Detail", "Contains a collection of Revit detail views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.Detail;
    #endregion
  }
}
