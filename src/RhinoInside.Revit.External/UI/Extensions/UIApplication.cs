using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIApplicationExtension
  {
    /// <summary>
    ///  Looks up and retrieves the Revit command id with the given id string.
    /// </summary>
    /// <remarks>Looks like there is a bug on `RevitCommandId.LookupPostableCommandId` when resulting `RevitCommandId.Name` is an integer.</remarks>
    /// <param name="app"></param>
    /// <param name="postableCommand"></param>
    /// <returns>The Revit command id. Returns null if the command is not found.</returns>
    internal static RevitCommandId LookupPostableCommandId(this UIApplication app, PostableCommand postableCommand) => HostedApplication.Active.InvokeInHostContext
    (
      () =>
      {
        var commandId = default (RevitCommandId);
        try { commandId = RevitCommandId.LookupPostableCommandId(postableCommand); }
        catch { }

        if (commandId is null)
          app?.Application.WriteJournalComment($"{nameof(PostableCommand)} = {postableCommand} is not available.", timeStamp: true);

        return commandId;
      }
    );

    internal static IList<UIDocument> GetOpenUIDocuments(this UIApplication app) => HostedApplication.Active.InvokeInHostContext
    (
      () =>
      app.Application.Documents.Cast<Document>().
      Where(x => !x.IsLinked).
      Select(x => new UIDocument(x)).
      Where(x => x.GetOpenUIViews().Count > 0).
      ToArray()
    );

    public static bool TryGetDocument(this UIApplication app, Guid guid, out Document document) =>
      app.Application.Documents.Cast<Document>().TryGetDocument(guid, out document, app.ActiveUIDocument?.Document);
  }
}
