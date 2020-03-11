using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
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
    }
  }
}
