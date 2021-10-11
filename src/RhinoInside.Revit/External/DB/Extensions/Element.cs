using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal static class ElementEqualityComparer
  {
    public static readonly IEqualityComparer<Element> InterDocument = new InterDocumentComparer();
    public static readonly IEqualityComparer<Element> SameDocument = new SameDocumentComparer();

    struct SameDocumentComparer : IEqualityComparer<Element>
    {
      bool IEqualityComparer<Element>.Equals(Element x, Element y) => ReferenceEquals(x, y) || x?.Id == y?.Id;
      int IEqualityComparer<Element>.GetHashCode(Element obj) => obj?.Id.IntegerValue ?? int.MinValue;
    }

    struct InterDocumentComparer : IEqualityComparer<Element>
    {
      bool IEqualityComparer<Element>.Equals(Element x, Element y) =>  IsEquivalent(x, y);
      int IEqualityComparer<Element>.GetHashCode(Element obj) => (obj?.Id.IntegerValue ?? int.MinValue) ^ (obj?.Document.GetHashCode() ?? 0);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.Element"/> equals to this <see cref="Autodesk.Revit.DB.Element"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Autodesk.Revit.DB.Element"/> instances are considered equivalent if they represent the same element
    /// in this Revit session.
    /// </remarks>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsEquivalent(this Element self, Element other)
    {
      if (ReferenceEquals(self, other))
        return true;

      if (self?.Id != other?.Id)
        return false;

      if (!self.IsValidObject || !other.IsValidObject)
        return false;

      return self.Document.Equals(other.Document);
    }
  }

  internal struct ElementNameComparer : IComparer<string>
  {
    public int Compare(string x, string y) => NamingUtils.CompareNames(x, y);
  }

  public static class ElementExtension
  {
    public static bool IsValid(this Element element) => element?.IsValidObject == true;

    public static bool IsValidWithLog(this Element element, out string log)
    {
      if (element is null)        { log = "Element is a null reference.";                    return false; }
      if (!element.IsValidObject) { log = "Referenced Revit element was deleted or undone."; return false; }

      log = string.Empty;
      return true;
    }

    public static bool CanBeRenamed(this Element element)
    {
      if (element is null) return false;

      using (element.Document.RollBackScope())
      {
        try { element.Name = Guid.NewGuid().ToString("N"); }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      return true;
    }

    public static bool IsNameInUse(this Element element, string name)
    {
      if (element is null) return false;

      using (element.Document.RollBackScope())
      {
        try { element.Name = name; }
        // The caller needs to see this exception to know this element can not be renamed.
        //catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { return true; }
      }

      return element.Name == name;
    }

    public static GeometryElement GetGeometry(this Element element, Options options)
    {
      if (!element.IsValid())
        return default;

      var geometry = element.get_Geometry(options);

      if (!(geometry?.Any() ?? false) && element is GenericForm form && !form.Combinations.IsEmpty)
      {
        geometry.Dispose();

        options.IncludeNonVisibleObjects = true;
        return element.get_Geometry(options);
      }

      return geometry;
    }

#if !REVIT_2019
    public static IList<ElementId> GetDependentElements(this Element element, ElementFilter filter)
    {
      var doc = element.Document;
      using (doc.RollBackScope())
      {
        var collection = doc.Delete(element.Id);

        return filter is null ? 
          collection?.ToList():
          collection?.Where(x => filter.PassesFilter(doc, x)).ToList();
      }
    }
#endif

    /// <summary>
    /// Updater to collect changes on the Delete operation
    /// </summary>
    /// <remarks>
    /// Using this IUpdater avoids <see cref="Autodesk.Revit.ApplicationServices.Application.DocumentChanged"/> to be fired.
    /// </remarks>
    class DeleteUpdater : IUpdater, IDisposable, IFailuresPreprocessor
    {
      public string GetUpdaterName() => "Delete Updater";
      public string GetAdditionalInformation() => "N/A";
      public ChangePriority GetChangePriority() => ChangePriority.Annotations;
      public UpdaterId GetUpdaterId() => UpdaterId;
      public static readonly UpdaterId UpdaterId = new UpdaterId
      (
        AddIn.Id,
        new Guid("9536C7C9-C58B-4D48-9103-5C8EBAA6F6C8")
      );

      static readonly ElementId[] Empty = new ElementId[0];

      public ICollection<ElementId> DeletedElementIds { get; private set; } = Empty;
      public ICollection<ElementId> ModifiedElementIds { get; private set; } = Empty;

      public DeleteUpdater(Document document, ElementFilter filter)
      {
        UpdaterRegistry.RegisterUpdater(this, isOptional: true);

        if (filter is null)
          filter = CompoundElementFilter.Full;

        UpdaterRegistry.AddTrigger(UpdaterId, document, filter, Element.GetChangeTypeAny());
        UpdaterRegistry.AddTrigger(UpdaterId, document, filter, Element.GetChangeTypeElementDeletion());
      }

      void IDisposable.Dispose()
      {
        UpdaterRegistry.RemoveAllTriggers(UpdaterId);
        UpdaterRegistry.UnregisterUpdater(UpdaterId);
      }

      public void Execute(UpdaterData data)
      {
        DeletedElementIds = data.GetDeletedElementIds();
        ModifiedElementIds = data.GetModifiedElementIds();
      }

      public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor) => FailureProcessingResult.ProceedWithRollBack;
    }

    /// <summary>
    /// Same as <see cref="Document.Delete(ICollection{ElementId})"/> but also return modified elements.
    /// </summary>
    /// <param name="document">The document where elementIds belong to.</param>
    /// <param name="elementIds">The ids of the elements to delete.</param>
    /// <param name="modifiedElements">The modified element id set.</param>
    /// <param name="filter">What type of elements we are interested of. Can be null to return all related elements.</param>
    /// <returns>The deleted element id set.</returns>
    public static ICollection<ElementId> GetDependentElements
    (
      this Document document,
      ICollection<ElementId> elementIds,
      out ICollection<ElementId> modifiedElements,
      ElementFilter filter
    )
    {
      using (var updater = new DeleteUpdater(document, filter))
      using (var tx = new Transaction(document, "Delete"))
      {
        tx.Start();
        document.Delete(elementIds);
        tx.Commit
        (
          tx.GetFailureHandlingOptions().
          SetClearAfterRollback(true).
          SetForcedModalHandling(true).
          SetFailuresPreprocessor(updater)
        );

        modifiedElements = updater.ModifiedElementIds ?? new List<ElementId>();
        return updater.DeletedElementIds              ?? new List<ElementId>();
      }
    }

    internal static ElementFilter CreateElementClassFilter(Type type)
    {
      if (type == typeof(Area))
        return new AreaFilter();

      if (type == typeof(AreaTag))
        return new AreaTagFilter();

      if (type == typeof(Room))
        return new RoomFilter();

      if (type == typeof(RoomTag))
        return new RoomTagFilter();

      if (type == typeof(Space))
        return new SpaceFilter();

      if (type == typeof(SpaceTag))
        return new SpaceTagFilter();

      if (type.IsSubclassOf(typeof(CurveElement)))
        type = typeof(CurveElement);

      return new ElementClassFilter(type);
    }

    public static T[] GetDependents<T>(this Element element) where T : Element
    {
      var doc = element.Document;
      if (typeof(T) == typeof(Element))
      {
        var ids = element.GetDependentElements(default);
        return ids.Where(x => x != element.Id).Select(x => doc.GetElement(x)).ToArray() as T[];
      }
      else
      {
        using (var filter = CreateElementClassFilter(typeof(T)))
        {
          var ids = element.GetDependentElements(filter);
          return ids.Where(x => x != element.Id).Select(x => doc.GetElement(x)).OfType<T>().ToArray();
        }
      }
    }

    public static T GetFirstDependent<T>(this Element element) where T : Element
    {
      var doc = element.Document;
      if (typeof(T) == typeof(Element))
      {
        var ids = element.GetDependentElements(default);
        return ids.Where(x => x != element.Id).Select(x => doc.GetElement(x)).FirstOrDefault() as T;
      }
      else
      {
        using (var filter = CreateElementClassFilter(typeof(T)))
        {
          var ids = element.GetDependentElements(filter);
          return ids.Where(x => x != element.Id).Select(x => doc.GetElement(x)).OfType<T>().FirstOrDefault();
        }
      }
    }

    #region Parameter
    struct ParameterEqualityComparer : IEqualityComparer<Parameter>
    {
      public bool Equals(Parameter x, Parameter y) => x.Id.IntegerValue == y.Id.IntegerValue;
      public int GetHashCode(Parameter obj) => obj.Id.IntegerValue;
    }

    public static IEnumerable<Parameter> GetParameters(this Element element, ParameterClass set)
    {
      switch (set)
      {
        case ParameterClass.Any:
          return BuiltInParameterExtension.BuiltInParameters.
            Select
            (
              x =>
              {
                try { return element.get_Parameter(x); }
                catch (Autodesk.Revit.Exceptions.InternalException) { return null; }
              }
            ).
            Where(x => x?.Definition is object).
            Union(element.Parameters.Cast<Parameter>().Where(x => x.StorageType != StorageType.None), default(ParameterEqualityComparer)).
            OrderBy(x => x.Id.IntegerValue);
        case ParameterClass.BuiltIn:
          return BuiltInParameterExtension.BuiltInParameters.
            Select
            (
              x =>
              {
                try { return element.get_Parameter(x); }
                catch (Autodesk.Revit.Exceptions.InternalException) { return null; }
              }
            ).
            Where(x => x?.Definition is object);
        case ParameterClass.Project:
          return element.Parameters.Cast<Parameter>().
            Where(p => !p.IsShared && p.Id.IntegerValue > 0).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 1).
            OrderBy(x => x.Id.IntegerValue);
        case ParameterClass.Family:
          return element.Parameters.Cast<Parameter>().
            Where(p => !p.IsShared && p.Id.IntegerValue > 0).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 0).
            OrderBy(x => x.Id.IntegerValue);
        case ParameterClass.Shared:
          return element.Parameters.Cast<Parameter>().
            Where(p => p.IsShared).
            OrderBy(x => x.Id.IntegerValue);
      }

      return Enumerable.Empty<Parameter>();
    }

    public static IEnumerable<Parameter> GetParameters(this Element element, string name, ParameterClass set)
    {
      switch (set)
      {
        case ParameterClass.Any:
          return element.GetParameters(name, ParameterClass.BuiltIn).
            Union(element.GetParameters(name), default(ParameterEqualityComparer)).
            OrderBy(x => x.Id.IntegerValue);

        case ParameterClass.BuiltIn:
          return BuiltInParameterExtension.BuiltInParameterMap.TryGetValue(name, out var parameters) ?
            parameters.Select(x => element.get_Parameter(x)).Where(x => x?.Definition is object) :
            Enumerable.Empty<Parameter>();

        case ParameterClass.Project:
          return element.GetParameters(name).
            Where(p => !p.IsShared && p.Id.IntegerValue > 0).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 1).
            OrderBy(x => x.Id.IntegerValue);

        case ParameterClass.Family:
          return element.GetParameters(name).
            Where(p => !p.IsShared && p.Id.IntegerValue > 0).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 0).
            OrderBy(x => x.Id.IntegerValue);

        case ParameterClass.Shared:
          return element.GetParameters(name).
            Where(p => p.IsShared).
            OrderBy(x => x.Id.IntegerValue);
      }

      return Enumerable.Empty<Parameter>();
    }

    public static Parameter GetParameter(this Element element, ElementId parameterId)
    {
      if (parameterId.TryGetBuiltInParameter(out var builtInParameter))
        return element.get_Parameter(builtInParameter);

      if (element.Document.GetElement(parameterId) is ParameterElement parameterElement)
        return element.get_Parameter(parameterElement.GetDefinition());

      return default;
    }

