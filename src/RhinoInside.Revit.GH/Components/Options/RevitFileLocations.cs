using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Options
{
  [ComponentVersion(introduced: "1.9")]
  public class RevitFileLocations : Component
  {
    public override Guid ComponentGuid => new Guid("CB3D697E-B227-4F69-8514-9EEB83C5016C");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public RevitFileLocations() : base
    (
      "Default File Locations", "File Locations",
      "Gets Revit default file locations",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Project Template", "PT", string.Empty, GH_ParamAccess.item);
      manager.AddTextParameter("Family Templates", "FT", string.Empty, GH_ParamAccess.item);
      manager.AddTextParameter("Point Clouds", "FT", string.Empty, GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DA.SetData("Project Template", Revit.ActiveDBApplication.DefaultProjectTemplate);
      DA.SetData("Family Templates", Revit.ActiveDBApplication.FamilyTemplatePath);
      DA.SetData("Point Clouds", Revit.ActiveDBApplication.PointCloudsRootPath);
    }
  }
}
