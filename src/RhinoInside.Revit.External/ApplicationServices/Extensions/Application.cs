using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Microsoft.Win32.SafeHandles.InteropServices;

namespace RhinoInside.Revit.External.ApplicationServices.Extensions
{
  using DB.Extensions;
  using External.UI;

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

    /// <summary>
    /// Queries for all open <see cref="Autodesk.Revit.DB.View"/> in Revit UI.
    /// </summary>
    /// <param name="app"></param>
    public static IEnumerable<View> GetOpenViews(this Application app)
    {
      using (var documents = app.Documents)
      {
        foreach (var doc in documents.Cast<Document>())
        {
          if (doc.IsLinked)
            continue;

          var openViewIds = HostedApplication.Active.InvokeInHostContext(() =>
          {
            using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
              return uiDocument.GetOpenUIViews().Select(x => x.ViewId).ToList();
          });

          using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
          {
            foreach (var viewId in openViewIds)
              yield return doc.GetElement(viewId) as View;
          }
        }
      }
    }
    #endregion

    #region Settings
    public static string GetCurrentUsersDataFolderPath(this Application app)
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

    internal static string GetProjectPath(this Application app)
    {
      if (TryGetProfileValue(app, "Directories", "ProjectPath", out var projectsPath))
        return System.Environment.ExpandEnvironmentVariables(projectsPath);

      return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
    }

    public static bool TryGetProfileValue(this Application app, string section, string key, out string value)
    {
      if (!string.IsNullOrEmpty(section) && !string.IsNullOrEmpty(key))
      {
        var revit_ini = Path.Combine(app.GetCurrentUsersDataFolderPath(), "Revit.ini");
        var result = new System.Text.StringBuilder(1024);
        while (Kernel32.GetPrivateProfileString(section, key, string.Empty, result, (uint) result.Capacity, revit_ini) == result.Capacity - 1)
          result.EnsureCapacity(result.Capacity * 2);

        if (result.Length > 0)
        {
          value = result.ToString();
          return true;
        }
      }

      value = default;
      return false;
    }

    public static ColorSettings GetColorSettings(this Application app) => new ColorSettings(app);
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

  public class ColorSettings
  {
    internal ColorSettings(Application app)
    {
#if REVIT_2021
      using (var colors = ColorOptions.GetColorOptions())
      {
        EditingColor = colors.EditingColor;
        CalculatingColor = colors.CalculatingColor;
        AlertColor = colors.AlertColor;
        PreselectionColor = colors.PreselectionColor;
        SelectionSemitransparent = colors.SelectionSemitransparent;
        SelectionColor = colors.SelectionColor;
        BackgroundColor = colors.BackgroundColor;
      }
#else
      EditingColor = new Color(128, 255, 64);
      if (app.TryGetProfileValue("Colors", "EditingColor", out var editingColor))
      {
        if (int.TryParse(editingColor, out var abgr))
          EditingColor = ToColor(abgr);
      }

      CalculatingColor = new Color(0, 255, 255);
      if (app.TryGetProfileValue("Colors", "TemporaryColor", out var calculatingColor))
      {
        if (int.TryParse(calculatingColor, out var abgr))
          CalculatingColor = ToColor(abgr);
      }

      AlertColor = new Color(255, 128, 0);
      if (app.TryGetProfileValue("Colors", "ErrorColor", out var errorColor))
      {
        if (int.TryParse(errorColor, out var abgr))
          AlertColor = ToColor(abgr);
      }

      SelectionColor = new Color(0, 59, 189);
      if (app.TryGetProfileValue("Colors", "PreHiliteColor", out var preHiliteColor))
      {
        if (int.TryParse(preHiliteColor, out var abgr))
          PreselectionColor = ToColor(abgr);
      }

      SelectionSemitransparent = false;
      if (app.TryGetProfileValue("Graphics", "SemiTransparent", out var semiTransparent))
      {
        if (int.TryParse(semiTransparent, out var _semiTransparent))
          SelectionSemitransparent = _semiTransparent != 0;
      }

      SelectionColor = new Color(0, 59, 189);
      if (app.TryGetProfileValue("Colors", "HiliteColor", out var hiliteColor))
      {
        if (int.TryParse(hiliteColor, out var abgr))
          SelectionColor = ToColor(abgr);
      }

      BackgroundColor = new Color(255, 255, 255);
      if (app.TryGetProfileValue("Colors", "BackgroundColor", out var backgroundColor))
      {
        if (int.TryParse(backgroundColor, out var abgr))
          BackgroundColor = ToColor(abgr);
      }
#endif
    }

    static Color ToColor(int abgr) => new Color
    (
      (byte) ((abgr >> 0) & byte.MaxValue),
      (byte) ((abgr >> 8) & byte.MaxValue),
      (byte) ((abgr >> 16) & byte.MaxValue)
    );

    public Color EditingColor { get; private set; }
    public Color CalculatingColor { get; private set; }
    public Color AlertColor { get; private set; }
    public Color PreselectionColor { get; private set; }
    public bool SelectionSemitransparent { get; private set; }
    public Color SelectionColor { get; private set; }
    public Color BackgroundColor { get; private set; }
  }
}
