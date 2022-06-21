using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Options
{
  public class RevitVersion : Component
  {
    public override Guid ComponentGuid => new Guid("ACE507E5-2F94-4037-814B-FD9E94B6F87C");
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public RevitVersion() : base
    (
      "Revit Version", "Version",
      "Gets Revit version information",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("Name", "N", "Version name", GH_ParamAccess.item);
      manager.AddTextParameter("Version", "V", "Version number", GH_ParamAccess.item);
      manager.AddTextParameter("Build", "B", "Version build", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DA.SetData("Name", Revit.ActiveDBApplication.VersionName);
      DA.SetData("Version", Revit.ActiveDBApplication.VersionNumber);
      DA.SetData("Build", Revit.ActiveDBApplication.VersionBuild);
    }
  }
}
