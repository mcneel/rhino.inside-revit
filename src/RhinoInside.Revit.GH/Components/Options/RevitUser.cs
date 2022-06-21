using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components.Options
{
  public class RevitUser : Component
  {
    public override Guid ComponentGuid => new Guid("4BFEB1EE-FE14-430A-96EA-EAC5CD7A7382");
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    protected override string IconTag => string.Empty;

    public RevitUser() : base
    (
      "Revit User", "User",
      "Gets Revit user information",
      "Revit", "Document"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddTextParameter("User Name", "UN", "User name for the current Revit session.", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DA.SetData("User Name", Revit.ActiveDBApplication.Username);
    }
  }
}
