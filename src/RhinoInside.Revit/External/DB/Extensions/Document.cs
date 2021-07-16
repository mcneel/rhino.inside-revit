using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class DocumentExtension
  {
    public static bool IsValid(this Document doc) => doc?.IsValidObject == true;

    public static bool IsValidWithLog(this Document doc, out string log)
    {
      if (doc is null)        { log = "Document is a null reference.";         return false; }
      if (!doc.IsValidObject) { log = "Referenced Revit document was closed."; return false; }

      log = string.Empty;
      return true;
    }

    /// <summary>
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.Document"/> equals to this <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Autodesk.Revit.DB.Document"/> instances are considered equivalent if they represent the same document
    /// in this Revit session.
    /// </remarks>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsEquivalent(this Document self, Document other)
    {
      if (ReferenceEquals(self, other))
        return true;

      if (self is null || other is null)
        return false;

      return self.Equals(other);
    }

    public static bool Release(this Document doc)
    {
      if (doc.IsValid())
      {
        try
        {
          using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
          {
            if (uiDocument.GetOpenUIViews().Count == 0)
              return doc.Close(false);
          }
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
      }

      return false;
    }

    public static Guid GetFingerprintGUID(this Document doc)
    {
      if (doc?.IsValidObject != true)
        return Guid.Empty;

      return ExportUtils.GetGBXMLDocumentId(doc);
    }

    private static int seed = 0;
    private static readonly Dictionary<Guid, int> DocumentsSessionDictionary = new Dictionary<Guid, int>();

    public static int DocumentSessionId(Guid key)
    {
      if (key == Guid.Empty)
        throw new ArgumentException("Invalid argument value", nameof(key));

      if (DocumentsSessionDictionary.TryGetValue(key, out var value))
        return value;

      DocumentsSessionDictionary.Add(key, ++seed);
      return seed;
    }

    #region File
    /// <summary>
    /// The file name of the document's disk file without extension.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The mode title</returns>
    public static string GetTitle(this Document doc)
    {
#if REVIT_2022
      return doc.Title;
#else
      return Path.GetFileNameWithoutExtension(doc.Title);
#endif
    }

    /// <summary>
    /// The file name of the document's disk file with extension.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The model file name</returns>
    /// <remarks>
    /// This method returns an empty string if the project has not been saved.
    /// Note that the file name returned will be empty if a document is detached.
    /// <see cref="Document.IsDetached"/>
    /// </remarks>
    public static string GetFileName(this Document doc)
    {
      /// <summary>
      /// To enforce the method remarks, we need to check IsDetached here.
      /// </summary>
      if (doc is null || doc.IsDetached)
        return string.Empty;

      if (!string.IsNullOrEmpty(doc.PathName))
        return Path.GetFileName(doc.PathName);

      return doc.GetTitle() + (doc.IsFamilyDocument ? ".rfa" : ".rvt");
    }

    /// <summary>
    /// Gets the model path of the model.
    /// </summary>
    /// <remarks>
    /// If <paramref name="doc"/> is still not saved this method returns null.
    /// </remarks>
    /// <param name="doc"></param>
    /// <returns>The model path or null if still not saved.</returns>
    public static ModelPath GetModelPath(this Document doc)
    {
      /// <summary>
      /// Revit documentation reads like:
      ///
      /// <see cref="Document.PathName"/> summary:
      /// This string is empty if the project has not been saved or does not have a disk
      /// file associated with it yet. Note that the pathname will be **empty** if a document
      /// is detached. See Autodesk.Revit.DB.Document.IsDetached.
      ///  
      /// <see cref="Document.IsDetached"/> summary:
      /// Note that <see cref="Document.Title"/> and <see cref="Document.PathName"/> 
      /// will be **empty** strings if a document is detached.
      /// 
      /// Both descriptions are not accurate.
      /// Detached models return only the file name on its Document.PathName property,
      /// instead of an empty string. So we need to check IsDetached here.
      /// </summary>
      if (string.IsNullOrEmpty(doc.PathName) || doc.IsDetached)
        return default;

#if REVIT_2019
      if (doc.IsModelInCloud)
        return doc.GetCloudModelPath();
#endif

      return ModelPathUtils.ConvertUserVisiblePathToModelPath(doc.PathName);
    }
    #endregion

    #region ElementId
    internal static bool TryGetDocument(this IEnumerable<Document> set, Guid guid, out Document document, Document activeDBDocument)
    {
      if (guid != Guid.Empty)
      {
        // For performance reasons and also in case of conflict the ActiveDBDocument will have priority
        if (activeDBDocument is object && ExportUtils.GetGBXMLDocumentId(activeDBDocument) == guid)
        {
          document = activeDBDocument;
          return true;
        }

        foreach (var doc in set.Where(x => ExportUtils.GetGBXMLDocumentId(x) == guid))
        {
          document = doc;
          return true;
        }
      }

      document = default;
      return false;
    }

    public static bool TryGetCategoryId(this Document doc, string uniqueId, out ElementId categoryId)
    {
      categoryId = default;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id))
      {
        if (EpisodeId == Guid.Empty)
        {
          if (((BuiltInCategory) id).IsValid())
            categoryId = new ElementId((BuiltInCategory) id);
        }
        else
        {
          if (AsCategory(doc?.GetElement(uniqueId)) is Category category)
            categoryId = category.Id;
        }
      }

      return categoryId is object;
    }

    public static bool TryGetParameterId(this Document doc, string uniqueId, out ElementId parameterId)
    {
      parameterId = default;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id))
      {
        if (EpisodeId == Guid.Empty)
        {
          if (((BuiltInParameter) id).IsValid())
            parameterId = new ElementId((BuiltInParameter) id);
        }
        else
        {
          if (doc?.GetElement(uniqueId) is ParameterElement parameter)
            parameterId = parameter.Id;
        }
      }

      return parameterId is object;
    }

    public static bool TryGetLinePatternId(this Document doc, string uniqueId, out ElementId patternId)
    {
      patternId = default;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id))
      {
        if (EpisodeId == Guid.Empty)
        {
          if (((BuiltInLinePattern) id).IsValid())
            patternId = new ElementId(id);
        }
        else
        {
          if (doc?.GetElement(uniqueId) is LinePatternElement pattern)
            patternId = pattern.Id;
        }
      }

      return patternId is object;
    }

    public static bool TryGetElementId(this Document doc, string uniqueId, out ElementId elementId)
    {
      elementId = default;

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id) && EpisodeId == Guid.Empty)
      {
        if (((BuiltInCategory) id).IsValid())
          elementId = new ElementId((BuiltInCategory) id);

        else if (((BuiltInParameter) id).IsValid())
          elementId = new ElementId((BuiltInParameter) id);

        else if (((BuiltInLinePattern) id).IsValid())
          elementId = new ElementId(id);
      }
      else
      {
        try
        {
          if (Reference.ParseFromStableRepresentation(doc, uniqueId) is Reference reference)
            elementId = reference.ElementId;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { }
      }

      return elementId is object;
    }

    /// <summary>
    /// Compare two <see cref="Autodesk.Revit.DB.Reference"/> objects to know it are referencing same <see cref="Autodesk.Revit.Element"/>
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>true if both references are equivalent</returns>
    public static bool AreEquivalentReferences(this Document doc, Reference x, Reference y)
    {
      var UniqueIdX = x?.ConvertToStableRepresentation(doc);
      var UniqueIdY = y?.ConvertToStableRepresentation(doc);

      return UniqueIdX == UniqueIdY;
    }

    public static T GetElement<T>(this Document doc, ElementId elementId) where T : Element
    {
      return doc.GetElement(elementId) as T;
    }

    public static T GetElement<T>(this Document doc, Reference reference) where T : Element
    {
      return doc.GetElement(reference) as T;
    }
    #endregion

    #region Name
    internal static bool TryGetElement<T>(this Document doc, out T element, string name, string parentName = default, BuiltInCategory? categoryId = default) where T : Element
    {
      if (typeof(ElementType).IsAssignableFrom(typeof(T)))
      {
        using (var collector = new FilteredElementCollector(doc).WhereElementIsKindOf(typeof(T)))
        {
          element = collector.
          WhereElementIsKindOf(typeof(T)).
          WhereCategoryIdEqualsTo(categoryId).
          WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, parentName).
          WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_TYPE_NAME, name).
          OfType<T>().FirstOrDefault();
        }
      }
      else if (typeof(AppearanceAssetElement).IsAssignableFrom(typeof(T)))
      {
        element = name is object ? AppearanceAssetElement.GetAppearanceAssetElementByName(doc, name) as T : default;
      }
      else
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          var elementCollector = collector.
          WhereElementIsKindOf(typeof(T)).
          WhereCategoryIdEqualsTo(categoryId);

          var nameParameter = ElementExtension.GetNameParameter(typeof(T));
          var enumerable = nameParameter != BuiltInParameter.INVALID ?
            elementCollector.WhereParameterEqualsTo(nameParameter, name) :
            elementCollector.Where(x => x.Name == name);

          element = enumerable.OfType<T>().FirstOrDefault();
        }
      }

      return element is object;
    }

    internal static IEnumerable<Element> GetNamesakeElements(this Document doc, Type type, string name, string parentName = default, BuiltInCategory? categoryId = default)
    {
      var enumerable = Enumerable.Empty<Element>();

      if (!string.IsNullOrEmpty(name))
      {
        if (typeof(ElementType).IsAssignableFrom(type))
        {
          enumerable = new FilteredElementCollector(doc).
          WhereElementIsKindOf(type).
          WhereCategoryIdEqualsTo(categoryId).
          WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, parentName).
          WhereParameterBeginsWith(BuiltInParameter.ALL_MODEL_TYPE_NAME, name);
        }
        else
        {
          var elementCollector = new FilteredElementCollector(doc).
          WhereElementIsKindOf(type).
          WhereCategoryIdEqualsTo(categoryId);

          var nameParameter = ElementExtension.GetNameParameter(type);
          enumerable = nameParameter != BuiltInParameter.INVALID ?
            elementCollector.WhereParameterBeginsWith(nameParameter, name) :
            elementCollector.Where(x => x.Name.StartsWith(name));
        }
      }

      return enumerable.
        // WhereElementIsKindOf sometimes is not enough.
        Where(x => type.IsAssignableFrom(x.GetType())).
        // Look for elements called "name" or "name 1" but not "name abc".
        Where
        (
          x =>
          {
            TryParseNameId(x.Name, out var prefix, out var _);
            return prefix == name;
          }
        );
    }

    internal static bool TryParseNameId(string name, out string prefix, out int id)
    {
      id = default;
      var index = name.LastIndexOf(' ');
      if (index >= 0)
      {
        if (int.TryParse(name.Substring(index + 1), out id))
        {
          prefix = name.Substring(0, index);
          return true;
        }
      }

      prefix = name;
      return false;
    }

    internal static int? GetIncrementalName(this Document doc, Type type, ref string name, string parentName = default, BuiltInCategory? categoryId = default)
    {
      if (name is null)
        throw new ArgumentNullException(nameof(name));

      if (name is null)
        throw new ArgumentNullException(nameof(name));

      if (!NamingUtils.IsValidName(name))
        throw new ArgumentException("Element name contains prohibited characters and is invalid.", nameof(name));

      // Remove number sufix from name and trailing spaces.
      TryParseNameId(name.Trim(), out name, out var _);

      var last = doc.GetNamesakeElements(type, name, parentName, categoryId).
        OrderBy(x => x.Name, default(ElementNameComparer)).LastOrDefault();

      if (last is object)
      {
        if (TryParseNameId(last.Name, out name, out var id))
          return id + 1;

        return 1;
      }

      return default;
    }

    internal static IEnumerable<string> WhereNamePrefixedWith(this IEnumerable<string> enumerable, string prefix)
    {
      foreach (var value in enumerable)
      {
        if (!value.StartsWith(prefix)) continue;

        TryParseNameId(value, out var name, out var _);
        if (name != prefix) continue;

        yield return value;
      }
    }

    internal static string NextNameOrDefault(this IEnumerable<string> enumerable)
    {
      var last = enumerable.OrderBy(x => x, default(ElementNameComparer)).LastOrDefault();

      if (last is object)
      {
        TryParseNameId(last, out var next, out var id);
        return $"{next} {id + 1}";
      }

      return default;
    }
    #endregion

    #region Category
    internal static Category AsCategory(Element element)
    {
      if (element?.GetType() == typeof(Element))
      {
        // 1. We try with the regular way calling Category.GetCategory
        try
        {
          if (Category.GetCategory(element.Document, element.Id) is Category category)
            return category;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

        // 2. Try looking for any GraphicsStyle that points to the Category we are looking for.
        if (element.GetFirstDependent<GraphicsStyle>() is GraphicsStyle style)
        {
          if (style.GraphicsStyleCategory.Id == element.Id)
            return style.GraphicsStyleCategory;
        }
      }

      return null;
    }

    /// <summary>
    /// Query all <see cref="Autodesk.Revit.DB.Category"/> objects in <paramref name="doc"/>
    /// that are sub categories of <paramref name="parentId"/> or
    /// root categories if <paramref name="parentId"/> is <see cref="Autodesk.Revit.DB.ElementId.InvalidElementId"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="parent"></param>
    /// <returns>An <see cref="ICollection{T}"/> of <see cref="Autodesk.Revit.DB.Category"/></returns>
    /// <remarks>This method will return all categories and sub categories if <paramref name="parentId"/> is null.</remarks>
    public static ICollection<Category> GetCategories(this Document doc, ElementId parentId = default)
    {
      using (var collector = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)))
      {
        var categories =
        (
          collector.Cast<GraphicsStyle>().
          Select(x => x.GraphicsStyleCategory).
          Where(x => x.Name != string.Empty)
        );

        if (parentId is object)
          categories = categories.Where(x => parentId == (x.Parent?.Id ?? ElementId.InvalidElementId));

        return new HashSet<Category>(categories, CategoryEqualityComparer.SameDocument);
      }
    }

    public static Category GetCategory(this Document doc, BuiltInCategory categoryId)
    {
      if (doc is null || categoryId == BuiltInCategory.INVALID)
        return default;

      // 1. We try with the regular way calling Category.GetCategory
      try
      {
        if (Category.GetCategory(doc, categoryId) is Category category)
          return category;
      }
      catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }

      // 2. Try looking for any GraphicsStyle that points to the Category we are looking for.
      using (var collector = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)))
      {
        foreach (var style in collector.Cast<GraphicsStyle>())
        {
          var category = style.GraphicsStyleCategory;
          if (category.Id.IntegerValue == (int) categoryId)
            return style.GraphicsStyleCategory;
        }
      }

      return default;
    }

    public static Category GetCategory(this Document doc, ElementId id)
    {
      if (doc is null || !id.IsValid())
        return default;

      return id.TryGetBuiltInCategory(out var categoryId) ?
        GetCategory(doc, categoryId) :
        AsCategory(doc.GetElement(id));
    }

    static BuiltInCategory[] BuiltInCategoriesWithParameters;
    static Document BuiltInCategoriesWithParametersDocument;
    internal static IReadOnlyCollection<BuiltInCategory> GetBuiltInCategoriesWithParameters(this Document doc)
    {
      if (BuiltInCategoriesWithParameters is null || !doc.Equals(BuiltInCategoriesWithParametersDocument))
      {
        BuiltInCategoriesWithParametersDocument = doc;
        BuiltInCategoriesWithParameters = BuiltInCategoryExtension.BuiltInCategories.Where
        (
          bic =>
          {
            try { return Category.GetCategory(doc, bic)?.AllowsBoundParameters == true; }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
          }
        ).
        ToArray();
      }

      return BuiltInCategoriesWithParameters;
    }
    #endregion

    #region Family
    public static bool TryGetFamily(this Document doc, string name, out Family family, ElementId clueCategoryId = default)
    {
      if (clueCategoryId.IsValid())
      {
        {
          // We use categoryId as a clue too speed up search.
          using (var smallSet = new ElementCategoryFilter(clueCategoryId, false))
          {
            using (var collector = new FilteredElementCollector(doc).OfClass(typeof(Family)).WherePasses(smallSet))
              family = collector.Where(x => x.Name == name).FirstOrDefault() as Family;
          }
        }

        if (family is null)
        {
          // We look into other categories that are not 'clueCategoryId'.
          using (var bigSet = new ElementCategoryFilter(clueCategoryId, true))
          {
            // We use categoryId as a clue too speed up search.
            using (var collector = new FilteredElementCollector(doc).OfClass(typeof(Family)).WherePasses(bigSet))
              family = collector.Where(x => x.Name == name).FirstOrDefault() as Family;
          }
        }
      }
      else
      {
        using (var collector = new FilteredElementCollector(doc).OfClass(typeof(Family)))
          family = collector.Where(x => x.Name == name).FirstOrDefault() as Family;
      }

      return family is object;
    }
    #endregion

    #region Level
    public static Level FindLevelByHeight(this Document doc, double height)
    {
      Level level = null;

      if (!double.IsNaN(height))
      {
        var min = double.PositiveInfinity;
        using (var collector = new FilteredElementCollector(doc))
        {
          foreach (var levelN in collector.OfClass(typeof(Level)).Cast<Level>().OrderBy(c => c.GetHeight()))
          {
            var distance = Math.Abs(levelN.GetHeight() - height);
            if (distance < min)
            {
              level = levelN;
              min = distance;
            }
          }
        }
      }

      return level;
    }

    public static Level FindBaseLevelByHeight(this Document doc, double height, out Level topLevel)
    {
      height += Revit.ShortCurveTolerance;

      topLevel = null;
      Level level = null;
      using (var collector = new FilteredElementCollector(doc))
      {
        foreach (var levelN in collector.OfClass(typeof(Level)).Cast<Level>().OrderBy(c => c.GetHeight()))
        {
          if (levelN.GetHeight() <= height) level = levelN;
          else
          {
            topLevel = levelN;
            break;
          }
        }
      }
      return level;
    }

    public static Level FindTopLevelByHeight(this Document doc, double height, out Level baseLevel)
    {
      height -= Revit.ShortCurveTolerance;

      baseLevel = null;
      Level level = null;
      using (var collector = new FilteredElementCollector(doc))
      {
        foreach (var levelN in collector.OfClass(typeof(Level)).Cast<Level>().OrderByDescending(c => c.GetHeight()))
        {
          if (levelN.GetHeight() >= height) level = levelN;
          else
          {
            baseLevel = levelN;
            break;
          }
        }
      }
      return level;
    }
    #endregion

    #region View
    /// <summary>
    /// Gets the active Graphical <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active graphical <see cref="Autodesk.Revit.DB.View"/></returns>
    /// <remarks>
    /// The active view is the view that last had focus in the UI. null if no view is considered active.
    /// </remarks>
    public static View GetActiveGraphicalView(this Document doc)
    {
      var active = doc.ActiveView;
      if (active?.ViewType.IsGraphicalViewType() == true)
        return active;

      return Rhinoceros.InvokeInHostContext(() =>
      {
        using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
        {
          var activeView = uiDocument.ActiveGraphicalView;

          if (activeView is null)
          {
            var openViews = uiDocument.GetOpenUIViews().
                Select(x => doc.GetElement(x.ViewId) as View).
                Where(x => x.ViewType.IsGraphicalViewType());

            activeView = openViews.FirstOrDefault();
          }

          return activeView;
        }
      });
    }

    /// <summary>
    /// Gets the active <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active <see cref="Autodesk.Revit.DB.View"/></returns>
    public static View GetActiveView(this Document doc) => Rhinoceros.InvokeInHostContext(() =>
    {
      using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
      {
        var activeView = uiDocument.ActiveView;

        if (activeView is null)
        {
          var openViews = uiDocument.GetOpenUIViews().
              Select(x => doc.GetElement(x.ViewId) as View);

          activeView = openViews.FirstOrDefault();
        }

        return activeView;
      }
    });
    #endregion

    #region ElementType
    static readonly Guid PurgePerformanceAdviserRuleId = new Guid("E8C63650-70B7-435A-9010-EC97660C1BDA");
    public static bool GetPurgableElementTypes(this Document doc, out ICollection<ElementId> purgableTypeIds)
    {
      try
      {
        using (var adviser = PerformanceAdviser.GetPerformanceAdviser())
        {
          var rules = adviser.GetAllRuleIds().Where(x => x.Guid == PurgePerformanceAdviserRuleId).ToList();
          if (rules.Count > 0)
          {
            var results = adviser.ExecuteRules(doc, rules);
            if (results.Count > 0)
            {
              purgableTypeIds = new HashSet<ElementId>(results[0].GetFailingElements());
              return true;
            }
          }
        }
      }
      catch (Autodesk.Revit.Exceptions.InternalException) { }

      purgableTypeIds = default;
      return false;
    }
    #endregion
  }

  public static class ModelPathExtension
  {
    /// <summary>
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.ModelPath"/>
    /// equals to this <see cref="Autodesk.Revit.DB.ModelPath"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Autodesk.Revit.DB.ModelPath"/> instances are considered equivalent
    /// if they represent the same target model file.
    /// </remarks>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsEquivalent(this ModelPath self, ModelPath other)
    {
      if (ReferenceEquals(self, other))
        return true;

      if (self is null || other is null)
        return false;

      return self.Compare(other) == 0;
    }

    /// <summary>
    /// Returns whether this path is a file path (as opposed to a server path or cloud path)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool IsFilePath(this ModelPath self)
    {
      return !self.ServerPath && !self.IsCloudPath();
    }

    /// <summary>
    /// Returns whether this path is a server path (as opposed to a file path or cloud path)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool IsServerPath(this ModelPath self)
    {
      return self.ServerPath && !self.IsCloudPath();
    }

    /// <summary>
    /// Returns whether this path is a cloud path (as opposed to a file path or server path)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool IsCloudPath(this ModelPath self)
    {
#if REVIT_2019
      return self.CloudPath;
#else
      return self.GetProjectGUID() != Guid.Empty && self.GetModelGUID() != Guid.Empty;
#endif
    }

    /// <summary>
    /// Returns the region of the cloud account and project which contains this model.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string GetRegion(this ModelPath self)
    {
      if (self.IsCloudPath())
      {
#if REVIT_2021
        return self.Region;
#else
        return "GLOBAL";
#endif
      }

      return default;
    }
  }

  public static class ModelUri
  {
    public const string UriSchemeServer = "RSN";
    public const string UriSchemeCloud = "cloud";
    internal static readonly Uri Empty = new Uri("empty:");

    public static Uri ToUri(this ModelPath modelPath)
    {
      if (modelPath is null)
        return default;

      if (modelPath.Empty)
        return ModelUri.Empty;

      if (modelPath.IsCloudPath())
      {
        return new UriBuilder(UriSchemeCloud, modelPath.GetRegion(), 0, $"{modelPath.GetProjectGUID():D}/{modelPath.GetModelGUID():D}").Uri;
      }
      else
      {
        var path = ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
          return uri;
      }

      throw new ArgumentException($"Failed to convert {nameof(ModelPath)} in to {nameof(Uri)}", nameof(modelPath));
    }

    public static ModelPath ToModelPath(this Uri uri)
    {
      if (uri is null)
        return default;

      if (uri.IsFile)
        return new FilePath(uri.LocalPath);

      if (IsServerUri(uri, out var centralServerLocation, out var path))
        return new ServerPath(centralServerLocation, path);

      if (IsCloudUri(uri, out var region, out var projectId, out var modelId))
      {
        return Rhinoceros.InvokeInHostContext(() =>
        {
          try
          {
#if REVIT_2021
            return ModelPathUtils.ConvertCloudGUIDsToCloudPath(region, projectId, modelId);
#elif REVIT_2019
            return ModelPathUtils.ConvertCloudGUIDsToCloudPath(projectId, modelId);
#else
            return default(ModelPath);
#endif
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException) { return default; }
        });
      }

      throw new ArgumentException($"Failed to convert {nameof(Uri)} in to {nameof(ModelPath)}", nameof(uri));
    }

    public static bool IsEmptyUri(this Uri uri)
    {
      return uri.Scheme.Equals(Empty.Scheme, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool IsFileUri(this Uri uri, out string path)
    {
      if (uri.IsFile)
      {
        path = uri.LocalPath;
        return true;
      }

      path = default;
      return false;
    }

    public static bool IsServerUri(this Uri uri, out string centralServerLocation, out string path)
    {
      if (uri.Scheme.Equals(UriSchemeServer, StringComparison.InvariantCultureIgnoreCase))
      {
        centralServerLocation = uri.Host;
        path = uri.AbsolutePath;
        return true;
      }

      centralServerLocation = default;
      path = default;
      return false;
    }

    public static bool IsCloudUri(this Uri uri, out string region, out Guid projectId, out Guid modelId)
    {
      if (uri.Scheme.Equals(UriSchemeCloud, StringComparison.InvariantCultureIgnoreCase))
      {
        var fragments = uri.AbsolutePath.Split('/');
        if (fragments.Length == 3)
        {
          if
          (
            fragments[0] == string.Empty &&
            Guid.TryParseExact(fragments[1], "D", out projectId) &&
            Guid.TryParseExact(fragments[2], "D", out modelId)
          )
          {
            if (uri.Host == string.Empty)
              region = "GLOBAL";
            else
              region = uri.Host.ToUpperInvariant();

            return true;
          }
        }
      }

      region = default;
      projectId = default;
      modelId = default;
      return false;
    }
  }
}
