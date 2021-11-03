using System.Linq;
using RhinoInside.Revit.External.DB.Extensions;

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
    public static bool TryGetOpenUIDocument(this Autodesk.Revit.DB.Document document, out Autodesk.Revit.UI.UIDocument uiDocument)
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
    public static bool TryGetActiveGraphicalView(this Autodesk.Revit.UI.UIDocument uiDocument, out Autodesk.Revit.UI.UIView uiView)
    {
      uiView = Rhinoceros.InvokeInHostContext(() =>
      {
        var _uiView_ = default(Autodesk.Revit.UI.UIView);

        // List all open UI Views
        var openViews = uiDocument.GetOpenUIViews();

        // Look for one that is associated to the `ActiveGraphicalView`
        if (uiDocument.ActiveGraphicalView is Autodesk.Revit.DB.View activeView && activeView.IsValidObject)
          _uiView_ = openViews.Where(x => x.ViewId == activeView.Id).FirstOrDefault();

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
  }
}
