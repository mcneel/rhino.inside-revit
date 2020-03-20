using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentSave : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("FBB2E4A2-CC2A-470E-B7E8-CB3320166CC5");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "S";

    public DocumentSave() : base
    (
      "Save Document", "Save",
      "Saves a document to a given file path",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);

      var path = new Grasshopper.Kernel.Parameters.Param_FilePath();
      path.FileFilter = "Project File (*.rvt)|*.rvt";
      manager[manager.AddParameter(path, "Path", "P", string.Empty, GH_ParamAccess.item)].Optional = true;

      manager.AddBooleanParameter("OverrideFile", "O", "Override file on disk", GH_ParamAccess.item, false);
      manager.AddBooleanParameter("Compact", "O", "Compact the file", GH_ParamAccess.item, false);
      manager.AddIntegerParameter("Backups", "B", "The maximum number of backups to keep on disk", GH_ParamAccess.item, -1);
      manager[manager.AddParameter(new Parameters.View(), "PreviewView", "PreviewView", "The view that will be used to generate the file preview", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.Document(), "Document", "Document", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var filePath = string.Empty;
      DA.GetData("Path", ref filePath);

      var overrideFile = false;
      if (!DA.GetData("OverrideFile", ref overrideFile))
        return;

      var compact = false;
      if (!DA.GetData("Compact", ref compact))
        return;

      var backups = -1;
      if (!DA.GetData("Backups", ref backups))
        return;

      var view = default(DB.View);
      if (DA.GetData("PreviewView", ref view))
      {
        if (!view.Document.Equals(doc))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"View '{view.Title}' is not a valid view in document {doc.Title}");
          return;
        }
      }

      if (string.IsNullOrEmpty(filePath))
      {
        if (overrideFile)
        {
          using (var saveOptions = new DB.SaveOptions() { Compact = compact })
          {
            if (view is object)
              saveOptions.PreviewViewId = view.Id;

            doc.Save(saveOptions);
          }

          DA.SetData("Document", doc);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to collect data from 'Path'.");
      }
      else
      {
        if (filePath.Last() == Path.DirectorySeparatorChar)
          filePath = Path.Combine(filePath, doc.Title);

        if (Path.IsPathRooted(filePath) && filePath.Contains(Path.DirectorySeparatorChar))
        {
          if (!Path.HasExtension(filePath))
          {
            if (doc.IsFamilyDocument)
              filePath += ".rfa";
            else
              filePath += ".rvt";
          }

          using (var saveAsOptions = new DB.SaveAsOptions() { OverwriteExistingFile = overrideFile, Compact = compact })
          {
            if (backups > -1)
              saveAsOptions.MaximumBackups = backups;

            if (view is object)
              saveAsOptions.PreviewViewId = view.Id;

            doc.SaveAs(filePath, saveAsOptions);
            DA.SetData("Document", doc);
          }
        }
        else
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path should be absolute.");
        }
      }
    }
  }
}
