using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  using External.DB.Extensions;

  [ComponentVersion(introduced: "1.0", updated: "1.9")]
  public class DocumentIdentity : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("94BD655C-77DD-4A88-BDDB-B9456C45F06C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ID";

    public DocumentIdentity() : base
    (
      name: "Document Identity",
      nickname: "Identity",
      description: "Basic information about a document identity.",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Guid>("Document ID", "ID", "A unique identifier for the document"),
      ParamDefinition.Create<Param_String>("Model Path", "MP", "The document path"),
      ParamDefinition.Create<Param_String>("Name", "N", "Document name.\nLink type name used when document is linked.", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_String>("Title", "T", "Document title whithout user name nor extension.\nIn family documents same as Family Name.", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Is Family", "F", "Identifies if the document is a family document", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Param_Enum<Types.UnitSystem>>("Unit System", "US", "Document unit system", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      else Params.TrySetData(DA, "Document", () => doc);

      DA.SetData("Document ID", doc.DocumentId);
      Params.TrySetData(DA, "Model Path", () => doc.GetModelPathName());
      Params.TrySetData(DA, "Name", () => doc.Name);
      Params.TrySetData(DA, "Title", () => doc.Title);
      Params.TrySetData(DA, "Is Family", () => doc.IsFamilyDocument ?? false);
      Params.TrySetData(DA, "Unit System", () => doc.DisplayUnitSystem);
    }
  }

  [ComponentVersion(introduced: "1.0", updated: "1.9")]
  public class DocumentFile : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("C1C15806-311A-4A07-9DAE-6DBD1D98EC05");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "F";

    public DocumentFile() : base
    (
      name: "Document File",
      nickname: "File",
      description: "Basic information about a document local file.",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_Boolean>("Saved", "S", "Identifies if the document has been saved", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_FilePath>("Path", "P", "The fully qualified path of the document's local disk file"),
      ParamDefinition.Create<Param_String>("Name", "N", "The document's local file name", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_String>("Extension", "P", "The document's local file extension", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Number>("Size", "SZ", "Document's local file size (bytes)", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Read Only", "RO", "Identifies if the document was opened from a read-only file", relevance: ParamRelevance.Primary),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("PathName") is IGH_Param pathName)
        pathName.Name = "Path";

      if (Params.Output<IGH_Param>("ReadOnly") is IGH_Param readOnly)
        readOnly.Name = "Read Only";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      else Params.TrySetData(DA, "Document", () => doc);

      Params.TrySetData(DA, "Saved", () => doc.FilePath is object);
      Params.TrySetData(DA, "Path", () => doc.FilePath);
      Params.TrySetData(DA, "Name", () => doc.FileName);
      Params.TrySetData(DA, "Extension", () => doc.FileExtension);
      Params.TrySetData(DA, "Size", () =>
      {
        try
        {
          if (doc.FilePath is string path)
          {
            var info = new System.IO.FileInfo(path);
            if (info.Exists) return (double?) info.Length;
          }
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
        }

        return default;
      });
      Params.TrySetData(DA, "Read Only", () => doc.Value?.IsReadOnlyFile);
    }
  }

  [ComponentVersion(introduced: "1.0", updated: "1.9")]
  public class DocumentWorksharing : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("F7D56DB0-F1C1-45BB-AA07-196039FFF862");
    public override GH_Exposure Exposure => GH_Exposure.quarternary | GH_Exposure.obscure;
    protected override string IconTag => "⟳";

    public DocumentWorksharing() : base
    (
      name: "Document Worksharing",
      nickname: "Worksharing",
      description: "Worksharing information about a document.",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Param_Boolean>("Workshared", "WS", "Identifies if worksharing have been enabled in the document"),
      ParamDefinition.Create<Param_Boolean>("Detached", "D", "Identifies if document is a detached local copy"),
      ParamDefinition.Create<Param_Boolean>("Pending", "P", "Identifies if document has changes still not saved on the central file", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Boolean>("Central", "C", "Identifies if document is a central workshared model", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Guid>("Central Version", "CV", "This is the central model's episode GUID corresponding on the last reload.", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Integer>("Central Saves", "CS", "This is the central model's number of saves corresponding on the last reload.", relevance: ParamRelevance.Secondary),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("IsWorkshared") is IGH_Param isWorkshared)
        isWorkshared.Name = "Workshared";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc))
        return;

      DA.SetData("Workshared", doc.IsWorkshared);
      DA.SetData("Detached", doc.IsDetached);
      Params.TrySetData(DA, "Pending", () => doc.HasPendingChanges);
      Params.TrySetData(DA, "Central", () => doc.IsCentral);

      var version = doc.CentralVersion;
      Params.TrySetData(DA, "Central Version", () => version?.VersionGUID);
      Params.TrySetData(DA, "Central Saves", () => version?.NumberOfSaves);
    }
  }

  [ComponentVersion(introduced: "1.0", updated: "1.9")]
  public class DocumentCloud : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("2577A55B-A198-4760-9183-ADF8193FB5BD");
    public override GH_Exposure Exposure => GH_Exposure.quarternary;
    protected override string IconTag => "☁";

    public DocumentCloud() : base
    (
      name: "Document Server",
      nickname: "Server",
      description: "Document server information.",
      category: "Revit",
      subCategory: "Document"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Param_Boolean>("On Server", "S", "Identifies if document is stored on a document server"),
      ParamDefinition.Create<Param_Boolean>("On Cloud", "C", "Identifies if document is stored on Autodesk cloud services", relevance: ParamRelevance.Occasional),
      ParamDefinition.Create<Param_String>("Server Path", "SP", "Central Server Path", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Guid>("Project GUID", "PID", "The GUID identifies the project to which the model is associated"),
      ParamDefinition.Create<Param_Guid>("Model GUID", "MID", "The GUID identifies this model in the project"),
    };

    public override void AddedToDocument(GH_Document document)
    {
      if (Params.Output<IGH_Param>("IsInCloud") is IGH_Param isInCloud)
        isInCloud.Name = "On Cloud";

      if (Params.Output<IGH_Param>("ProjectGUID") is IGH_Param projecGUID)
        projecGUID.Name = "Project GUID";

      if (Params.Output<IGH_Param>("ModelGUID") is IGH_Param modelGUID)
        modelGUID.Name = "Model GUID";

      base.AddedToDocument(document);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;

      if (doc.IsWorkshared == true)
      {
        if (doc.GetModelPath() is ARDB.ModelPath modelPath)
        {
          DA.SetData("On Server", modelPath.ServerPath);

          if (modelPath.ServerPath)
          {
            Params.TrySetData(DA, "Server Path", () => modelPath.CentralServerPath);

#if REVIT_2019
            if (modelPath.CloudPath)
            {
              Params.TrySetData(DA, "On Cloud", () => true);

              DA.SetData("Project GUID", modelPath.GetProjectGUID());
              DA.SetData("Model GUID", modelPath.GetModelGUID());
            }
            else
#endif
            {
              Params.TrySetData(DA, "On Cloud", () => false);
              DA.SetData("Model GUID", doc.Value?.WorksharingCentralGUID);
            }
          }
        }
      }
    }
  }
}
