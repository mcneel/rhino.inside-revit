using Autodesk.Revit.DB;


namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ProjectLocationExtension
  {
#if !REVIT_2018
    public static SiteLocation GetSiteLocation(this ProjectLocation location) => location.SiteLocation;

    public static ProjectPosition GetProjectPosition(this ProjectLocation location, XYZ point) => location.get_ProjectPosition(point);
#endif
  }
}
