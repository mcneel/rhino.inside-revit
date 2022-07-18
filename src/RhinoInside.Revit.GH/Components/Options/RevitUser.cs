using System;
using System.Globalization;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Options
{
  using External.ApplicationServices.Extensions;

  [ComponentVersion(introduced: "1.9")]
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
      manager.AddCultureParameter("Language", "L", "Language", GH_ParamAccess.item);
      manager.AddTextParameter("User Name", "UN", "User name for the current Revit session.", GH_ParamAccess.item);
      manager.AddTextParameter("User ID", "ID", "The user id of the user currently logged in.", GH_ParamAccess.item);
      manager.AddBooleanParameter("Logged In", "LI", "Gets if the user is logged in from this session to their Autodesk account.", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DA.SetData("Language", new CultureInfo(Revit.ActiveDBApplication.Language.ToLCID()));
      DA.SetData("User Name", Revit.ActiveDBApplication.Username);
      DA.SetData("User ID", Revit.ActiveDBApplication.LoginUserId);
      DA.SetData("Logged In", Autodesk.Revit.ApplicationServices.Application.IsLoggedIn);
    }
  }
}
