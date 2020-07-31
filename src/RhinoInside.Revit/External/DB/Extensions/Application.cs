using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ApplicationExtension
  {
    /// <summary>
    /// Queries for all open <see cref="Autodesk.Revit.DB.Document"/> in Revit UI.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="projects"></param>
    /// <param name="families"></param>
    public static void GetOpenDocuments(this Autodesk.Revit.ApplicationServices.Application app, out IList<Document> projects, out IList<Document> families)
    {
      using (var documents = app.Documents)
      {
        projects = new List<Document>();
        families = new List<Document>();

        foreach (var doc in documents.Cast<Document>())
        {
          if (doc.IsLinked)
            continue;

          if (doc.GetActiveGraphicalView() is null)
            continue;

          if (doc.IsFamilyDocument)
            families.Add(doc);
          else
            projects.Add(doc);
        }
      }
    }
  }
}
