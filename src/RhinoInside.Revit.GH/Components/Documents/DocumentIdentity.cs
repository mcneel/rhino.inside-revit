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
      ParamDefinition.Create<Param_String>("Title", "T", "Document title", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_String>("Name", "N", "Document name whithout extension"),
      ParamDefinition.Create<Param_Boolean>("Is Family", "F", "Identifies if the document is a family document", relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Parameters.Param_Enum<Types.UnitSystem>>("Unit System", "US", "Document unit system", relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.TryGetDocumentOrCurrent(this, DA, "Document", out var doc)) return;
      else Params.TrySetData(DA, "Document", () => doc);

      DA.SetData("Document ID", doc.DocumentGUID);
      Params.TrySetData(DA, "Model Path", () => doc.ModelPath);
      Params.TrySetData(DA, "Title", () => doc.Title);
      Params.TrySetData(DA, "Name", () => doc.Name);
      Params.TrySetData(DA, "Is Family", () => doc.Value.IsFamilyDocument);
      Params.TrySetData(DA, "Unit System", () => (ARDB.UnitSystem) doc.Value.DisplayUnitSystem);
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
      description: "Basic information about a document file.",
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
      ParamDefinition.Create<Param_FilePath>("Path", "P", "The fully qualified path of the document's disk file"),
      ParamDefinition.Create<Param_String>("Name", "N", "The document's file name", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_String>("Extension", "P", "The document's file extension", relevance: ParamRelevance.Secondary),
      ParamDefinition.Create<Param_Number>("Size", "S", "Document's file size (bytes)", relevance: ParamRelevance.Primary),
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
      ParamDefinition.Create<Param_Boolean>("IsWorkshared", "WS", "Identifies if worksharing have been enabled in the document", GH_ParamAccess.item),
      ParamDefinition.Create<Param_String>("ServerPath", "SP", "Central Server Path", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Guid>("CentralGUID", "CID", "The central GUID of the server-based model", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Boolean>("Detached", "D", "Identifies if a workshared document is detached", GH_ParamAccess.item),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      DA.SetData("IsWorkshared", doc.IsWorkshared);

      if (doc.IsWorkshared)
      {
        if (doc.GetWorksharingCentralModelPath() is ARDB.ModelPath worksharingPath && worksharingPath.ServerPath)
          DA.SetData("ServerPath", worksharingPath.CentralServerPath);

        try { DA.SetData("CentralGUID", doc.WorksharingCentralGUID); }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }

        try { DA.SetData("Detached", doc.IsDetached); }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      }
    }
  }

  public class DocumentCloud : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("2577A55B-A198-4760-9183-ADF8193FB5BD");
#if REVIT_2019
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
#else
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure | GH_Exposure.hidden;
    public override bool SDKCompliancy(int exeVersion, int exeServiceRelease) => false;
#endif
    protected override string IconTag => "☁";

    public DocumentCloud() : base
    (
      name: "Document Cloud",
      nickname: "Cloud",
      description: "Cloud information about a document.",
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
      ParamDefinition.Create<Param_Boolean>("IsInCloud", "C", "Identifies if document is stored on Autodesk cloud services", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Guid>("ProjectGUID", "PID", "The GUID identifies the Cloud project to which the model is associated", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Guid>("ModelGUID", "MID", "The GUID identifies this model in the Cloud project", GH_ParamAccess.item),
    };

#if !REVIT_2019
    protected override void BeforeSolveInstance()
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"'{Name}' component is only supported on Revit 2019 or above.");
      base.BeforeSolveInstance();
    }
#endif

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;

#if REVIT_2019
      DA.SetData("IsInCloud", doc.IsModelInCloud);

      if (doc.IsModelInCloud)
      {
        if (doc.GetCloudModelPath() is ARDB.ModelPath cloudPath && cloudPath.CloudPath)
        {
          DA.SetData("ProjectGUID", cloudPath.GetProjectGUID());
          DA.SetData("ModelGUID", cloudPath.GetModelGUID());
        }
      }
#endif
    }
  }
}
