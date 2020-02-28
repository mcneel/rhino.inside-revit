using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI
{
  public abstract class Application : IExternalApplication
  {
    protected abstract Result OnStartup(UIControlledApplication app);
    Result IExternalApplication.OnStartup(UIControlledApplication app) =>
      ActivationGate.Open(() => OnStartup(app), this);

    protected abstract Result OnShutdown(UIControlledApplication app);
    Result IExternalApplication.OnShutdown(UIControlledApplication app) =>
      ActivationGate.Open(() => OnShutdown(app), this);

    public virtual bool CatchException(Exception e, UIApplication app, object sender) => false;
  }

  public abstract class Command : IExternalCommand
  {
    public abstract Result Execute(ExternalCommandData data, ref string message, ElementSet elements);
    Result IExternalCommand.Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var _message = message;
      try { return ActivationGate.Open(() => Execute(data, ref _message, elements), this); }
      catch (Exceptions.CancelException e) { _message = e.Message; return Result.Cancelled; }
      catch (Exceptions.FailException e)   { _message = e.Message; return Result.Failed; }
      catch (Autodesk.Revit.Exceptions.ApplicationException e) { _message = e.Message; return Result.Failed; }
      catch (Exception e)
      {
        if (CatchException(e, data.Application))
          return Result.Failed;

        throw;
      }
      finally { message = _message; }
    }

    protected virtual bool CatchException(Exception e, UIApplication app) =>
      app.CatchException(e, this);
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
      try { ActivationGate.Open(() => Execute(app), this); }
      catch (Exceptions.CancelException e) { if (!string.IsNullOrEmpty(e.Message)) Debug.Fail(e.Source, e.Message); }
      catch (Exceptions.FailException e)   { Debug.Fail(e.Source, e.Message); }
      catch (Autodesk.Revit.Exceptions.ApplicationException e) { Debug.Fail(e.Source, e.Message); }
      catch (Exception e)
      {
        if (CatchException(e, app))
          return;

        throw;
      }
    }

    protected virtual bool CatchException(Exception e, UIApplication app) =>
      app.CatchException(e, this);
  }
}
