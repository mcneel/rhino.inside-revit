using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB
{
  /// <summary>
  /// Base class for an external Revit application
  /// </summary>
  public abstract class ExternalApplication : IExternalDBApplication
  {
    protected abstract ExternalDBApplicationResult OnShutdown(ControlledApplication application);
    ExternalDBApplicationResult IExternalDBApplication.OnShutdown(ControlledApplication application) =>
      ActivationGate.Open(() => OnShutdown(application), this);

    protected abstract ExternalDBApplicationResult OnStartup(ControlledApplication application);
    ExternalDBApplicationResult IExternalDBApplication.OnStartup(ControlledApplication application) =>
      ActivationGate.Open(() => OnStartup(application), this);
  }
}
