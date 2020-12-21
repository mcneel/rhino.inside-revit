using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Parameters
{
  public abstract class Element<T, R> : ElementIdParam<T, R>
  where T : class, Types.IGH_ElementId
  {
    protected Element(string name, string nickname, string description, string category, string subcategory) :
      base(name, nickname, description, category, subcategory)
    { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Plural(ref List<T> values) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Singular(ref T value) => GH_GetterResult.cancel;
  }

  public class Element : Element<Types.IGH_Element, object>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary;
    public override Guid ComponentGuid => new Guid("F3EA4A9C-B24F-4587-A358-6A7E6D8C028B");

    public Element() : base("Element", "Element", "Represents a Revit document element.", "Params", "Revit Primitives") { }

    protected override Types.IGH_Element InstantiateT() => new Types.Element();
  }
}
