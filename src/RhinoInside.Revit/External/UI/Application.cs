using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI
{
  /// <summary>
  /// Base class for an external Revit application
  /// </summary>
  public abstract class ExternalApplication : AddInId, IExternalApplication
  {
    protected ExternalApplication(Guid addInId) : base(addInId) { }

    protected abstract Result OnStartup(UIControlledApplication app);
    Result IExternalApplication.OnStartup(UIControlledApplication app)
    {
      if (app.ActiveAddInId.GetGUID() != GetGUID())
        return Result.Failed;

      return ActivationGate.Open(() => OnStartup(app), this);
    }

    protected abstract Result OnShutdown(UIControlledApplication app);
    Result IExternalApplication.OnShutdown(UIControlledApplication app) =>
      ActivationGate.Open(() => OnShutdown(app), this);

    public virtual bool CatchException(Exception e, UIApplication app, object sender) => false;

    public virtual void ReportException(Exception e, UIApplication app, object sender) { }
  }
}
