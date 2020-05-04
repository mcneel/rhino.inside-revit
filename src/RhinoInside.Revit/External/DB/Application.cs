using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

namespace RhinoInside.Revit.External.DB
{
  public abstract class Application : IExternalDBApplication
  {
    protected abstract ExternalDBApplicationResult OnShutdown(ControlledApplication application);
    ExternalDBApplicationResult IExternalDBApplication.OnShutdown(ControlledApplication application) =>
      ActivationGate.Open(() => OnShutdown(application), this);

    protected abstract ExternalDBApplicationResult OnStartup(ControlledApplication application);
    ExternalDBApplicationResult IExternalDBApplication.OnStartup(ControlledApplication application) =>
      ActivationGate.Open(() => OnStartup(application), this);
  }
}
