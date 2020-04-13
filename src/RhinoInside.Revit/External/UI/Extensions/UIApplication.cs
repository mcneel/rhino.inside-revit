using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIApplicationExtension
  {
    internal static bool ActivateRibbonTab(this UIApplication app, string tabName)
    {
      foreach (var tab in Autodesk.Windows.ComponentManager.Ribbon.Tabs)
      {
        if (tab.Name == tabName)
        {
          tab.IsActive = true;
          return true;
        }
      }

      return false;
    }

    internal static bool CatchException(this UIApplication app, Exception e, object sender)
    {
      var addinId = app.ActiveAddInId?.GetGUID() ?? Guid.Empty;
      if (addinId != Guid.Empty)
      {
        if (app.LoadedApplications.OfType<UI.Application>().Where(x => x.GetGUID() == addinId).FirstOrDefault() is UI.Application addin)
          return addin.CatchException(e, app, sender);
      }

      return false;
    }

    internal static void ReportException(this UIApplication app, Exception e, object sender)
    {
      var comment = $@"Managed exception caught from external API application '{e.Source}' in method '{e.TargetSite}' Exception type: '<{e.GetType().FullName}>,' Exception method: '<{e.Message}>,' Stack trace '   {e.StackTrace}";
      comment = comment.Replace(Environment.NewLine, $"{Environment.NewLine}'");
      app.Application.WriteJournalComment(comment, true);

      foreach (var hWnd in ActivationGate.GateWindows)
      {
        using (var window = new WindowHandle(hWnd))
        {
          window.HideOwnedPopups();
          window.Hide();
        }
      }

      var addinId = app.ActiveAddInId?.GetGUID() ?? Guid.Empty;
      if (addinId != Guid.Empty)
      {
        if (app.LoadedApplications.OfType<UI.Application>().Where(x => x.GetGUID() == addinId).FirstOrDefault() is UI.Application addin)
          addin.ReportException(e, app, sender);
      }
    }

    public static bool TryGetDocument(this UIApplication app, Guid guid, out Document document) =>
      app.Application.Documents.Cast<Document>().TryGetDocument(guid, out document, app.ActiveUIDocument?.Document);
  }
}
