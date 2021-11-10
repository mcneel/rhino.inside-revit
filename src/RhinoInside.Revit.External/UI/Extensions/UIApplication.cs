using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIApplicationExtension
  {
    internal static IList<UIDocument> GetOpenUIDocuments(this UIApplication app)
    {
      return HostedApplication.Active.InvokeInHostContext
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
