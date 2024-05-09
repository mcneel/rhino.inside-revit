using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public static Guid GetCreationGUID(this Document doc)
    {
#if REVIT_2024
      return doc.CreationGUID;
#else
      return ExportUtils.GetGBXMLDocumentId(doc);
#endif
    }

    internal static Guid GetPersistentGUID(this Document doc)
    {
      if (doc?.IsValidObject != true)
        return Guid.Empty;

      return GetCreationGUID(doc);
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
        if (activeDBDocument is object && activeDBDocument.GetPersistentGUID() == guid)
        {
          document = activeDBDocument;
          return true;
        }

        foreach (var doc in set.Where(x => x.GetPersistentGUID() == guid))
        {
          document = doc;
          return true;
        }
      }

      document = default;
      return false;
    }

    public static bool TryGetLinkElementId(this Document doc, string persistent, out LinkElementId linkElementId)
    {
      if (ReferenceId.TryParse(persistent, out var reference, doc))
      {
        linkElementId = reference.IsLinked ?
          new LinkElementId(new ElementId(reference.Record.Id), new ElementId(reference.Element.Id)) :
          new LinkElementId(new ElementId(reference.Record.Id));

        return true;
      }

      linkElementId = default;
      return false;
    }

    public static T GetElement<T>(this Document doc, ElementId elementId) where T : Element
    {
      return doc.GetElement(elementId) as T;
    }

    public static T GetElement<T>(this Document doc, Reference reference) where T : Element
    {
      return doc.GetElement(reference) as T;
    }

    public static Element GetElement(this Document doc, ElementId elementId, ElementId linkedElementId)
    {
      var element = doc.GetElement(elementId);
      if (linkedElementId == ElementIdExtension.Invalid) return element;
      if (element is RevitLinkInstance link)
        return link.GetLinkDocument()?.GetElement(linkedElementId);

      return null;
    }

    public static GeometryObject GetGeometryObjectFromReference(this Document doc, Reference reference, out Transform transform)
    {
      transform = null;

      var element = doc.GetElement(reference.ElementId);
      if (element is RevitLinkInstance link)
      {
        transform = link.GetTransform();
        element = link.GetLinkDocument()?.GetElement(reference.LinkedElementId);
        reference = reference.CreateReferenceInLink(link);
      }

      if (element?.GetGeometryObjectFromReference(reference) is GeometryObject geometryObject)
      {
        if (element is Instance instance)
          transform = transform is object ? transform * instance.GetTransform() : instance.GetTransform();
        else
          transform = Transform.Identity;

        return geometryObject;
      }
      else transform = null;

      return null;
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
      if (type is null)
        throw new ArgumentNullException(nameof(type));

      if (nomen is null)
        throw new ArgumentNullException(nameof(nomen));

      if (!ElementNaming.IsValidName(nomen))
        throw new ArgumentException($"The name cannot contain these prohibited characters {string.Join(" ", ElementNaming.InvalidCharacters.ToCharArray())}", nameof(nomen));

      // Remove number sufix from name and trailing spaces.
      TryParseNomenId(nomen.Trim(), out nomen, out var _);

      var last = doc.GetNamesakeElements(nomen, type, parentName, categoryId).
        OrderBy(ElementExtension.GetElementNomen, ElementNaming.NameComparer).LastOrDefault();

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

    internal static IList<Element> GetNamesakeElements(this Document doc, string name, Type type, string parentName = default, BuiltInCategory? categoryId = default)
    {
      var enumerable = Enumerable.Empty<Element>();

      if (string.IsNullOrWhiteSpace(name))
        return enumerable.ToList();

      var nomenParameter = ElementExtension.GetNomenParameter(type);
      using (var elementCollector = new FilteredElementCollector(doc))
      {
        var isElementType = typeof(ElementType).IsAssignableFrom(type);
        var collector =
          (isElementType ? elementCollector.WhereElementIsElementType() : elementCollector.WhereElementIsNotElementType()).
          WhereCategoryIdEqualsTo(categoryId).
          WhereElementIsKindOf(type);

        if(nomenParameter != BuiltInParameter.INVALID)
          collector = collector.WhereParameterBeginsWith(nomenParameter, name);

        if (string.IsNullOrWhiteSpace(parentName))
        {
          enumerable = collector;
        }
        else
        {
          if (isElementType)
          {
            enumerable = collector.
              WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, parentName).
              Cast<ElementType>().Where(x => x.FamilyName.Equals(parentName, ElementNaming.ComparisonType));
          }
          else if (typeof(View).IsAssignableFrom(type))
          {
            if (Enum.TryParse(parentName, out ViewType viewType))
            {
              enumerable = collector.
                Cast<View>().Where(x => !x.IsTemplate && x.ViewType == viewType);
            }
          }
          else if (typeof(FillPatternElement).IsAssignableFrom(type))
          {
            if (Enum.TryParse(parentName, out FillPatternTarget target))
            {
              enumerable = collector.Cast<FillPatternElement>().Where
              (
                x =>
                {
                  using (var pattern = x.GetFillPattern())
                    return pattern.Target == target;
                }
              );
            }
          }
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
          ).
          ToList();
      }
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
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementIdExtension.Invalid).
              WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, type.FamilyName).
              WhereParameterEqualsTo(nomenParameter, nomen).
              Cast<ElementType>().
              Where(x => x.FamilyName.Equals(type.FamilyName, ElementNaming.ComparisonType)).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementIdExtension.Invalid;
          }
        }
        else if (element is View view)
        {
          using (var collector = new FilteredElementCollector(target))
          {
            return collector.WhereElementIsElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementIdExtension.Invalid).
              WhereParameterEqualsTo(nomenParameter, nomen).
              Cast<View>().
              Where(x => x.IsTemplate == view.IsTemplate).
              Where(x => x.ViewType == view.ViewType).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementIdExtension.Invalid;
          }
        }
        else if (element is SharedParameterElement sharedParameter)
        {
          return SharedParameterElement.Lookup(target, sharedParameter.GuidValue)?.Id ?? ElementIdExtension.Invalid;
        }
        else if (element is AppearanceAssetElement asset)
        {
          return AppearanceAssetElement.GetAppearanceAssetElementByName(target, asset.Name)?.Id ?? ElementIdExtension.Invalid;
        }
        else if (element is FillPatternElement fillPattern)
        {
          using (var pattern = fillPattern.GetFillPattern())
            return FillPatternElement.GetFillPatternElementByName(target, pattern.Target, fillPattern.Name)?.Id ?? ElementIdExtension.Invalid;
        }
        else
        {
          using (var collector = new FilteredElementCollector(target))
          {
            if (nomenParameter != BuiltInParameter.INVALID)
            {
              return collector.WhereElementIsNotElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementIdExtension.Invalid).
              WhereParameterEqualsTo(nomenParameter, nomen).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementIdExtension.Invalid;
            }
            else if (element is Family || element is ParameterElement || element.CanBeRenominated())
            {
              return collector.WhereElementIsNotElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementIdExtension.Invalid).
              Where(x => x.GetElementNomen(nomenParameter).Equals(nomen, ElementNaming.ComparisonType)).
              Select(x => x.Id).
              FirstOrDefault() ?? ElementIdExtension.Invalid;
            }
          }
        }
      }

      return ElementIdExtension.Invalid;
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
          Where(x => x.Id != ElementIdExtension.Invalid && x.Name != string.Empty)
        );

        if (parentId is object)
          categories = categories.Where(x => parentId == (x.Parent?.Id ?? ElementIdExtension.Invalid));

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
      catch { }

      // 2. Try looking for any GraphicsStyle that points to the Category we are looking for.
      using (var collector = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)))
      {
        foreach (var style in collector.ToElements().Cast<GraphicsStyle>())
        {
          var category = style.GraphicsStyleCategory;
          if (category.ToBuiltInCategory() == categoryId)
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
            catch { return false; }
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
      var (definition, _) = doc.GetParameterDefinitions(scope).FirstOrDefault(x => x.Definition.Name == parameterName);
      parameterElement = doc.GetElement(definition?.Id ?? ElementIdExtension.Invalid) as ParameterElement;
      return parameterElement is object;
    }
    #endregion

    #region Family
    public static bool TryGetFamily(this Document doc, string familyName, out Family family, ElementId clueCategoryId = default)
    {
      family = null;

      if (clueCategoryId.IsValid())
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          family = collector.OfClass(typeof(FamilySymbol)).
            WhereElementIsElementType().
            WhereCategoryIdEqualsTo(clueCategoryId).
            WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, familyName).
            Cast<FamilySymbol>().
            FirstOrDefault(x => ElementNaming.NameEqualityComparer.Equals(x.FamilyName, familyName))?. // To enfoce name casing.
            Family;
        }
      }

      // In case is not in `clueCategoryId`
      if (family is null)
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          family = collector.OfClass(typeof(FamilySymbol)).
            WhereElementIsElementType().
            WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, familyName).
            Cast<FamilySymbol>().
            FirstOrDefault(x => ElementNaming.NameEqualityComparer.Equals(x.FamilyName, familyName))?. // To enfoce name casing.
            Family;
        }
      }

      // In case Family does not have any type
      if (family is null)
      {
        using (var collector = new FilteredElementCollector(doc))
        {
          family = collector.OfClass(typeof(Family)).
            FirstOrDefault(x => ElementNaming.NameEqualityComparer.Equals(x.Name, familyName)) as
            Family;
        }
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
          OfType<View3D>().
          FirstOrDefault(x => !x.IsTemplate && x.Name.Equals(viewName, ElementNaming.ComparisonType));
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
          var activeView = uiDocument.ActiveGraphicalView ??
              uiDocument.
              GetOpenUIViews().
              Select(x => doc.GetElement(x.ViewId) as View).
              FirstOrDefault(ViewExtension.IsGraphicalView);

          return activeView;
        }
      });
    }

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

      return SetActiveView(document, view, out wasOpened);
    }

    /// <summary>
    /// Gets the active <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The active <see cref="Autodesk.Revit.DB.View"/> or null if no view is considered active.</returns>
    /// <remarks>
    /// The active view is the last view of the provided document that had the focus in the UI.
    /// </remarks>
    public static View GetActiveView(this Document doc)
    {
      var active = doc.ActiveView;
      if (active is object)
        return active;

      return HostedApplication.Active.InvokeInHostContext(() =>
      {
        using (var uiDocument = new Autodesk.Revit.UI.UIDocument(doc))
        {
          var activeView = uiDocument.ActiveView ??
              uiDocument.
              GetOpenUIViews().
              Select(x => doc.GetElement(x.ViewId) as View).
              FirstOrDefault();

          return activeView;
        }
      });
    }

    /// <summary>
    /// Sets the active <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="view">View to be activated</param>
    public static bool SetActiveView(this Document document, View view)
    {
      if (view is null)
        throw new ArgumentNullException(nameof(view));

      return SetActiveView(document, view, out var _);
    }

    /// <summary>
    /// Sets the active Graphical <see cref="Autodesk.Revit.DB.View"/> of the provided <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="view">View to be activated</param>
    private static bool SetActiveView(this Document document, View view, out bool wasOpened)
    {
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
                  // We create an EMPTY drafting view because it does not show any model element.
                  // Hopefully will be fast enough.
                  var drafting = ViewDrafting.Create(document, document.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeDrafting));
                  var typeId = document.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                  textNoteId = TextNote.Create(document, drafting.Id, XYZExtension.Zero, "Show me!!", typeId).Id;

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

    #region Create
    internal static Autodesk.Revit.Creation.ItemFactoryBase Create(this Document document)
    {
      return document.IsFamilyDocument ? (Autodesk.Revit.Creation.ItemFactoryBase) document.FamilyCreate : (Autodesk.Revit.Creation.ItemFactoryBase) document.Create;
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

    internal static ReadOnlyElementIdSet DeleteElements<T>(this Document doc, ICollection<T> elements) where T : Element
    {
      if (!doc.IsModifiable || elements.Count == 0)
      {
        // This should quickly return or throw the appropiate exception.
        return doc.Delete(Array.Empty<ElementId>()).AsReadOnlyElementIdSet();
      }

      var elementIds = new List<ElementId>(elements.Count);

      // Linked Document should have ActiveView to null, so no need to check here.
      if (typeof(T).IsAssignableFrom(typeof(View)) && doc.ActiveView is View activeView)
      {
        // May fail because of Active View.

        var activeViewId = activeView.Id;
        var activeViewFound = false;
        foreach (var element in elements)
        {
          if (element is null) continue;

          Debug.Assert(element.Document.Equals(doc));
          if (activeViewId == element.Id)
          {
            activeViewFound = true;
          }
          else
          {
            elementIds.Add(element.Id);
          }
        }

        if (activeViewFound)
        {
          using (var message = new FailureMessage(ExternalFailures.ViewFailures.CanNotDeleteActiveView))
          {
            var resolution = Autodesk.Revit.DB.DeleteElements.Create(doc, activeViewId);
            message.AddResolution(FailureResolutionType.DeleteElements, resolution);
            message.SetFailingElement(activeViewId);
            doc.PostFailure(message);
          }
        }
      }
      else
      {
        foreach (var element in elements)
        {
          if (element is null) continue;
          Debug.Assert(element.Document.Equals(doc));
          elementIds.Add(element.Id);
        }
      }

      return doc.Delete(elementIds).AsReadOnlyElementIdSet();
    }

    internal static ReadOnlyElementIdSet DeleteElement<T>(this Document doc, T elements) where T : Element
    {
      return DeleteElements(doc, new T[] { elements });
    }
    #endregion

    #region Copy
    internal static IReadOnlyDictionary<ElementId, ElementId> CopyElements
    (
      this Document sourceDocument,
      IEnumerable<ElementId> elementsToCopy,
      Document destinationDocument = default,
      CopyPasteOptions options = default,
      Transform transform = default
    )
    {
      using (var copyOptions = options ?? new CopyPasteOptions())
      {
        if (options is null) copyOptions.SetDuplicateTypeNamesAction(DuplicateTypeAction.UseDestinationTypes);

        var idsToCopy = new HashSet<ElementId>(elementsToCopy, default(ElementIdEqualityComparer)).ToArray();
        Array.Sort(idsToCopy, ElementIdComparer.Ascending);

        var idsCopied = ElementTransformUtils.CopyElements
        (
          sourceDocument,
          idsToCopy,
          destinationDocument ?? sourceDocument,
          transform,
          copyOptions
        ).AsReadOnlyElementIdList();

        var copiedElements = new SortedList<ElementId, ElementId>(idsToCopy.Length, ElementIdComparer.Ascending);
        for(var i = 0; i < Math.Min(idsToCopy.Length, idsCopied.Count); ++i)
          copiedElements.Add(idsToCopy[i], idsCopied[i]);

        return copiedElements;
      }
    }
    #endregion

    #region Resources
    static readonly Dictionary<string, Document> Resources = new Dictionary<string, Document>();

    static Document ResourceDocument(this Autodesk.Revit.ApplicationServices.Application app, string resourceName)
    {
      if (!Resources.TryGetValue(resourceName, out var document) || !document.IsValidObject)
      {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var assemblyName = new System.Reflection.AssemblyName(assembly.FullName);
        var name = $"{assemblyName.Name}.Resources.RVT{app.VersionNumber}.{resourceName}";
        using (var resourceStream = assembly.GetManifestResourceStream(name))
        {
          var activeWindow = Microsoft.Win32.SafeHandles.WindowHandle.ActiveWindow;
          var tempPath = Path.Combine(Path.GetTempPath(), resourceName);
          try
          {
            using (var temp = File.OpenWrite(tempPath))
              resourceStream.CopyTo(temp);

            Resources[resourceName] = document = app.NewProjectDocument(tempPath);
          }
          finally
          {
            File.Delete(tempPath);
            Microsoft.Win32.SafeHandles.WindowHandle.ActiveWindow = activeWindow;
          }
        }
      }

      return document;
    }

    static readonly string WorkPlaneBasedFamilyName = ElementNaming.MakeValidName($"[{WorkPlaneBasedSymbolName}]");
    const string WorkPlaneBasedSymbolName = "Work Plane-Based";

    static FamilySymbol GetWorkPlaneBasedSymbol(this Document document)
    {
      var symbol = default(FamilySymbol);

      if (document is object)
      {
        if (symbol is null)
        {
          using
          (
            var collector = new FilteredElementCollector(document).WhereElementIsElementType().
            OfCategoryId(new ElementId(BuiltInCategory.OST_GenericModel)).
            OfClass(typeof(FamilySymbol)).
            WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, WorkPlaneBasedFamilyName).
            WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_TYPE_NAME, WorkPlaneBasedSymbolName)
          )
          {
            symbol = collector.Cast<FamilySymbol>().FirstOrDefault();
          }
        }

        // Check if is in another category
        if (symbol is null)
        {
          using
          (
            var collector = new FilteredElementCollector(document).WhereElementIsElementType().
            OfClass(typeof(FamilySymbol)).
            WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, WorkPlaneBasedFamilyName).
            WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_TYPE_NAME, WorkPlaneBasedSymbolName)
          )
          {
            symbol = collector.Cast<FamilySymbol>().FirstOrDefault();
          }
        }
      }

      return symbol;
    }

    internal static FamilySymbol EnsureWorkPlaneBasedSymbol(this Document document)
    {
      if (GetWorkPlaneBasedSymbol(document) is FamilySymbol symbol)
      {
        if(symbol.Family.FamilyCategoryId.ToBuiltInCategory() != BuiltInCategory.OST_GenericModel)
          symbol.Family.FamilyCategoryId = new ElementId(BuiltInCategory.OST_GenericModel);

        symbol.Family.get_Parameter(BuiltInParameter.FAMILY_WORK_PLANE_BASED).Update(true);
        symbol.Family.get_Parameter(BuiltInParameter.FAMILY_ALWAYS_VERTICAL).Update(false);
        symbol.Family.get_Parameter(BuiltInParameter.FAMILY_SHARED).Update(false);
      }
      else symbol = CreateWorkPlaneBasedSymbol(document, WorkPlaneBasedFamilyName, WorkPlaneBasedSymbolName);

      return symbol;
    }

    internal static FamilySymbol CreateWorkPlaneBasedSymbol(this Document document, string familyName, string symbolName = default)
    {
      if (GetWorkPlaneBasedSymbol(document.Application.ResourceDocument("RiR-Template.rte")) is var symbol)
      {
        using (symbol.Document.RollBackScope())
        {
          // Change the name to avoid any possible name collision
          var uniqueName = Guid.NewGuid().ToString();
          symbol.Family.Name = uniqueName;
          symbol.Name = uniqueName;

          symbol = symbol.CloneElement(document);
          symbol.Family.Name = familyName;
          symbol.Name = symbolName ?? familyName;
          return symbol;
        }
      }

      return null;
    }
    #endregion
  }

  public static class CopyPasteOptionsExtension
  {
    struct UseDestinationTypeHandler : IDuplicateTypeNamesHandler
    {
      public DuplicateTypeAction OnDuplicateTypeNamesFound(DuplicateTypeNamesHandlerArgs args) => DuplicateTypeAction.UseDestinationTypes;
    }

    struct UseUniqueTypeHandler : IDuplicateTypeNamesHandler
    {
      public DuplicateTypeAction OnDuplicateTypeNamesFound(DuplicateTypeNamesHandlerArgs args) => DuplicateTypeAction.Abort;
    }

    public static void SetDuplicateTypeNamesAction(this CopyPasteOptions options, DuplicateTypeAction action)
    {
      switch (action)
      {
        case DuplicateTypeAction.UseDestinationTypes: options.SetDuplicateTypeNamesHandler(default(UseDestinationTypeHandler)); break;
        case DuplicateTypeAction.Abort:               options.SetDuplicateTypeNamesHandler(default(UseUniqueTypeHandler));      break;
        default: throw new ArgumentOutOfRangeException(nameof(action));
      }
    }
  }
}
