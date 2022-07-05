using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using System.Linq;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.External.ApplicationServices.Extensions
{
  public static class ApplicationExtension
  {
    public static DefinitionFile CreateSharedParameterFile(this Application app)
    {
      string sharedParametersFilename = app.SharedParametersFilename;
      try
      {
        // Create Temp Shared Parameters File
        app.SharedParametersFilename = Path.GetTempFileName();
        return app.OpenSharedParameterFile();
      }
      finally
      {
        // Restore User Shared Parameters File
        try { File.Delete(app.SharedParametersFilename); }
        finally { app.SharedParametersFilename = sharedParametersFilename; }
      }
    }

#if !REVIT_2018
    public static IEnumerable<Autodesk.Revit.Utility.Asset> GetAssets(this Application app, Autodesk.Revit.Utility.AssetType assetType)
    {
      return app.get_Assets(assetType).Cast<Autodesk.Revit.Utility.Asset>();
    }

    public static AppearanceAssetElement Duplicate(this AppearanceAssetElement element, string name)
    {
      return AppearanceAssetElement.Create(element.Document, name, element.GetRenderingAsset());
    }
#endif

    public static int ToLCID(this LanguageType value)
    {
      switch (value)
      {
        case LanguageType.English_USA: return 1033;
        case LanguageType.German: return 1031;
        case LanguageType.Spanish: return 1034;
        case LanguageType.French: return 1036;
        case LanguageType.Italian: return 1040;
        case LanguageType.Dutch: return 1043;
        case LanguageType.Chinese_Simplified: return 2052;
        case LanguageType.Chinese_Traditional: return 1028;
        case LanguageType.Japanese: return 1041;
        case LanguageType.Korean: return 1042;
        case LanguageType.Russian: return 1049;
        case LanguageType.Czech: return 1029;
        case LanguageType.Polish: return 1045;
        case LanguageType.Hungarian: return 1038;
        case LanguageType.Brazilian_Portuguese: return 1046;
#if REVIT_2018
        case LanguageType.English_GB: return 2057;
#endif
      }

      return 1033;
    }

    #region SubVersionNumber
    internal static string GetSubVersionNumber(this Application app)
    {
#if REVIT_2018
      return app.SubVersionNumber;
#else
      return $"{app.VersionNumber}.0";
#endif
    }
    #endregion

    #region UI
    /// <summary>
    /// Queries for all open <see cref="Autodesk.Revit.DB.Document"/> in Revit UI.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="projects"></param>
    /// <param name="families"></param>
    public static void GetOpenDocuments(this Application app, out IList<Document> projects, out IList<Document> families)
    {
      using (var documents = app.Documents)
      {
        projects = new List<Document>();
        families = new List<Document>();

        foreach (var doc in documents.Cast<Document>())
        {
          if (doc.IsLinked)
            continue;

          if (doc.GetActiveView() is null)
            continue;

          if (doc.IsFamilyDocument)
            families.Add(doc);
          else
            projects.Add(doc);
        }
      }
    }
    #endregion
  }

  static class ControlledApplicationExtension
  {
    public static string GetCurrentUsersDataFolderPath(this ControlledApplication app)
    {
#if REVIT_2019
      return app.CurrentUsersDataFolderPath;
#else
      return System.IO.Path.Combine
      (
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
        "Autodesk",
        "Revit",
        app.VersionName
      );
#endif
    }

    #region SubVersionNumber
    internal static string GetSubVersionNumber(this ControlledApplication app)
    {
      try { return InternalGetSubVersionNumber(app); }
      catch (System.MissingMemberException) { return $"{app.VersionNumber}.0"; }
    }

#if REVIT_2018
    static string InternalGetSubVersionNumber(ControlledApplication app) => app.SubVersionNumber;
#else
    static string InternalGetSubVersionNumber(ControlledApplication app) => $"{app.VersionNumber}.0";
#endif
    #endregion
  }
}
