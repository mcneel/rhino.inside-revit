using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.12")]
  public class Annotation : GraphicalElement<Types.IGH_Annotation, ARDB.Element>
  {
    public override GH_Exposure Exposure => GH_Exposure.quinary | GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("35D2829E-1F8E-494D-8D93-A4D2A7351729");
    protected override string IconTag => string.Empty;

    public Annotation() : base
    (
      name: "Annotation",
      nickname: "Annotation",
      description: "Contains a collection of Revit annotative elements",
      category: "Params",
      subcategory: "Revit Elements"
    )
    { }

    #region UI
    //protected override IEnumerable<string> ConvertsTo => base.ConvertsTo.Concat
    //(
    //  new string[] { "View", }
    //);

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override void Menu_AppendManageCollection(ToolStripDropDown menu) { }
    #endregion
  }
}