#if !REVIT_2022
    public static Parameter GetParameter(this Element element, Schemas.ParameterId parameterId)
    {
      return element.get_Parameter(parameterId);
    }
#endif

    public static Parameter GetParameter(this Element element, string name, ParameterClass set)
    {
      var parameters = element.GetParameters(name, set);
      return parameters.FirstOrDefault(x => !x.IsReadOnly) ?? parameters.FirstOrDefault();
    }

    public static Parameter GetParameter(this Element element, string name, Schemas.DataType type, ParameterClass set)
    {
      var parameters = element.GetParameters(name, set).Where(x => (Schemas.DataType) x.Definition.GetDataType() == type);
      return parameters.FirstOrDefault(x => !x.IsReadOnly) ?? parameters.FirstOrDefault();
    }

    public static Parameter GetParameter(this Element element, string name, Schemas.DataType type, ParameterBinding parameterBinding, ParameterClass set)
    {
      if (element is ElementType ? parameterBinding != ParameterBinding.Type : parameterBinding != ParameterBinding.Instance)
        return null;

      return GetParameter(element, name, type, set);
    }

    public static void CopyParametersFrom(this Element to, Element from, ICollection<BuiltInParameter> parametersMask = null)
    {
      if (from is null || to is null || to.IsEquivalent(from))
        return;

      foreach (var previousParameter in from.GetParameters(ParameterClass.Any))
        using (previousParameter)
        using (var param = to.get_Parameter(previousParameter.Definition))
        {
          if (param is null || param.IsReadOnly)
            continue;

          if
          (
            parametersMask is object &&
            param.Definition is InternalDefinition internalDefinition &&
            parametersMask.Contains(internalDefinition.BuiltInParameter)
          )
            continue;

          switch (previousParameter.StorageType)
          {
            case StorageType.Integer:
              param.Update(previousParameter.AsInteger());
              break;

            case StorageType.Double:
              param.Update(previousParameter.AsDouble());
              break;

            case StorageType.String:
              param.Update(previousParameter.AsString());
              break;

            case StorageType.ElementId:
              param.Update(GetNamesakeElement(to.Document, from.Document, previousParameter.AsElementId()));
              break;
          }
        }
    }

    internal static BuiltInParameter GetNameParameter(Type type)
    {
      // `DB.Family` parameter `ALL_MODEL_FAMILY_NAME` use to be `null`.
      //
      // if (typeof(Family).IsAssignableFrom(type))
      //   return BuiltInParameter.ALL_MODEL_FAMILY_NAME;

      if (typeof(ElementType).IsAssignableFrom(type))
        return BuiltInParameter.ALL_MODEL_TYPE_NAME;

      if (typeof(DatumPlane).IsAssignableFrom(type))
        return BuiltInParameter.DATUM_TEXT;

      if (typeof(View).IsAssignableFrom(type))
        return BuiltInParameter.VIEW_NAME;

      if (typeof(Viewport).IsAssignableFrom(type))
        return BuiltInParameter.VIEWPORT_VIEW_NAME;

      if (typeof(PropertySetElement).IsAssignableFrom(type))
        return BuiltInParameter.PROPERTY_SET_NAME;

      if (typeof(AssemblyInstance).IsAssignableFrom(type))
        return BuiltInParameter.ASSEMBLY_NAME;

      if (typeof(Material).IsAssignableFrom(type))
        return BuiltInParameter.MATERIAL_NAME;

      if (typeof(DesignOption).IsAssignableFrom(type))
        return BuiltInParameter.OPTION_NAME;

      if (typeof(Phase).IsAssignableFrom(type))
        return BuiltInParameter.PHASE_NAME;

      if (typeof(AreaScheme).IsAssignableFrom(type))
        return BuiltInParameter.AREA_SCHEME_NAME;

      if (typeof(Room).IsAssignableFrom(type))
        return BuiltInParameter.ROOM_NAME;

      if (typeof(Zone).IsAssignableFrom(type))
        return BuiltInParameter.ZONE_NAME;

      return BuiltInParameter.INVALID;
    }

    static BuiltInParameter GetNameParameter(Element element)
    {
      var builtInParameter = GetNameParameter(element.GetType());
      if (builtInParameter != BuiltInParameter.INVALID) return builtInParameter;

      if (element.Category is Category category)
      {
        if (category.Id.TryGetBuiltInCategory(out var builtInCategory) == true)
        {
          switch(builtInCategory)
          {
            case BuiltInCategory.OST_DesignOptionSets: return BuiltInParameter.OPTION_SET_NAME;
            case BuiltInCategory.OST_VolumeOfInterest: return BuiltInParameter.VOLUME_OF_INTEREST_NAME;
          }
        }
      }

      // This is too slow.
      //// Find a built-in parameter called "Name"
      //{
      //  // Get "Name" localized
      //  var _Name_ = LabelUtils.GetLabelFor(BuiltInParameter.DATUM_TEXT);
      //  if (element.GetParameter(_Name_, ParameterClass.BuiltIn) is Parameter param && param.StorageType == StorageType.String)
      //    return (BuiltInParameter) param.Id.IntegerValue;
      //}

      return BuiltInParameter.INVALID;
    }

    internal static ElementId GetNamesakeElement(Document target, Document source, ElementId elementId)
    {
      if (elementId.IsBuiltInId() || target.IsEquivalent(source))
        return elementId;

      if (source.GetElement(elementId) is Element element)
      {
        if (element is ElementType type)
        {
          using (var collector = new FilteredElementCollector(target))
          {
            return collector.WhereElementIsElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementId.InvalidElementId).
              WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_FAMILY_NAME, type.FamilyName).
              WhereParameterEqualsTo(BuiltInParameter.ALL_MODEL_TYPE_NAME, type.Name).
              FirstElementId();
          }
        }
        if (element is AppearanceAssetElement)
        {
          return AppearanceAssetElement.GetAppearanceAssetElementByName(target, element.Name)?.Id ?? ElementId.InvalidElementId;
        }
        else
        {
          using (var collector = new FilteredElementCollector(target))
          {
            var name = element.Name;
            var nameParameterId = GetNameParameter(element);
            if (nameParameterId != BuiltInParameter.INVALID)
            {
              return collector.WhereElementIsNotElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementId.InvalidElementId).
              WhereParameterEqualsTo(nameParameterId, name).
              FirstElementId();
            }
            else
            {
              return collector.WhereElementIsNotElementType().
              WhereElementIsKindOf(element.GetType()).
              WhereCategoryIdEqualsTo(element.Category?.Id ?? ElementId.InvalidElementId).
              Where(x => x.Name == name).Select(x => x.Id).FirstOrDefault() ??
              ElementId.InvalidElementId;
            }
          }
        }
      }

      return ElementId.InvalidElementId;
    }

    public static T GetParameterValue<T>(this Element element, BuiltInParameter paramId)
    {
      using (var param = element.get_Parameter(paramId))
      {
        if (param is null)
          throw new System.InvalidOperationException();

        if (typeof(T) == typeof(bool))
        {
          if (param.StorageType != StorageType.Integer || (Schemas.DataType) param.Definition.GetDataType() != Schemas.SpecType.Boolean.YesNo)
            throw new System.InvalidCastException();

          return (T) (object) (param.AsInteger() != 0);
        }
        else if (typeof(T) == typeof(int))
        {
          if (param.StorageType != StorageType.Integer || (Schemas.DataType) param.Definition.GetDataType() != Schemas.SpecType.Int.Integer)
            throw new System.InvalidCastException();

          return (T) (object) (param.AsInteger() != 0);
        }
        else if (typeof(T).IsSubclassOf(typeof(Enum)))
        {
          if (param.StorageType != StorageType.Integer || (Schemas.DataType) param.Definition.GetDataType() != Schemas.SpecType.Int.Integer)
            throw new System.InvalidCastException();

          return (T) (object) (param.AsInteger() != 0);
        }
        else if (typeof(T) == typeof(double))
        {
          if (param.StorageType != StorageType.Double)
            throw new System.InvalidCastException();

          return (T) (object) param.AsDouble();
        }
        else if (typeof(T) == typeof(string))
        {
          if (param.StorageType != StorageType.String)
            throw new System.InvalidCastException();

          return (T) (object) param.AsString();
        }
        else if (typeof(T).IsSubclassOf(typeof(Element)))
        {
          if (param.StorageType != StorageType.ElementId)
            throw new System.InvalidCastException();

          var id = param.AsElementId();
          if (id.IsCategoryId(element.Document))
            throw new System.InvalidCastException();

          return (T) (object) element.Document.GetElement(param.AsElementId());
        }
        else if (typeof(T) == typeof(Category))
        {
          if (param.StorageType != StorageType.ElementId)
            throw new System.InvalidCastException();

          return (T) (object) element.Document.GetCategory(param.AsElementId());
        }
      }

      return default;
    }

    public static void UpdateParameterValue(this Element element, BuiltInParameter paramId, bool value)
    {
      using (var param = element.get_Parameter(paramId))
      {
        if (param is null)
          throw new System.InvalidOperationException();

        if(param.StorageType != StorageType.Integer || (Schemas.DataType) param.Definition.GetDataType() != Schemas.SpecType.Boolean.YesNo)
          throw new System.InvalidCastException();

        param.Update(value ? 1 : 0);
      }
    }

    public static void UpdateParameterValue(this Element element, BuiltInParameter paramId, object value)
    {
      if (element.get_Parameter(paramId) is Parameter param)
      {
        switch (value)
        {
          case int intVal:
            if (StorageType.Integer == param.StorageType)
              param.Update(intVal);
            break;
          case string strVal:
            if (StorageType.String == param.StorageType)
              param.Update(strVal);
            break;
          case double dblVal:
            if (StorageType.Double == param.StorageType)
              param.Update(dblVal);
            break;
          case ElementId idVal:
            if (StorageType.ElementId == param.StorageType)
              param.Update(idVal);
            break;
        }
      }
    }
    #endregion

    #region Replace
    public static T ReplaceElement<T>(this T from, T to, ICollection<BuiltInParameter> mask) where T : Element
    {
      to.CopyParametersFrom(from, mask);
      return to;
    }
    #endregion
  }
}
