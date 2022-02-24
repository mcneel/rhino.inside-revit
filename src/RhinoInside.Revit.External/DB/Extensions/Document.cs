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

    #region File
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
      // Document.Title may contain the extension
      // on some Revit versions or depending on user settings.
      // To avoid the corner case where the file was called "Project.rvt.rvt",
      // we try first with the Document.PathName.
      return string.IsNullOrEmpty(doc.PathName) ?
        Path.GetFileNameWithoutExtension(doc.Title) :
        Path.GetFileNameWithoutExtension(doc.PathName);
    }

    /// <summary>
    /// The document's file name.
    /// </summary>
    /// <param name="doc"></param>
    /// <returns>The file name of the document's disk file with extension.</returns>
    /// <remarks>
    /// This method returns an non empty string even if the project has not been saved yet.
    /// </remarks>
    public static string GetFileName(this Document doc)
    {
      return string.IsNullOrEmpty(doc.PathName) ?
        Path.GetFileNameWithoutExtension(doc.Title) + (doc.IsFamilyDocument ? ".rfa" : ".rvt") :
        Path.GetFileName(doc.PathName);        
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

    #region Nomen
    internal static bool TryGetElement<T>(this Document doc, out T element, string nomen, string parentName = default, BuiltInCategory? categoryId = default) where T : Element
    {
      if (typeof(ElementType).IsAssignableFrom(typeof(T)))
      {
        using (var collector = new FilteredElementCollector(doc).WhereElementIsKindOf(typeof(T)))
        {
          element = collector.
          WhereElementIsKindOf(typeof(T)).
          WhereCategoryIdEqualsTo(categoryId).
          WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, parentName).
          WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_TYPE_NAME, nomen).
          OfType<T>().FirstOrDefault();
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
          WhereElementIsKindOf(typeof(T)).
          WhereCategoryIdEqualsTo(categoryId);

          var nameParameter = ElementExtension.GetNomenParameter(typeof(T));
          var enumerable = nameParameter != BuiltInParameter.INVALID ?
            elementCollector.WhereParameterEqualsTo(nameParameter, nomen) :
            elementCollector.Where(x => x.Name == nomen);

          element = enumerable.OfType<T>().FirstOrDefault();
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

    internal static IEnumerable<Element> GetNamesakeElements(this Document doc, string name, Type type, string parentName = default, BuiltInCategory? categoryId = default)
    {
      var enumerable = Enumerable.Empty<Element>();

      if (!string.IsNullOrEmpty(name))
      {
        if (typeof(ElementType).IsAssignableFrom(type))
        {
          enumerable = new FilteredElementCollector(doc).
          WhereElementIsElementType().
          WhereElementIsKindOf(type).
          WhereCategoryIdEqualsTo(categoryId).
          WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, parentName).
          WhereParameterBeginsWith(BuiltInParameter.ALL_MODEL_TYPE_NAME, name);
        }
        else
        {
          var elementCollector = new FilteredElementCollector(doc).
          WhereElementIsNotElementType().
          WhereElementIsKindOf(type).
          WhereCategoryIdEqualsTo(categoryId);

          var nameParameter = ElementExtension.GetNomenParameter(type);
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
            TryParseNomenId(x.Name, out var prefix, out var _);
            return prefix == name;
          }
        );
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

      if (!NamingUtils.IsValidName(nomen))
        throw new ArgumentException("Element name contains prohibited characters and is invalid.", nameof(nomen));

      // Remove number sufix from name and trailing spaces.
      TryParseNomenId(nomen.Trim(), out nomen, out var _);

      var last = doc.GetNamesakeElements(nomen, type, parentName, categoryId).
        OrderBy(x => x.GetElementNomen(), default(ElementNameComparer)).LastOrDefault();

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
        if (!value.StartsWith(prefix)) continue;

        TryParseNomenId(value, out var name, out var _);
        if (name != prefix) continue;

        yield return value;
      }
    }

    internal static string NextNomenOrDefault(this IEnumerable<string> enumerable)
    {
      var last = enumerable.OrderBy(x => x, default(ElementNameComparer)).LastOrDefault();

      if (last is object)
      {
        TryParseNomenId(last, out var next, out var id);
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
      height += doc.Application.ShortCurveTolerance;

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
      height -= doc.Application.ShortCurveTolerance;

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
  }
}
