using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Sketch : ElementIdWithPreviewParam<Types.Sketch, Autodesk.Revit.DB.Sketch>
  {
    public override Guid ComponentGuid => new Guid("2B0684C4-A444-406D-8BEC-69683D146388");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;

    public Sketch() : base("Sketch", "Sketch", "Represents a Revit document sketch.", "Params", "Revit Primitives") { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Sketch> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref Types.Sketch value) => GH_GetterResult.cancel;
  }
}
