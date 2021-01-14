using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BasePointExtension
  {
    /// <summary>
    /// Gets the shared position of the BasePoint.
    /// </summary>
    /// <param name="basePoint"></param>
    /// <returns></returns>
    public static XYZ GetSharedPosition(this BasePoint basePoint)
    {
#if REVIT_2020
      return basePoint.SharedPosition;
#else
      var position = basePoint.Document.ActiveProjectLocation.GetProjectPosition(GetPosition(basePoint));
      return new XYZ(position.EastWest, position.NorthSouth, position.Elevation);
#endif
    }

    /// <summary>
    /// Gets the position of the BasePoint.
    /// </summary>
    /// <param name="basePoint"></param>
    /// <returns></returns>
    public static XYZ GetPosition(this BasePoint basePoint)
    {
#if REVIT_2020
      return basePoint.Position;
#else
      return basePoint.get_BoundingBox(null).Min;
#endif
    }

    /// <summary>
    /// Gets the project internal origin base point for the document.
    /// </summary>
    /// <param name="doc">The document from which to get the internal origin base point.</param>
    /// <returns>The project base point of the document.</returns>
    public static BasePoint GetInternalOriginPoint(Document doc)
    {
      using (var collector = new FilteredElementCollector(doc))
      {
        var pointCollector = System.Linq.Enumerable.Cast<BasePoint>(collector.OfClass(typeof(BasePoint)));
        pointCollector = System.Linq.Enumerable.Where(pointCollector, x => x.Category.Id.IntegerValue == (int) BuiltInCategory.OST_IOS_GeoSite);
        return System.Linq.Enumerable.FirstOrDefault(pointCollector);
      }
    }

    /// <summary>
    /// Gets the project base point for the document.
    /// </summary>
    /// <param name="doc">The document from which to get the project base point.</param>
    /// <returns>The project base point of the document.</returns>
    public static BasePoint GetProjectBasePoint(Document doc)
    {
#if REVIT_2021
      return BasePoint.GetProjectBasePoint(doc);
#else
      using (var collector = new FilteredElementCollector(doc))
      {
        var pointCollector = collector.OfCategory(BuiltInCategory.OST_ProjectBasePoint);
        return pointCollector.FirstElement() as BasePoint;
      }
#endif
    }

    /// <summary>
    /// Gets the survey point for the document.
    /// </summary>
    /// <param name="doc">The document from which to get the survey point.</param>
    /// <returns>The survey point of the document.</returns>
    public static BasePoint GetSurveyPoint(Document doc)
    {
#if REVIT_2021
      return BasePoint.GetSurveyPoint(doc);
#else
      using (var collector = new FilteredElementCollector(doc))
      {
        var pointCollector = collector.OfCategory(BuiltInCategory.OST_SharedBasePoint);
        return pointCollector.FirstElement() as BasePoint;
      }
#endif
    }
  }
}
