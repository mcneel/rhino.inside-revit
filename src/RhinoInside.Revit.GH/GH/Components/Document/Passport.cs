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
      "Document.Identity", "Identity",
      "Query document identity information",
      "Revit", "Document"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Title", "Title", "Document title", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Element(), "ProjectInformation", "Information", "The Document ProjectInformation element", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.SetData("Title", doc.Title);
      DA.SetData("ProjectInformation", doc.ProjectInformation);
    }
  }

  public class DocumentPassport : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("C1C15806-311A-4A07-9DAE-6DBD1D98EC05");
    public override GH_Exposure Exposure => GH_Exposure.obscure;
    protected override string IconTag => "PASS";
    protected override DB.ElementFilter ElementFilter => null;

    public DocumentPassport() : base
    (
      "Document.Passport", "Passport",
      string.Empty,
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      base.RegisterInputParams(manager);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_FilePath(), "PathName", "Path", "The fully qualified path of the document's disk file", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "GUID", "GUID", "A unique identifier for the document", GH_ParamAccess.item);

      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "WorksharingCentralGUID", "WorksharingCentralGUID", "The central GUID of the server-based model", GH_ParamAccess.item);

      manager.AddTextParameter("CloudServerPath", "CloudServerPath", "Cloud Server Path", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "CloudProjectGUID", "CloudProjectGUID", "The GUID identifies the Cloud project to which the model is associated", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "CloudModelGUID", "CloudModelGUID", "The GUID identifies this model in the Cloud project", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      DA.SetData("PathName", doc.PathName);
      DA.SetData("GUID", doc.GetFingerprintGUID());

      if (doc.IsWorkshared)
      {
        try { DA.SetData("WorksharingCentralGUID", doc.WorksharingCentralGUID); }
        catch (Autodesk.Revit.Exceptions.InapplicableDataException) { }
      }

      if(doc.IsModelInCloud && doc.GetCloudModelPath() is DB.ModelPath cloudPath)
      {
        DA.SetData("CloudServerPath", cloudPath.ServerPath);
        DA.SetData("CloudProjectGUID", cloudPath.GetProjectGUID());
        DA.SetData("CloudModelGUID", cloudPath.GetModelGUID());
      }
    }
  }
}
