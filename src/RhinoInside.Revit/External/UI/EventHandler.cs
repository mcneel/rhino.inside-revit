using System;
using System.Diagnostics;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI
{
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
      catch (Exceptions.FailException e) { Debug.Fail(e.Source, e.Message); }
      catch (Autodesk.Revit.Exceptions.ApplicationException e) { Debug.Fail(e.Source, e.Message); }
      catch (Exception e)
      {
        if (!CatchException(e, app))
          throw;

        _exception = e;
      }

      if (_exception is object)
        ReportException(_exception, app);
    }

    protected virtual bool CatchException(Exception e, UIApplication app) =>
      app.CatchException(e, this);
    protected virtual void ReportException(Exception e, UIApplication app) =>
      app.ReportException(e, this);
  }
}
