using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class Document : PersistentParam<Types.Document>
  {
    public override Guid ComponentGuid => new Guid("F3427D5C-3793-4E32-B219-8172D56EF04C");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override Types.Document PreferredCast(object data) => Types.Document.FromDocument(data as DB.Document);

    public Document() : base("Document", "DOC", "Contains a collection of Revit documents", "Params", "Revit Primitives")
    { }

    [Flags]
    public enum DataGrouping
    {
      None = 0,
      Document = 1,
    };

    public DataGrouping Grouping { get; set; } = DataGrouping.None;

    public sealed override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader))
        return false;

      int grouping = (int) DataGrouping.None;
      reader.TryGetInt32("Grouping", ref grouping);
      Grouping = (DataGrouping) grouping;

      return true;
    }
    public sealed override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer))
        return false;

      if (Grouping != DataGrouping.None)
        writer.SetInt32("Grouping", (int) Grouping);

      return true;
    }

    #region UI
    protected override void Menu_AppendPreProcessParameter(ToolStripDropDown menu)
    {
      base.Menu_AppendPreProcessParameter(menu);

      Menu_AppendItem(menu, "Group", Menu_Group, true, (Grouping & DataGrouping.Document) != 0);
    }

    private void Menu_Group(object sender, EventArgs e)
    {
      RecordUndoEvent("Set: Grouping");
      if (Grouping == DataGrouping.Document)
        Grouping = DataGrouping.None;
      else
        Grouping = DataGrouping.Document;

      OnObjectChanged(GH_ObjectEventType.Options);

      if (Kind == GH_ParamKind.output)
        ExpireOwner();

      ExpireSolution(true);
    }

    protected override void Menu_AppendPromptOne(ToolStripDropDown menu) { }
    protected override void Menu_AppendPromptMore(ToolStripDropDown menu) { }
    protected override GH_GetterResult Prompt_Singular(ref Types.Document value) => GH_GetterResult.cancel;
    protected override GH_GetterResult Prompt_Plural(ref List<Types.Document> values) => GH_GetterResult.cancel;
    #endregion

    protected override void ProcessVolatileData()
    {
      if (Grouping == DataGrouping.Document)
      {
        if (Kind == GH_ParamKind.floating)
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Grouped by Document");

        var data = new GH_Structure<Types.Document>();
        var pathCount = m_data.PathCount;
        for (int p = 0; p < pathCount; ++p)
        {
          var path = m_data.Paths[p];
          var branch = m_data.get_Branch(path);
          foreach (var item in branch)
          {
            if (item is Types.Document doc)
            {
              var id = DocumentExtension.DocumentSessionId(doc.Value.GetFingerprintGUID());
              data.Append(doc, path.AppendElement(id));
            }
            else
              data.Append(null, path.AppendElement(0));
          }
        }

        m_data = data;
      }

      base.ProcessVolatileData();
    }

    public static bool GetDataOrDefault(IGH_Component component, IGH_DataAccess DA, string name, out DB.Document document)
    {
      document = default;
      var _Document_ = name is null ? -1 : component.Params.IndexOfInputParam(name);
      if (_Document_ < 0)
      {
        document = Revit.ActiveDBDocument;
        if (document?.IsValidObject != true)
        {
          if (component.GetTopLevelObject() is IGH_ActiveObject active)
            active.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There is no current Revit document");

          return false;
        }

        // In case the user has more than one document open we show which one this component is working on
        if (Revit.ActiveDBApplication.Documents.Size > 1)
        {
          if (component.GetTopLevelObject() is IGH_ActiveObject active)
            active.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Using document '{document.GetTitle()}'");
        }
      }
      else
      {
        if (!DA.GetData(_Document_, ref document) || document is null)
          return false;

        if (document.IsValidObject != true)
        {
          component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input parameter Document failed to collect data");
          return false;
        }
      }

      return true;
    }
  }
}
