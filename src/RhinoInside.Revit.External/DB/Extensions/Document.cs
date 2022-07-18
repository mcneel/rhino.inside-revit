using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using RhinoInside.Revit.External.UI;

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

      if (!self.IsValidObject || !other.IsValidObject)
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

    public static Guid? GetExportID(this Document doc)
    {
      if (doc?.IsValidObject != true)
        return default;

      return ExportUtils.GetGBXMLDocumentId(doc);
    }

    public static Guid? GetExportID(this Document document, ElementId id)
    {
      if (document?.IsValidObject != true || id is null)
        return default;

      return ExportUtils.GetExportId(document, id);
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

    #region Identity
    /// <summary>
    /// The document's name.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The file name of the document's disk file.</returns>
    /// <remarks>
    /// This method returns an non empty string even if the project has not been saved yet.
    /// </remarks>
    public static string GetName(this Document doc)
    {
      // Document.Title may contain the extension
      // on some Revit versions or depending on user settings.
      // To avoid the corner case where the file was called "Project.rvt.rvt",
      // we try first with the Document.PathName.
      return string.IsNullOrEmpty(doc.PathName) ?
        Path.GetFileNameWithoutExtension(doc.Title) + (doc.IsFamilyDocument ? ".rfa" : ".rvt") :
        Path.GetFileName(doc.PathName);
    }

    /// <summary>
    /// The document's title.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The file name of the document's disk file without extension.</returns>
    /// <remarks>
    /// This method returns an non empty string even if the project has not been saved yet.
    /// </remarks>
    public static string GetTitle(this Document doc)
    {
      var title = string.Empty;
      if (doc.IsWorkshared && doc.GetWorksharingCentralModelPath() is ModelPath centralPath)
        title = Path.GetFileNameWithoutExtension(centralPath.ToUserVisiblePath());

      if (string.IsNullOrEmpty(title))
        title = Path.GetFileNameWithoutExtension(doc.Title);

      if (doc.IsDetached && title.EndsWith("_detached"))
        title = title.Substring(0, title.Length - "_detached".Length);        

      return title;
    }
    #endregion

    #region File
    /// <summary>
    /// The fully qualified path of the document's local disk file.
    /// If <paramref name="doc"/> is still not saved this method returns null.
    /// </summary>
    /// <remarks>
    /// On workshared documents returns the local copy path.
    /// </remarks>
    /// <param name="doc"></param>
    /// <returns>The path or null if still not saved.</returns>
    public static string GetPathName(this Document doc)
    {
      var pathName = doc.PathName;

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
      if (string.IsNullOrEmpty(pathName) || doc.IsDetached)
        return default;

#if REVIT_2019
      if (doc.IsModelInCloud)
      {
        if (doc.GetCloudModelPath() is ModelPath cloudPath)
        {
          using (cloudPath)
          {
            var projectGUID = cloudPath.GetProjectGUID();
            var modelGUID = cloudPath.GetModelGUID();
            if (projectGUID == Guid.Empty || modelGUID == Guid.Empty)
              return default;

            using (var app = doc.Application)
            {
              pathName = System.IO.Path.Combine
              (
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Autodesk", "Revit", app.VersionName,
                "CollaborationCache", app.LoginUserId,
                projectGUID.ToString(), $"{modelGUID}{System.IO.Path.GetExtension(pathName)}"
              );
            }
          }
        }
        else return default;
      }
#endif

      return File.Exists(pathName) ? pathName : default;
    }

    /// <summary>
    /// Gets the document local model path.
    /// If <paramref name="doc"/> is still not saved this method returns null.
    /// </summary>
    /// <remarks>
    /// On workshared documents returns the local copy path.
    /// </remarks>
    /// <param name="doc"></param>
    /// <returns>The model path or null if still not saved.</returns>
    public static ModelPath GetLocalModelPath(this Document doc)
    {
      return doc.GetPathName() is string pathName ?
        ModelPathUtils.ConvertUserVisiblePathToModelPath(pathName) :
        default;
    }

    /// <summary>
    /// Gets the document model path.
    /// If <paramref name="doc"/> is still not saved this method returns null.
    /// </summary>
    /// <remarks>
    /// On workshared documents returns the central model path else a local path.
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

      if (doc.IsWorkshared)
        return doc.GetWorksharingCentralModelPath();

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
        if (EpisodeId == ExportUtils.GetGBXMLDocumentId(doc))
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
        if (EpisodeId == ExportUtils.GetGBXMLDocumentId(doc))
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
        if (EpisodeId == ExportUtils.GetGBXMLDocumentId(doc))
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

      if (UniqueId.TryParse(uniqueId, out var EpisodeId, out var id) && EpisodeId == ExportUtils.GetGBXMLDocumentId(doc))
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

    #region Nomen
    internal static bool TryGetElement<T>(this Document doc, out T element, string nomen, string parentName = default, BuiltInCategory? categoryId = default) where T : Element
    {
      var nomenParameter = ElementExtension.GetNomenParameter(typeof(T));

      if (typeof(ElementType).IsAssignableFrom(typeof(T)))
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          element = collector.
            WhereElementIsElementType().
            WhereCategoryIdEqualsTo(categoryId).
            WhereElementIsKindOf(typeof(T)).
            WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, parentName).
            WhereParameterEqualsTo(nomenParameter, nomen).
            Cast<ElementType>().
            Where(x => x.FamilyName.Equals(parentName, ElementNaming.ComparisonType)).
            OfType<T>().
            FirstOrDefault(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType));
        }
      }
      else if (typeof(View).IsAssignableFrom(typeof(T)))
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          var elementCollector = collector.
            WhereElementIsNotElementType().
            WhereCategoryIdEqualsTo(categoryId).
            WhereElementIsKindOf(typeof(T));

          var enumerable = nomenParameter != BuiltInParameter.INVALID ?
            elementCollector.WhereParameterEqualsTo(nomenParameter, nomen) :
            elementCollector;

          element = enumerable.Cast<View>().
            Where(x => !x.IsTemplate && x.ViewType.ToString() == parentName).
            OfType<T>().
            FirstOrDefault(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType));
        }
      }
      else if (typeof(AppearanceAssetElement).IsAssignableFrom(typeof(T)))
      {
        element = nomen is object ? AppearanceAssetElement.GetAppearanceAssetElementByName(doc, nomen) as T : default;
      }
      else
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          var elementCollector = collector.
            WhereElementIsNotElementType().
            WhereCategoryIdEqualsTo(categoryId).
            WhereElementIsKindOf(typeof(T));

          var enumerable = nomenParameter != BuiltInParameter.INVALID ?
            elementCollector.WhereParameterEqualsTo(nomenParameter, nomen) :
            elementCollector;

          element = enumerable.
            OfType<T>().
            FirstOrDefault(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType));
        }
      }

      return element is object;
    }

    public static string NextIncrementalNomen(this Document document, string name, Type type, string parentName = default, BuiltInCategory? categoryId = default)
    {
      TryParseNomenId(name, out var prefix, out var _);
      return document.GetNamesakeElements
      (
        prefix, type, parentName, categoryId
      ).
      Select(x => x.Name).
      WhereNomenPrefixedWith(prefix).
      NextNomenOrDefault() ?? $"{prefix} 1";
    }

    internal static bool TryParseNomenId(string name, out string prefix, out int id)
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

    internal static int? GetIncrementalNomen(this Document doc, Type type, ref string nomen, string parentName = default, BuiltInCategory? categoryId = default)
    {
      if (nomen is null)
        throw new ArgumentNullException(nameof(nomen));

      if (nomen is null)
        throw new ArgumentNullException(nameof(nomen));

      if (!ElementNaming.IsValidName(nomen))
        throw new ArgumentException($"The name cannot contain these prohibited characters {string.Join(" ", ElementNaming.InvalidCharacters.ToCharArray())}", nameof(nomen));

      // Remove number sufix from name and trailing spaces.
      TryParseNomenId(nomen.Trim(), out nomen, out var _);

      var last = doc.GetNamesakeElements(nomen, type, parentName, categoryId).
        OrderBy(x => x.GetElementNomen(), ElementNaming.NameComparer).LastOrDefault();

      if (last is object)
      {
        if (TryParseNomenId(last.GetElementNomen(), out nomen, out var id))
          return id + 1;

        return 1;
      }

      return default;
    }

    internal static IEnumerable<string> WhereNomenPrefixedWith(this IEnumerable<string> enumerable, string prefix)
    {
      TryParseNomenId(prefix, out prefix, out var _);

      foreach (var value in enumerable)
      {
        if (!value.StartsWith(prefix, ElementNaming.ComparisonType)) continue;

        TryParseNomenId(value, out var name, out var _);
        if (!name.Equals(prefix, ElementNaming.ComparisonType)) continue;

        yield return value;
      }
    }

    internal static string NextNomenOrDefault(this IEnumerable<string> enumerable)
    {
      var last = enumerable.OrderBy(x => x, ElementNaming.NameComparer).LastOrDefault();

      if (last is object)
      {
        TryParseNomenId(last, out var next, out var id);
        return $"{next} {id + 1}";
      }

      return default;
    }

    internal static IEnumerable<Element> GetNamesakeElements(this Document doc, string name, Type type, string parentName = default, BuiltInCategory? categoryId = default)
    {
      var enumerable = Enumerable.Empty<Element>();

      if (string.IsNullOrWhiteSpace(name))
        return enumerable;

      var nomenParameter = ElementExtension.GetNomenParameter(type);

      if (typeof(ElementType).IsAssignableFrom(type))
      {
        enumerable = new FilteredElementCollector(doc).
        WhereElementIsElementType().
        WhereCategoryIdEqualsTo(categoryId).
        WhereElementIsKindOf(type).
        WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, parentName).
        WhereParameterBeginsWith(nomenParameter, name).
        Cast<ElementType>().
        Where(x => x.FamilyName.Equals(parentName, ElementNaming.ComparisonType));
      }
      else if (typeof(View).IsAssignableFrom(type))
      {
        enumerable = new FilteredElementCollector(doc).
        WhereElementIsNotElementType().
        WhereCategoryIdEqualsTo(categoryId).
        WhereElementIsKindOf(type).
        WhereParameterBeginsWith(nomenParameter, name).
        Cast<View>().
        Where(x => !x.IsTemplate && x.ViewType.ToString() == parentName);
      }
      else
      {
        var elementCollector = new FilteredElementCollector(doc).
        WhereElementIsNotElementType().
        WhereCategoryIdEqualsTo(categoryId).
        WhereElementIsKindOf(type);

        enumerable = nomenParameter != BuiltInParameter.INVALID ?
          elementCollector.WhereParameterBeginsWith(nomenParameter, name) :
          elementCollector;
      }

      return enumerable.
        // WhereElementIsKindOf sometimes is not enough.
        Where(x => type.IsAssignableFrom(x.GetType())).
        // Look for elements called "name" or "name 1" but not "name abc" or "Name 1".
        Where
        (
          x =>
          {
            TryParseNomenId(x.GetElementNomen(nomenParameter), out var prefix, out var _);
            return prefix.Equals(name, ElementNaming.ComparisonType);
          }
        );
    }

    internal static ElementId LookupElement(this Document target, Document source, ElementId elementId)
    {
      if (elementId.IsBuiltInId() || target.IsEquivalent(source))
        return elementId;

      if (source.GetElement(elementId) is Element element)
      {
        var nomen = element.GetElementNomen(out var nomenParameter);

        if (element is ElementType type)
        {
          using (var collector = new FilteredElementCollector(target))
          {
            return collector.WhereElementIsElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementId.InvalidElementId).
              WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, type.FamilyName).
              WhereParameterEqualsTo(nomenParameter, nomen).
              Cast<ElementType>().
              Where(x => x.FamilyName.Equals(type.FamilyName, ElementNaming.ComparisonType)).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementId.InvalidElementId;
          }
        }
        else if (element is View view)
        {
          using (var collector = new FilteredElementCollector(target))
          {
            return collector.WhereElementIsElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementId.InvalidElementId).
              WhereParameterEqualsTo(nomenParameter, nomen).
              Cast<View>().
              Where(x => x.IsTemplate == view.IsTemplate).
              Where(x => x.ViewType == view.ViewType).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementId.InvalidElementId;
          }
        }
        else if (element is SharedParameterElement sharedParameter)
        {
          return SharedParameterElement.Lookup(target, sharedParameter.GuidValue)?.Id ?? ElementId.InvalidElementId;
        }
        else if (element is AppearanceAssetElement asset)
        {
          return AppearanceAssetElement.GetAppearanceAssetElementByName(target, asset.Name)?.Id ?? ElementId.InvalidElementId;
        }
        else
        {
          using (var collector = new FilteredElementCollector(target))
          {
            if (nomenParameter != BuiltInParameter.INVALID)
            {
              return collector.WhereElementIsNotElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementId.InvalidElementId).
              WhereParameterEqualsTo(nomenParameter, nomen).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementId.InvalidElementId;
            }
            else
            {
              return collector.WhereElementIsNotElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementId.InvalidElementId).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementId.InvalidElementId;
            }
          }
        }
      }

      return ElementId.InvalidElementId;
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
          Where(x => x.Id != ElementId.InvalidElementId && x.Name != string.Empty)
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
      if (BuiltInCategoriesWithParametersDocument?.IsValidObject != true || !doc.IsEquivalent(BuiltInCategoriesWithParametersDocument))
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

    #region Parameters
    public static IEnumerable<(InternalDefinition Definition, ParameterScope Scope)> GetParameterDefinitions(this Document doc, ParameterScope scope)
    {
      if (scope.HasFlag(ParameterScope.Instance) || scope.HasFlag(ParameterScope.Type))
      {
        if (doc.IsFamilyDocument)
        {
          foreach (var parameter in doc.FamilyManager.Parameters.Cast<FamilyParameter>())
          {
            if (parameter.Definition is InternalDefinition definition)
            {
              var bindingScope = parameter.IsInstance ? ParameterScope.Instance : ParameterScope.Type;
              if (scope.HasFlag(bindingScope))
                yield return (definition, bindingScope);
            }
          }
        }
        else
        {
          using (var iterator = doc.ParameterBindings.ForwardIterator())
          {
            while (iterator.MoveNext())
            {
              if (iterator.Key is InternalDefinition definition)
              {
                var bindingScope = ParameterScope.Unknown;
                bindingScope |= iterator.Current is InstanceBinding ? ParameterScope.Instance : ParameterScope.Unknown;
                bindingScope |= iterator.Current is TypeBinding ?     ParameterScope.Type     : ParameterScope.Unknown;

                if (scope.HasFlag(bindingScope))
                  yield return (definition, bindingScope);
              }
            }
          }
        }
      }

      if (scope.HasFlag(ParameterScope.Global) && GlobalParametersManager.AreGlobalParametersAllowed(doc))
      {
        foreach (var id in GlobalParametersManager.GetAllGlobalParameters(doc))
        {
          if (doc.GetElement(id) is GlobalParameter parameter)
          {
            if (parameter.GetDefinition() is InternalDefinition definition)
            {
              yield return (definition, ParameterScope.Global);
            }
          }
        }
      }
    }

    public static bool TryGetParameter(this Document doc, out ParameterElement parameterElement, string parameterName, ParameterScope scope)
    {
      var (definition, _) = doc.GetParameterDefinitions(scope).Where(x => x.Definition.Name == parameterName).FirstOrDefault();
      parameterElement = doc.GetElement(definition?.Id ?? ElementId.InvalidElementId) as ParameterElement;
      return parameterElement is object;
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
    public static Level GetClosestLevel(this Document doc, double elevationAboveOrigin, int direction = 0)
    {
      if (direction == 0) return GetNearestLevel(doc, elevationAboveOrigin);
      if (direction  < 0) return GetNearestBaseLevel(doc, elevationAboveOrigin, out var _);
      if (direction  > 0) return GetNearestTopLevel(doc, elevationAboveOrigin, out var _);

      return default;
    }

    public static Level GetNearestLevel(this Document doc, double elevationAboveOrigin)
    {
      Level level = null;

      if (!double.IsNaN(elevationAboveOrigin))
      {
        var min = double.PositiveInfinity;
        using (var collector = new FilteredElementCollector(doc).OfClass(typeof(Level)))
        {
          foreach (var levelN in collector.Cast<Level>().OrderBy(LevelExtension.GetElevation))
          {
            var distance = Math.Abs(levelN.GetElevation() - elevationAboveOrigin);
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

    public static Level GetNearestBaseLevel(this Document doc, double elevationAboveOrigin, out Level topLevel, ElementFilter filter = default)
    {
      elevationAboveOrigin += doc.Application.ShortCurveTolerance;

      topLevel = null;
      Level level = null;
      using (var collector = new FilteredElementCollector(doc).OfClass(typeof(Level)))
      {
        var levelCollector = filter is object ? collector.WherePasses(filter) : collector;
        foreach (var levelN in levelCollector.Cast<Level>().OrderBy(LevelExtension.GetElevation))
        {
          if (levelN.GetElevation() <= elevationAboveOrigin) level = levelN;
          else
          {
            topLevel = levelN;
            break;
          }
        }
      }

      return level;
    }

    public static Level GetNearestTopLevel(this Document doc, double elevationAboveOrigin, out Level baseLevel, ElementFilter filter = default)
    {
      elevationAboveOrigin -= doc.Application.ShortCurveTolerance;

      baseLevel = null;
      Level level = null;
      using (var collector = new FilteredElementCollector(doc).OfClass(typeof(Level)))
      {
        var levelCollector = filter is object ? collector.WherePasses(filter) : collector;
        foreach (var levelN in levelCollector.Cast<Level>().OrderByDescending(LevelExtension.GetElevation))
        {
          if (levelN.GetElevation() >= elevationAboveOrigin) level = levelN;
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
    /// Gets the default <see cref="Autodesk.Revit.DB.View3D"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="username">View user name. Use <see cref="default"/> to query using the current session user name.</param>
    /// <returns>The default <see cref="Autodesk.Revit.DB.View3D"/> or null if no default 3D view is found.</returns>
    public static View3D GetDefault3DView(this Document doc, string username = default)
    {
      username = username ?? (doc.IsWorkshared ? doc.Application.Username : string.Empty);
      var viewName = string.IsNullOrEmpty(username) ? "{3D}" : $"{{3D - {username}}}";

      using (var collector = new FilteredElementCollector(doc).OfClass(typeof(View3D)))
      {
        return collector.
          WhereParameterEqualsTo(BuiltInParameter.VIEW_NAME, viewName).
          OfType<View3D>().Where(x => !x.IsTemplate && x.Name.Equals(viewName, ElementNaming.ComparisonType)).
          FirstOrDefault();
      }
    }

    /// <summary>
    /// Gets the active Graphical <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active graphical <see cref="Autodesk.Revit.DB.View"/> or null if no view is considered active.</returns>
    /// <remarks>
    /// The active view is the last view of the provided document that had the focus in the UI.
    /// </remarks>
    public static View GetActiveGraphicalView(this Document doc)
    {
      var active = doc.ActiveView;
      if (active.IsGraphicalView())
        return active;

      return HostedApplication.Active.InvokeInHostContext(() =>
      {
        using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
        {
          var activeView = uiDocument.ActiveGraphicalView;

          if (activeView is null)
          {
            var openViews = uiDocument.GetOpenUIViews().
                Select(x => doc.GetElement(x.ViewId) as View).
                Where(x => x.IsGraphicalView());

            activeView = openViews.FirstOrDefault();
          }

          return activeView;
        }
      });
    }

    /// <summary>
    /// Sets the active Graphical <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="view">View to be activated</param>
    public static bool SetActiveGraphicalView(this Document document, View view) =>
      SetActiveGraphicalView(document, view, out var _);

    /// <summary>
    /// Sets the active Graphical <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="view">View to be activated</param>
    public static bool SetActiveGraphicalView(this Document document, View view, out bool wasOpened)
    {
      if (view is null)
        throw new ArgumentNullException(nameof(view));

      if (!view.IsGraphicalView())
        throw new ArgumentException("Input view is not a graphical view.", nameof(view));

      if (!document.Equals(view.Document))
        throw new ArgumentException("View does not belong to the specified document", nameof(view));

      if (document.IsModifiable || document.IsReadOnly)
        throw new InvalidOperationException("Invalid document state.");

      using (var uiDocument = new Autodesk.Revit.UI.UIDocument(document))
      {
        var openViews = uiDocument.GetOpenUIViews();
        if (openViews.Count == 0)
          throw new InvalidOperationException("Input view document is not open on the Revit UI");

        wasOpened = openViews.Any(x => x.ViewId == view.Id);

        var activeUIDocument = uiDocument.Application.ActiveUIDocument;
        if (activeUIDocument is null)
          throw new InvalidOperationException("There are no documents opened on the Revit UI");

        if (!document.IsEquivalent(activeUIDocument.Document))
        {
          // This method may fail if the view is empty, or due to the BUG if its Id coincides
          // with the active one from an other document, also modifies the Zoom on the view.
          // 
          //// 1. We use `UIDocument.ShowElements` on the target view to activate its document.
          ////
          //// Looks like Revit `UIDocument.ShowElements` has a bug comparing with the current view.
          //// If the ElementId or UniqueId coincides it does not change the active document.
          //if (activeUIDocument.ActiveView.Id != view.Id && activeUIDocument.ActiveView.UniqueId != view.UniqueId)
          //{
          //  // Some filters are added to the `ARDB.FilteredElementCollector` to avoid the
          //  // 'No valid view is found' message from Revit.
          //  using
          //  (
          //    var collector = new FilteredElementCollector(document, view.Id).
          //    WherePasses(new ElementCategoryFilter(ElementId.InvalidElementId, inverted: true)).
          //    WherePasses(External.DB.CompoundElementFilter.ElementHasBoundingBoxFilter)
          //  )
          //  {
          //    var elements = collector.ToElementIds();
          //    if (elements.Count > 0)
          //    {
          //      uiDocument.ShowElements(elements);
          //      return true;
          //    }

          //    // Continue with the alternative method when the view is completly empty.
          //  }
          //}

          // 2. Alternative method is less performant but aims to work on any case
          // without altering the view zoom level.
          using (var group = new TransactionGroup(document))
          {
            group.IsFailureHandlingForcedModal = true;

            if (group.Start("Activate View") == TransactionStatus.Started)
            {
              var textNoteId = default(ElementId);
              using (var tx = new Transaction(document, "Activate Document"))
              {
                if (tx.Start() == TransactionStatus.Started)
                {
                  // We create an EMPTY sheet because it does not show any model element.
                  // Hopefully will be fast enough.
                  var sheet = ViewSheet.Create(view.Document, ElementId.InvalidElementId);
                  var typeId = document.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                  textNoteId = TextNote.Create(document, sheet.Id, XYZ.Zero, "Show me!!", typeId).Id;

                  var options = tx.GetFailureHandlingOptions().
                                   SetClearAfterRollback(true).
                                   SetDelayedMiniWarnings(false).
                                   SetForcedModalHandling(true).
                                   SetFailuresPreprocessor(FailuresPreprocessor.NoErrors);

                  if (tx.Commit(options) != TransactionStatus.Committed)
                    return false;
                }
              }

              // Since the new View is not already open `UIDocument.ShowElements` asks the user
              // to look for that view. We press OK here.
              var activeWindow = Microsoft.Win32.SafeHandles.WindowHandle.ActiveWindow;
              void PressOK(object sender, Autodesk.Revit.UI.Events.DialogBoxShowingEventArgs args) =>
                args.OverrideResult((int) Microsoft.Win32.SafeHandles.DialogResult.IDOK);

              uiDocument.Application.DialogBoxShowing += PressOK;
              uiDocument.ShowElements(textNoteId);
              uiDocument.Application.DialogBoxShowing -= PressOK;
              Microsoft.Win32.SafeHandles.WindowHandle.ActiveWindow = activeWindow;

              uiDocument.ActiveView = view;
              group.RollBack();
            }
          }
        }
        else if (uiDocument.ActiveView.Id != view.Id)
        {
          uiDocument.ActiveView = view;
        }

        return true;
      }
    }

    /// <summary>
    /// Gets the active <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active <see cref="Autodesk.Revit.DB.View"/></returns>
    public static View GetActiveView(this Document doc)
    {
      var active = doc.ActiveView;
      if (active is object)
        return active;

      return HostedApplication.Active.InvokeInHostContext(() =>
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
    }
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

    #region Delete
    /// <summary>
    /// Indicates if a collection of elements can be deleted.
    /// </summary>
    /// <param name="doc">The document.</param>
    /// <param name="ids">The collection od ids to check.</param>
    /// <returns>True if all elements can be deleted, false otherwise.</returns>
    public static bool CanDeleteElements(this Document doc, ICollection<ElementId> ids)
    {
      foreach(var id in ids)
      {
        if (!DocumentValidation.CanDeleteElement(doc, id))
          return false;

        if (doc.GetElement(id) is Element element)
        {
          switch(element)
          {
            // `DocumentValidation.CanDeleteElement` return true on 'Gross Building Area'.
            // UI crashes if we delete it.
            case AreaScheme scheme:
              return !scheme.IsGrossBuildingArea;
          }
        }
        else return false;
      }

      return true;
    }
    #endregion
  }
}
