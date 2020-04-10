using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentIdentity : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("94BD655C-77DD-4A88-BDDB-B9456C45F06C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ID";
    protected override DB.ElementFilter ElementFilter => null;

    public DocumentIdentity() : base
    (
      "Document Identity", "Identity",
      "Query document identity information",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Title", "T", "Document title", GH_ParamAccess.item);
      manager.AddBooleanParameter("IsFamily", "F", "Identifies if the document is a family document", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Param_Enum<Types.UnitSystem>(),"UnitSystem", "U", "Identifies if the document units", GH_ParamAccess.item);
      //manager.AddParameter(new Parameters.Element(), "ProjectInformation", "I", "The Document ProjectInformation element", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.SetData("Title", doc.Title);
      DA.SetData("IsFamily", doc.IsFamilyDocument);
      DA.SetData("UnitSystem", (DB.UnitSystem) doc.DisplayUnitSystem);
      //DA.SetData("ProjectInformation", doc.ProjectInformation);
    }
  }

  public class DocumentFile : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("C1C15806-311A-4A07-9DAE-6DBD1D98EC05");
    public override GH_Exposure Exposure => GH_Exposure.obscure;
    protected override string IconTag => "F";
    protected override DB.ElementFilter ElementFilter => null;

    public DocumentFile() : base
    (
      "Document File", "File",
      string.Empty,
      "Revit", "Document"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "DocumentGUID", "DGUID", "A unique identifier for the document", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_FilePath(), "PathName", "PN", "The fully qualified path of the document's disk file", GH_ParamAccess.item);
      manager.AddBooleanParameter("ReadOnly", "RO", "Identifies if the document was opened from a read-only file", GH_ParamAccess.item);
      manager.AddBooleanParameter("Modified", "M", "Identifies if the document has been modified", GH_ParamAccess.item);
      manager.AddIntegerParameter("NumberOfSaves", "NOS", "The number of times the document has been saved", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "VersionGUID", "VGUID", "A unique identifier for the document version", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      var version = DB.Document.GetDocumentVersion(doc);

      DA.SetData("DocumentGUID", doc.GetFingerprintGUID());
      DA.SetData("PathName", doc.PathName);
      DA.SetData("ReadOnly", doc.IsReadOnlyFile);
      DA.SetData("Modified", doc.IsModified);
      DA.SetData("NumberOfSaves", version.NumberOfSaves);
      DA.SetData("VersionGUID", version.VersionGUID);
    }
  }

  public class DocumentWorksharing : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("F7D56DB0-F1C1-45BB-AA07-196039FFF862");
    public override GH_Exposure Exposure => GH_Exposure.obscure;
    protected override string IconTag => "⟳";
    protected override DB.ElementFilter ElementFilter => null;

    public DocumentWorksharing() : base
    (
      "Document Worksharing", "Worksharing",
      string.Empty,
      "Revit", "Document"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("IsWorkshared", "WS", "Identifies if worksharing have been enabled in the document", GH_ParamAccess.item);
      manager.AddTextParameter("ServerPath", "SP", "Central Server Path", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "CentralGUID", "WCGUID", "The central GUID of the server-based model", GH_ParamAccess.item);
      manager.AddBooleanParameter("Detached", "D", "Identifies if a workshared document is detached", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.SetData("IsWorkshared", doc.IsWorkshared);

      if (doc.IsWorkshared)
      {
        if (doc.GetWorksharingCentralModelPath() is DB.ModelPath worksharingPath && worksharingPath.ServerPath)
          DA.SetData("ServerPath", worksharingPath.CentralServerPath);

        try { DA.SetData("CentralGUID", doc.WorksharingCentralGUID); }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }

        try { DA.SetData("Detached", doc.IsDetached); }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      }
    }
  }


#if REVIT_2020
  public class DocumentCloud : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("2577A55B-A198-4760-9183-ADF8193FB5BD");
    public override GH_Exposure Exposure => GH_Exposure.obscure;
    protected override string IconTag => "☁";
    protected override DB.ElementFilter ElementFilter => null;

    public DocumentCloud() : base
    (
      "Document Cloud", "Cloud",
      string.Empty,
      "Revit", "Document"
    )
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBooleanParameter("IsInCloud", "C", "Identifies if document is stored on Autodesk cloud services", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "ProjectGUID", "PID", "The GUID identifies the Cloud project to which the model is associated", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "ModelGUID", "MID", "The GUID identifies this model in the Cloud project", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.SetData("IsInCloud", doc.IsModelInCloud);

      if (doc.IsModelInCloud)
      {
        if (doc.GetCloudModelPath() is DB.ModelPath cloudPath && cloudPath.CloudPath)
        {
          DA.SetData("ProjectGUID", cloudPath.GetProjectGUID());
          DA.SetData("ModelGUID", cloudPath.GetModelGUID());
        }
      }
    }
  }
#endif
}
