using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.6")]
  public class Opening : GraphicalElement<Types.Opening, ARDB.Opening>
  {
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("18D46E90-46BA-47DF-B11B-AE78748BBDA7");

    public Opening() : base("Opening", "Opening", "Contains a collection of Revit opening elements", "Params", "Revit Elements") { }

    #region UI
    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

      //Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.Opening, "Opening");
      Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.OpeningByFace, "Opening by Face");
      Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.ShaftOpening, "Shaft Opening");
      Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.WallOpening, "Wall Opening");
      Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.VerticalOpening, "Vertical Opening");
      Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.DormerOpening, "Dormer Opening");
    }
    #endregion
  }
}
