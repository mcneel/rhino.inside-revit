using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.GH.Kernel.Attributes;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentSave : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("FBB2E4A2-CC2A-470E-B7E8-CB3320166CC5");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "S";

    public DocumentSave() : base
    (
      name: "Save Document",
      nickname: "Save",
      description: "Saves a document to a given file path",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(new Parameters.Document(), ParamVisibility.Voluntary),
      ParamDefinition.FromParam
      (
        new Param_FilePath()
        {
          Name = "Path",
          NickName = "P",
          Access = GH_ParamAccess.item,
          Optional = true,
          FileFilter = "Project File (*.rvt)|*.rvt"
        }
      ),
      ParamDefinition.Create<Param_Boolean>("Override File", "OF", "Override file on disk", defaultValue: false, GH_ParamAccess.item),
      ParamDefinition.Create<Param_Boolean>("Compact", "C", "Compact the file", defaultValue: false, GH_ParamAccess.item),
      ParamDefinition.Create<Param_Integer>("Backups", "B", "The maximum number of backups to keep on disk", -1, GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.View>("View", "View", "The view that will be used to generate the file preview", GH_ParamAccess.item, optional: true)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Document>("Document", "DOC", string.Empty, GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var filePath = string.Empty;
      DA.GetData("Path", ref filePath);

      var overrideFile = false;
      if (!DA.GetData("Override File", ref overrideFile))
        return;

      var compact = false;
      if (!DA.GetData("Compact", ref compact))
        return;

      var backups = -1;
      if (!DA.GetData("Backups", ref backups))
        return;

      var view = default(DB.View);
      if (DA.GetData("View", ref view))
      {
        if (!view.Document.Equals(doc))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"View '{view.Title}' is not a valid view in document {doc.Title}");
          return;
        }
      }

      try
      {
        Guest.Instance.CommitTransactionGroups();

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
      finally
      {
        Guest.Instance.StartTransactionGroups();
      }
    }
  }
}
