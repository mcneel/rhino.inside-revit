using System.Linq;
using RhinoInside.Revit.External.DB.Extensions;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIDocumentExtension
  {
    /// <summary>
    /// Try to found an open <see cref="Autodesk.Revit.UI.UIDocument"/> that is referencing the specified <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="uiDocument"></param>
    /// <returns>true on succes.</returns>
    public static bool TryGetOpenUIDocument(this Document document, out UIDocument uiDocument)
    {
      uiDocument = new Autodesk.Revit.UI.UIDocument(document);
      if(uiDocument.GetOpenUIViews().Count == 0)
      {
        uiDocument.Dispose();
        uiDocument = default;
        return false;
      }

      return true;
    }

    /// <summary>
    /// Gets the active Graphical <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.UI.UIDocument"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active graphical <see cref="Autodesk.Revit.DB.View"/></returns>
    public static bool TryGetActiveGraphicalView(this UIDocument uiDocument, out UIView uiView)
    {
      uiView = HostedApplication.Active.InvokeInHostContext(() =>
      {
        var _uiView_ = default(Autodesk.Revit.UI.UIView);

        // List all open UI Views
        var openViews = uiDocument.GetOpenUIViews();

        // Look for one that is associated to the `ActiveGraphicalView`
        if (uiDocument.ActiveGraphicalView is Autodesk.Revit.DB.View activeView && activeView.IsValidObject)
          _uiView_ = openViews.FirstOrDefault(x => x.ViewId == activeView.Id);

        // Look for the first Graphical View that is open
        if (_uiView_?.IsValidObject != true)
        {
          _uiView_ = openViews.
          Where
          (
            x =>
            {
              using (var view = uiDocument.Document.GetElement(x.ViewId) as Autodesk.Revit.DB.View)
                return view.IsGraphicalView();
            }
          ).
          FirstOrDefault();
        }

        return _uiView_;
      });

      return uiView is object;
    }

    /// <summary>
    /// Looks up and retrieves the Revit command id from the given built-in <see cref="Autodesk.Revit.UI.PostableCommand"/>.
    /// </summary>
    /// <param name="uiDocument">The UI document.</param>
    /// <param name="postableCommand">The postable command.</param>
    /// <param name="commandId"></param>
    /// <returns>True on success; False otherwise.</returns>
    public static bool TryGetRevitCommandId(this UIDocument uiDocument, PostableCommand postableCommand, out RevitCommandId commandId)
    {
      commandId = default;
      if (uiDocument is null) return false;
      if (!System.Enum.IsDefined(typeof(RevitCommandId), postableCommand)) return false;

      commandId = HostedApplication.Active?.InvokeInHostContext(() => RevitCommandId.LookupPostableCommandId(postableCommand));
      if (commandId is null) return false;
      if (!uiDocument.Application.CanPostCommand(commandId)) return false;

      if (uiDocument.Document.IsFamilyDocument)
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
