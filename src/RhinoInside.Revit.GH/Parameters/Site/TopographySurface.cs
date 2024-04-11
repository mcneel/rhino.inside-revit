using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class TopographySurface : GraphicalElement<Types.TopographySurface, ARDB.Architecture.TopographySurface>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("0700BE5F-9B9C-4235-ADD5-787E42898114");

    public TopographySurface() : base
    (
      name: "Topography",
      nickname: "Topography",
      description: "Contains a collection of Revit topography elements",
      category: "Params",
      subcategory: "Revit"
    )
    { }

    #region UI
    protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    (
      new string[] { "Mesh", }
    );

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var create = Menu_AppendItem(menu, $"Set new {TypeName}");

#if !REVIT_2024
      Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.Toposurface, "Toposurface");
      Menu_AppendPromptNew(create.DropDown, Autodesk.Revit.UI.PostableCommand.Subregion, "Region");
#endif
    }
    #endregion
  }
}
