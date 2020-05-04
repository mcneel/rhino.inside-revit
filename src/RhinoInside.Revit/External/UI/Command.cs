using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI
{
  using Extensions;

  public abstract class Command : IExternalCommand
  {
    public abstract Result Execute(ExternalCommandData data, ref string message, ElementSet elements);
    Result IExternalCommand.Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
      var _exception = default(Exception);
      var _message = message;

      try { return ActivationGate.Open(() => Execute(data, ref _message, elements), this); }
      catch (Exceptions.CancelException e) { _message = e.Message; return Result.Cancelled; }
      catch (Exceptions.FailException e) { _message = e.Message; return Result.Failed; }
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
}
