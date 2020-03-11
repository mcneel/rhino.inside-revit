using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters.Documents
{
  class Document : GH_PersistentParam<Types.Documents.Document>
  {
    public override Guid ComponentGuid => new Guid("F3427D5C-3793-4E32-B219-8172D56EF04C");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override Types.Documents.Document PreferredCast(object data) => data is DB.Document doc ? new Types.Documents.Document(doc) : null;

    public Document() : base("Document", "Document", string.Empty, "Params", "Revit")
    { }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Singular(ref Types.Documents.Document value) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Documents.Document> values) => GH_GetterResult.cancel;
  }
}
