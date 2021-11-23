using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
  using External.DB.Extensions;

  public class DocumentIdentity : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("94BD655C-77DD-4A88-BDDB-B9456C45F06C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ID";

    public DocumentIdentity() : base
    (
      name: "Document Identity",
      nickname: "Identity",
      description: "Query document identity information",
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
      ParamDefinition.Create<Param_String>("Title", "T", "Document title", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Boolean>("Is Family", "F", "Identifies if the document is a family document", GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.Param_Enum<Types.UnitSystem>>("Unit System", "U", "Identifies if the document units", GH_ParamAccess.item),
      //ParamDefinition.Create<Parameters.Element>("Project Information", "I", "The Document ProjectInformation element", GH_ParamAccess.item)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      DA.SetData("Title", doc.Title);
      DA.SetData("Is Family", doc.IsFamilyDocument);
      DA.SetData("Unit System", (ARDB.UnitSystem) doc.DisplayUnitSystem);
      //DA.SetData("Project Information", doc.ProjectInformation);
    }
  }

  public class DocumentFile : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("C1C15806-311A-4A07-9DAE-6DBD1D98EC05");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "F";

    public DocumentFile() : base
    (
      name: "Document File",
      nickname: "File",
      description: string.Empty,
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
      ParamDefinition.Create<Param_Guid>("DocumentGUID", "DGUID", "A unique identifier for the document", GH_ParamAccess.item),
      ParamDefinition.Create<Param_FilePath>("PathName", "PN", "The fully qualified path of the document's disk file", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Boolean>("ReadOnly", "RO", "Identifies if the document was opened from a read-only file", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Boolean>("Modified", "M", "Identifies if the document has been modified", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Integer>("NumberOfSaves", "NOS", "The number of times the document has been saved", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Guid>("VersionGUID", "VGUID", "A unique identifier for the document version", GH_ParamAccess.item),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      var version = ARDB.Document.GetDocumentVersion(doc);

      DA.SetData("DocumentGUID", doc.GetFingerprintGUID());
      DA.SetData("PathName", doc.PathName);
      DA.SetData("ReadOnly", doc.IsReadOnlyFile);
      DA.SetData("Modified", doc.IsModified);
      DA.SetData("NumberOfSaves", version.NumberOfSaves);
      DA.SetData("VersionGUID", version.VersionGUID);
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
      description: string.Empty,
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
      ParamDefinition.Create<Param_Guid>("CentralGUID", "WCGUID", "The central GUID of the server-based model", GH_ParamAccess.item),
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


#if REVIT_2020
  public class DocumentCloud : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("2577A55B-A198-4760-9183-ADF8193FB5BD");
    public override GH_Exposure Exposure => GH_Exposure.obscure;
    protected override string IconTag => "☁";

    public DocumentCloud() : base
    (
      name: "Document Cloud",
      nickname: "Cloud",
      description: string.Empty,
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

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      DA.SetData("IsInCloud", doc.IsModelInCloud);

      if (doc.IsModelInCloud)
      {
        if (doc.GetCloudModelPath() is ARDB.ModelPath cloudPath && cloudPath.CloudPath)
        {
          DA.SetData("ProjectGUID", cloudPath.GetProjectGUID());
          DA.SetData("ModelGUID", cloudPath.GetModelGUID());
        }
      }
    }
  }
#endif
}
