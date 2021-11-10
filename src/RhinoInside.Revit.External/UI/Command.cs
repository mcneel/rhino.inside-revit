using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI
{
  public abstract class ExternalCommand : IExternalCommand
  {
    public abstract Result Execute(ExternalCommandData data, ref string message, ElementSet elements);
    Result IExternalCommand.Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var _exception = default(Exception);
      var _message = message;

      try { return ActivationGate.Open(() => Execute(data, ref _message, elements), this); }
      catch (CancelException e) { _message = e.Message; return Result.Cancelled; }
      catch (FailException e) { _message = e.Message; return Result.Failed; }
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
      ExternalApplication.InvokeCatchException(app, e, this);
    protected virtual void ReportException(Exception e, UIApplication app) =>
      ExternalApplication.InvokeReportException(app, e, this);
  }

  public abstract class CommandAvailability : IExternalCommandAvailability
  {
    public virtual bool IsRuntimeReady() => true;
    protected virtual bool IsCommandAvailable(UIApplication app, CategorySet selectedCategories) => true;

    bool IExternalCommandAvailability.IsCommandAvailable(UIApplication app, CategorySet selectedCategories)
    {
      if (ActivationGate.IsOpen) return false;
      if (!IsRuntimeReady())     return false;

      return IsCommandAvailable(app, selectedCategories);
    }
  }
}
