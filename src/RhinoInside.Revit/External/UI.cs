using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI
{
  public abstract class Application : AddInId, IExternalApplication
  {
    protected Application(Guid addInId) : base(addInId) { }

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

  public abstract class Command : IExternalCommand
  {
    public abstract Result Execute(ExternalCommandData data, ref string message, ElementSet elements);
    Result IExternalCommand.Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var _exception = default(Exception);
      var _message = message;

      try { return ActivationGate.Open(() => Execute(data, ref _message, elements), this); }
      catch (Exceptions.CancelException e) { _message = e.Message; return Result.Cancelled; }
      catch (Exceptions.FailException e)   { _message = e.Message; return Result.Failed; }
      catch (Autodesk.Revit.Exceptions.ApplicationException e) { _message = e.Message; return Result.Failed; }
      catch (Exception e)
      {
        if (!CatchException(e, data.Application))
          throw;

        _exception = e;
      }
      finally { message = _message; }

      if (_exception is object)
        ReportException(_exception, data.Application);

      return Result.Failed;
    }

    protected virtual bool CatchException(Exception e, UIApplication app) =>
      app.CatchException(e, this);
    protected virtual void ReportException(Exception e, UIApplication app) =>
      app.ReportException(e, this);
  }

  public abstract class CommandAvailability : IExternalCommandAvailability
  {
    public abstract bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories);

    bool IExternalCommandAvailability.IsCommandAvailable(UIApplication app, CategorySet selectedCategories) =>
      !ActivationGate.IsOpen && IsCommandAvailable(app, selectedCategories);
  }

  public abstract class EventHandler : IExternalEventHandler
  {
    public abstract string GetName();
    string IExternalEventHandler.GetName() =>
      GetName();

    protected abstract void Execute(UIApplication app);
    void IExternalEventHandler.Execute(UIApplication app)
    {
      var _exception = default(Exception);

      try { ActivationGate.Open(() => Execute(app), this); }
      catch (Exceptions.CancelException e) { if (!string.IsNullOrEmpty(e.Message)) Debug.Fail(e.Source, e.Message); }
      catch (Exceptions.FailException e)   { Debug.Fail(e.Source, e.Message); }
      catch (Autodesk.Revit.Exceptions.ApplicationException e) { Debug.Fail(e.Source, e.Message); }
      catch (Exception e)
      {
        if (!CatchException(e, app))
          throw;

        _exception = e;
      }

      if(_exception is object)
        ReportException(_exception, app);
    }

    protected virtual bool CatchException(Exception e, UIApplication app) =>
      app.CatchException(e, this);
    protected virtual void ReportException(Exception e, UIApplication app) =>
      app.ReportException(e, this);
  }
}
