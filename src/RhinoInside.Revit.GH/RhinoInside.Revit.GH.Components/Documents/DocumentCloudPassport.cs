using System;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Documents
{
#if REVIT_2020
  public class DocumentCloudPassport : DocumentComponent
  {
    public override Guid ComponentGuid => new Guid("2577A55B-A198-4760-9183-ADF8193FB5BD");
    public override GH_Exposure Exposure => GH_Exposure.obscure;
    protected override string IconTag => "CLOUD";
    protected override DB.ElementFilter ElementFilter => null;

    public DocumentCloudPassport() : base
    (
      "Document.CloudPassport", "CloudPassport",
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
      manager.AddTextParameter("ServerPath", "ServerPath", "Cloud Server Path", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "ProjectGUID", "ProjectGUID", "The GUID identifies the Cloud project to which the model is associated", GH_ParamAccess.item);
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "ModelGUID", "ModelGUID", "The GUID identifies this model in the Cloud project", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      if (doc.IsModelInCloud && doc.GetCloudModelPath() is DB.ModelPath cloudPath)
      {
        DA.SetData("ServerPath", cloudPath.ServerPath);
        DA.SetData("ProjectGUID", cloudPath.GetProjectGUID());
        DA.SetData("ModelGUID", cloudPath.GetModelGUID());
      }
    }
  }
#endif
}
