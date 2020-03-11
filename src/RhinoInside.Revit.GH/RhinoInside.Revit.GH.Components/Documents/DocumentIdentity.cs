using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
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
}
