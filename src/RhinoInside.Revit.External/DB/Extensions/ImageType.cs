using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ImageTypeExtension
  {
#if !REVIT_2020
    public static bool CanReload(this ImageType type)
    {
      return type.IsLoadedFromFile() && System.IO.File.Exists(type.Path);
    }
#endif
  }
}
