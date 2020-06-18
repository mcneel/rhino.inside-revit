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
  }
}
