using System.Linq;

namespace RhinoInside.Revit.External.UI.Extensions
{
  public static class UIViewExtension
  {
    /// <summary>
    /// Try to found an open <see cref="Autodesk.Revit.UI.UIView"/> that is referencing the specified <see cref="Autodesk.Revit.DB.View"/> element.
    /// </summary>
    /// <param name="view"></param>
    /// <param name="uiView"></param>
    /// <returns>true on succes.</returns>
    public static bool TryGetOpenUIView(this Autodesk.Revit.DB.View view, out Autodesk.Revit.UI.UIView uiView)
    {
      if (view?.IsValidObject != true)
      {
        uiView = default;
        return false;
      }

      using (var uiDocument = new Autodesk.Revit.UI.UIDocument(view.Document))
      {
        uiView = uiDocument.GetOpenUIViews().Where(x => x.ViewId == view.Id).FirstOrDefault();
        return uiView is object;
      }
    }
  }
}
