using System;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  [ComponentVersion(introduced: "1.0", updated: "1.13")]
  public class DocumentSave : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("FBB2E4A2-CC2A-470E-B7E8-CB3320166CC5");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
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
      new ParamDefinition(new Parameters.Document()),
      new ParamDefinition
      (
        new Param_FilePath()
        {
          Name = "Path",
          NickName = "P",
          Optional = true,
          FileFilter = "Project File (*.rvt)|*.rvt"
        }, ParamRelevance.Primary
      ),
      ParamDefinition.Create<Param_Boolean>("Overwrite", "O", "Overwrite file on disk", defaultValue: false),
      ParamDefinition.Create<Param_Boolean>("Compact", "C", "Compact the file", defaultValue: false, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Integer>("Backups", "B", "The maximum number of backups to keep on disk", -1, relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Parameters.View>("View", "View", "The view that will be used to generate the file preview", optional: true, relevance: ParamRelevance.Occasional)
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Path", "P", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Written", "W", "Wheter or not file is being written to disk", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.TryGetData(DA, "Path", out string filePath)) return;
      if (!Params.TryGetData(DA, "Overwrite", out bool? overwrite)) return;
      if (!Params.TryGetData(DA, "Compact", out bool? compact)) return;
      if (!Params.TryGetData(DA, "Backups", out int? backups)) return;
      if (Params.TryGetData(DA, "View", out Types.View view) && view?.Document.IsEquivalent(doc) is false)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"View '{view.Value.Title}' is not a valid view in document {doc.Title}");
        return;
      }

      try
      {
        Guest.Instance.CommitTransactionGroups();

        if (string.IsNullOrEmpty(filePath))
        {
          var wasSaved = !string.IsNullOrEmpty(doc.PathName);
          if (overwrite is true && wasSaved)
          {
            using (var saveOptions = new ARDB.SaveOptions() { Compact = compact is true })
            {
              if (view is object)
                saveOptions.PreviewViewId = view.Id;

              doc.Save(saveOptions);
            }

            Params.TrySetData(DA, "Document", () => doc);
            Params.TrySetData(DA, "Path", () => doc.PathName);
            Params.TrySetData(DA, "Written", () => true);
          }
          else
          {
            if (!wasSaved && overwrite is true)
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Document '{doc.GetName()}' is never being saved before, please specify a valid Path.");
            else
            {
              Params.TrySetData(DA, "Document", () => doc);
              Params.TrySetData(DA, "Path", () => wasSaved ? doc.PathName : null);
              Params.TrySetData(DA, "Written", () => false);
            }
          }
        }
        else
        {
          if (filePath.Last() == Path.DirectorySeparatorChar)
            filePath = Path.Combine(filePath, doc.Title);

          if (filePath.IsFullyQualifiedPath())
          {
            if (!Path.HasExtension(filePath))
            {
              if (doc.IsFamilyDocument) filePath += ".rfa";
              else                      filePath += ".rvt";
            }

            var exist = File.Exists(filePath);
            if (overwrite is true || !exist)
            {
              using (var saveAsOptions = new ARDB.SaveAsOptions() { OverwriteExistingFile = overwrite is true, Compact = compact is true })
              {
                if (backups > -1)
                  saveAsOptions.MaximumBackups = backups.Value;

                if (view is object)
                  saveAsOptions.PreviewViewId = view.Id;

                doc.SaveAs(filePath, saveAsOptions);
                Params.TrySetData(DA, "Document", () => doc);
                Params.TrySetData(DA, "Path", () => filePath);
                Params.TrySetData(DA, "Written", () => true);
              }
            }
            else
            {
              Params.TrySetData(DA, "Document", () => doc);
              Params.TrySetData(DA, "Path", () => filePath);
              Params.TrySetData(DA, "Written", () => false);
            }
          }
          else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path should be absolute.");
        }
      }
      finally
      {
        Guest.Instance.StartTransactionGroups();
      }
    }
  }
}
