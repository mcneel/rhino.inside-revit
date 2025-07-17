using System;
using System.IO;
using Grasshopper.Kernel;

namespace Grasshopper
{
  internal static class GH_DocumentExtension
  {
    public static string GetTransactionName(this GH_Document document)
    {
      var displayName = string.Empty;
      if (document.Properties.ProjectFileName is object) displayName = Path.GetFileNameWithoutExtension(document.Properties.ProjectFileName).TripleDot(24).Replace("_", " ").Replace("-", " ");
      if (string.IsNullOrEmpty(displayName) && document.FilePath is object) displayName = Path.GetFileNameWithoutExtension(document.FilePath).TripleDot(24).Replace("_", " ").Replace("-", " ");
      if (string.IsNullOrEmpty(displayName)) displayName = $"Grasshopper {DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentUICulture)}";

      return displayName.ToControlEscaped();
    }

    public static bool KeepOpen(this GH_Document document)
    {
#if RHINO_8
      return document.Properties.KeepOpen;
#else
      return false;
#endif
    }

    public static Rhino.RhinoDoc RhinoDocument(this GH_Document document)
    {
#if RHINO_8
      return document.RhinoDocument;
#else
      return Rhino.RhinoDoc.ActiveDoc;
#endif
    }
  }
}
