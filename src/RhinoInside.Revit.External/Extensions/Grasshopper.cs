using Grasshopper.Kernel;

namespace Grasshopper
{
  internal static class GH_DocumentExtension
  {
    public static bool KeepOpen(this GH_Document document)
    {
#if RHINO_8
      return document.Properties.KeepOpen;
#else
      return false;
#endif
    }
  }
}
