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
    internal static IList<UIDocument> GetOpenUIDocuments(this UIApplication app) => HostedApplication.Active.InvokeInHostContext
    (
      () => app?.Application.Documents.Cast<Document>().
            Where(x => !x.IsLinked).
            Select(x => new UIDocument(x)).
            Where(x => x.GetOpenUIViews().Count > 0).
            ToArray() ??
            Array.Empty<UIDocument>()
    );

    internal static IList<UIView> GetOpenUIViews(this UIApplication app) => HostedApplication.Active.InvokeInHostContext
    (
      () => app?.Application.Documents.Cast<Document>().
            Where(x => !x.IsLinked).
            Select(x => new UIDocument(x)).
            SelectMany(x => x.GetOpenUIViews()).
            ToArray() ??
            Array.Empty<UIView>()
    );

    public static bool TryGetDocument(this UIApplication app, Guid guid, out Document document) =>
      app.Application.Documents.Cast<Document>().TryGetDocument(guid, out document, app.ActiveUIDocument?.Document);

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
        var commandId = default(RevitCommandId);
        try { commandId = RevitCommandId.LookupPostableCommandId(postableCommand); }
        catch { }

        if (commandId is null)
          app?.Application.WriteJournalComment($"{nameof(PostableCommand)} = {postableCommand} is not available.", timeStamp: true);

        return commandId;
      }
    );

    /// <summary>
          /// Looks up and retrieves the Revit command id from the given built-in <see cref="Autodesk.Revit.UI.PostableCommand"/>.
          /// </summary>
          /// <param name="uiApplication">The UI application.</param>
          /// <param name="postableCommand">The postable command.</param>
          /// <param name="commandId"></param>
          /// <returns>True on success; False otherwise.</returns>
    public static bool TryGetRevitCommandId(this UIApplication uiApplication, PostableCommand postableCommand, out RevitCommandId commandId)
    {
      commandId = uiApplication.LookupPostableCommandId(postableCommand);
      if (commandId is null) return false;

      var uiDocument = uiApplication.ActiveUIDocument;
      if (uiDocument is null)
      {
        switch (postableCommand)
        {
#if REVIT_2022
          case PostableCommand.NewProject: break;
          case PostableCommand.NewFamily: break;
#else
          case PostableCommand.NewRevitFile: break;
          case PostableCommand.NewFamilyFile: break;
#endif
          case PostableCommand.NewConceptualMass: break;
          case PostableCommand.OpenRevitFile: break;
          default: return false;
        }
      }
      else if (uiDocument.Document.IsFamilyDocument)
      {
        switch (postableCommand)
        {
#if REVIT_2022
          case PostableCommand.GlobalParameters: return false;
#endif
          case PostableCommand.ProjectParameters: return false;
          case PostableCommand.DesignOptions: return false;
          case PostableCommand.Worksets: return false;
          case PostableCommand.Phases: return false;
          case PostableCommand.ProjectInformation: return false;
          case PostableCommand.Location: return false;
          case PostableCommand.ManageLinks: return false;
          case PostableCommand.ReviewWarnings: return false;
          case PostableCommand.NewSheet: return false;
          case PostableCommand.SheetIssuesOrRevisions: return false;
          case PostableCommand.Filters: return false;
          case PostableCommand.EditSelection: return false;
          case PostableCommand.SaveSelection: return false;

          case PostableCommand.Area: return false;
          case PostableCommand.Room: return false;
          case PostableCommand.Space: return false;
        }
      }
      else
      {
        switch (postableCommand)
        {
          case PostableCommand.FamilyCategoryAndParameters: return false;
          case PostableCommand.FamilyTypes: return false;
        }
      }

      return true;
    }
  }
}
