using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32.SafeHandles;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIApplicationExtension
  {
    internal static bool CatchException(this UIApplication app, Exception e, object sender)
    {
      var addinId = app.ActiveAddInId?.GetGUID() ?? Guid.Empty;
      if (addinId != Guid.Empty)
      {
        if (app.LoadedApplications.OfType<UI.ExternalApplication>().Where(x => x.GetGUID() == addinId).FirstOrDefault() is UI.ExternalApplication addin)
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
        if (app.LoadedApplications.OfType<UI.ExternalApplication>().Where(x => x.GetGUID() == addinId).FirstOrDefault() is UI.ExternalApplication addin)
          addin.ReportException(e, app, sender);
      }
    }

    internal static IList<UIDocument> GetOpenUIDocuments(this UIApplication app)
    {
      return Rhinoceros.InvokeInHostContext
      (
        () =>
        app.Application.Documents.Cast<Document>().
        Where(x => !x.IsLinked).
        Select(x => new UIDocument(x)).
        Where(x => x.GetOpenUIViews().Count > 0).
        ToArray()
      );
    }

    public static bool TryGetDocument(this UIApplication app, Guid guid, out Document document) =>
      app.Application.Documents.Cast<Document>().TryGetDocument(guid, out document, app.ActiveUIDocument?.Document);
  }
}
