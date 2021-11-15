using Autodesk.Revit.DB;


namespace RhinoInside.Revit.External.DB.Extensions
{
#if REVIT_2021
  using DBInternalOrigin = Autodesk.Revit.DB.InternalOrigin;
#elif REVIT_2020
  using DBInternalOrigin = Autodesk.Revit.DB.Element;
#else
  using DBInternalOrigin = Autodesk.Revit.DB.BasePoint;
#endif

  public static class InternalOriginExtension
  {
#if REVIT_2021
    /// <summary>
    /// Gets the shared position of the InternalOrigin.
    /// </summary>
    /// <param name="basePoint"></param>
    /// <returns></returns>
    public static XYZ GetSharedPosition(this InternalOrigin basePoint) => basePoint.SharedPosition;

    /// <summary>
    /// Gets the position of the InternalOrigin.
    /// </summary>
    /// <param name="basePoint"></param>
    /// <returns></returns>
    public static XYZ GetPosition(this InternalOrigin basePoint) => basePoint.Position;
#endif

    /// <summary>
    /// Gets the project internal origin for the document.
    /// </summary>
    /// <param name="doc">The document from which to get the internal origin.</param>
    /// <returns>The project internal origin of the document.</returns>
    public static DBInternalOrigin Get(Document doc)
    {
#if REVIT_2021
      return InternalOrigin.Get(doc);
#else
      using (var collector = new FilteredElementCollector(doc))
      {
        return collector.
#if !REVIT_2020
          OfClass(typeof(DBInternalOrigin)).
#endif
          OfCategoryId(new ElementId(BuiltInCategory.OST_IOS_GeoSite)).
          FirstElement() as DBInternalOrigin;
      }
#endif
    }
  }
}
