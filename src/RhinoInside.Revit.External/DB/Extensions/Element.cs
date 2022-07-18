using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal static class ElementEqualityComparer
  {
    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Element"/>
    /// that compares elements from different <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static readonly IEqualityComparer<Element> InterDocument = default(InterDocumentComparer);

    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Element"/>
    /// that assumes all elements are from the same <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static readonly IEqualityComparer<Element> SameDocument = default(SameDocumentComparer);

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
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.Element"/> equals
    /// to this <see cref="Autodesk.Revit.DB.Element"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Autodesk.Revit.DB.Element"/> instances are considered equivalent
    /// if they represent the same element in this Revit session.
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

  public static class ElementNaming 
  {
    /// <summary>
    /// Identifies if the input <paramref name="name"/> is valid for use as an Element name in Revit.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool IsValidName(string name) => NamingUtils.IsValidName(name);

    public static bool IsValidNameCharacter(char character) => NamingUtils.IsValidName(character.ToString());
    public static string InvalidCharacters => "\\:{}[]|;<>?`~";

    public static string MakeValidName(string name)
    {
      var builder = new System.Text.StringBuilder();

      foreach(var c in name)
      {
        switch(c)
        {
          case '\\': builder.Append('⧵'); break;
          case ':': builder.Append('∶'); break;
          case '{': builder.Append('❴'); break;
          case '}': builder.Append('❵'); break;
          case '[': builder.Append('［'); break;
          case ']': builder.Append('］'); break;
          case '|': builder.Append('∣'); break;
          case ';': builder.Append(';'); break;
          case '<': builder.Append('‹'); break;
          case '>': builder.Append('›'); break;
          case '?': builder.Append('¿'); break;
          case '`': builder.Append('｀'); break;
          case '~': builder.Append('∼'); break;
          default:
            if (IsValidNameCharacter(c)) builder.Append(c);
            else builder.Append('�');
            break;
        }
      }

      return builder.ToString();
    }

    struct ElementNameComparer : IComparer<string>
    {
      public int Compare(string x, string y) => NamingUtils.CompareNames(x, y);
    }

    /// <summary>
    /// Compares two Element name string for equality using Revit's comparison rules.
    /// </summary>
    public static readonly IEqualityComparer<string> NameEqualityComparer = StringComparer.Ordinal;

    /// <summary>
    /// Compares two Element name strings using Revit's comparison rules.
    /// </summary>
    public static readonly IComparer<string> NameComparer = default(ElementNameComparer);

    /// <summary>
    /// <see cref="System.StringComparison"/> used on <see cref="Autodesk.Revit.DB.NamingUtils"/> for equality comparsion.
    /// </summary>
    /// <remarks>
    /// Revit UI is not consitent some places uses a case sensitive comparsion.
    /// </remarks>
    public static readonly StringComparison ComparisonType = StringComparison.Ordinal;
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

    public static ElementKind GetElementKind(this Element element)
    {
      switch (element)
      {
        case DirectShape     _: return ElementKind.Direct;
        case DirectShapeType _: return ElementKind.Direct;
        case FamilyInstance  _: return ElementKind.Component;
        case FamilySymbol    _: return ElementKind.Component;
        case Family          _: return ElementKind.Component;
        case Element         _: return ElementKind.System;
      }

      return ElementKind.None;
    }

    public static Outline GetOutline(this Element element)
    {
      var bbox = element.GetBoundingBoxXYZ();
      if (bbox is null)
        return null;

      var xform = bbox.Transform;
      if (xform.IsIdentity)
        return new Outline(bbox.Min, bbox.Max);

      var min = bbox.Min;
      var max = bbox.Max;
      var corners = new XYZ[]
      {
        xform.OfPoint(new XYZ(min.X, min.Y, min.Z)),
        xform.OfPoint(new XYZ(max.X, min.Y, min.Z)),
        xform.OfPoint(new XYZ(max.X, max.Y, min.Z)),
        xform.OfPoint(new XYZ(min.X, max.Y, min.Z)),
        xform.OfPoint(new XYZ(min.X, min.Y, max.Z)),
        xform.OfPoint(new XYZ(max.X, min.Y, max.Z)),
        xform.OfPoint(new XYZ(max.X, max.Y, max.Z)),
        xform.OfPoint(new XYZ(min.X, max.Y, max.Z))
      };

      var minX = double.PositiveInfinity; var minY = double.PositiveInfinity; var minZ = double.PositiveInfinity;
      var maxX = double.NegativeInfinity; var maxY = double.NegativeInfinity; var maxZ = double.NegativeInfinity;

      foreach (var xyz in corners)
      {
        minX = Math.Min(minX, xyz.X); maxX = Math.Max(maxX, xyz.X);
        minY = Math.Min(minY, xyz.Y); maxY = Math.Max(maxY, xyz.Y);
        minZ = Math.Min(minZ, xyz.Z); maxZ = Math.Max(maxZ, xyz.Z);
      }

      return new Outline(new XYZ(minX, minY, minZ), new XYZ(maxX, maxY, maxZ));
    }

    public static bool HasBoundingBoxXYZ(this Element element)
    {
      using (var bbox = element.GetBoundingBoxXYZ())
        return bbox is object;
    }

    public static BoundingBoxXYZ GetBoundingBoxXYZ(this Element element, out View view)
    {
      view = element.ViewSpecific ? element.Document.GetElement(element.OwnerViewId) as View : default;
      return element.get_BoundingBox(view.IsGraphicalView() ? view : default);
    }

    public static BoundingBoxXYZ GetBoundingBoxXYZ(this Element element)
    {
      using (var view = element.ViewSpecific ? element.Document.GetElement(element.OwnerViewId) as View : default)
        return element.get_BoundingBox(view.IsGraphicalView() ? view : default);
    }

    static bool SkipGeometryObject(GeometryObject geometry, View view)
    {
      return geometry is null ||
      (view.Document.GetElement(geometry.GraphicsStyleId) is GraphicsStyle graphicsStyle &&
      view.GetCategoryHidden(graphicsStyle.GraphicsStyleCategory.Id));
    }

    static IEnumerable<XYZ> GetSamplePoints(IEnumerable<GeometryObject> geometries, View view)
    {
      foreach (var geometry in geometries)
      {
        if (SkipGeometryObject(geometry, view))
          continue;

        switch (geometry)
        {
          case GeometryInstance instance:
            foreach (var xyz in GetSamplePoints(instance.GetInstanceGeometry(), view))
              yield return xyz;

          break;

          case Mesh mesh:
            foreach (var xyz in mesh.Vertices)
              yield return xyz;

            break;

          case Solid solid:
            foreach (var face in solid.Faces.Cast<Face>())
            {
              foreach (var xyz in face.Triangulate().Vertices)
                yield return xyz;
            }

            break;

          case Curve curve:
            foreach (var xyz in curve.Tessellate())
              yield return xyz;

            break;

          case PolyLine polyline:
            foreach (var xyz in polyline.GetCoordinates())
              yield return xyz;

            break;
        }
      }
    }

    internal static IEnumerable<XYZ> GetSamplePoints(Element element, View view)
    {
      var options = new Options() { View = view };
      using (var geometry = element?.GetGeometry(options))
      {
        if (!SkipGeometryObject(geometry, view))
          return GetSamplePoints(geometry, view);
      }

      return Enumerable.Empty<XYZ>();
    }

    public static bool HasGeometry(this Element element)
    {
      using
      (
        var options = element.ViewSpecific ?
        new Options() { View = element.Document.GetElement(element.OwnerViewId) as View } :
        new Options()
      )
      using (var geometry = element.get_Geometry(options))
        return geometry is object;
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

    #region Dependents
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
      public UpdaterId GetUpdaterId() => updaterId;
      readonly UpdaterId updaterId;

      static readonly ElementId[] Empty = new ElementId[0];

      public ICollection<ElementId> DeletedElementIds { get; private set; } = Empty;
      public ICollection<ElementId> ModifiedElementIds { get; private set; } = Empty;

      public DeleteUpdater(Document document, ElementFilter filter)
      {
        updaterId = new UpdaterId
        (
          document.Application.ActiveAddInId,
          new Guid("9536C7C9-C58B-4D48-9103-5C8EBAA6F6C8")
        );

        UpdaterRegistry.RegisterUpdater(this, isOptional: true);

        if (filter is null)
          filter = CompoundElementFilter.All;

        UpdaterRegistry.AddTrigger(updaterId, document, filter, Element.GetChangeTypeAny());
        UpdaterRegistry.AddTrigger(updaterId, document, filter, Element.GetChangeTypeElementDeletion());
      }

      void IDisposable.Dispose()
      {
        UpdaterRegistry.RemoveAllTriggers(updaterId);
        UpdaterRegistry.UnregisterUpdater(updaterId);
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

    public static T[] GetDependents<T>(this Element element) where T : Element
    {
      var ids = element.GetDependentElements
      (
        CompoundElementFilter.ExclusionFilter(element.Id).Intersect
        (CompoundElementFilter.ElementClassFilter(typeof(T)))
      );

      var doc = element.Document;
      return ids.Select(doc.GetElement).OfType<T>().ToArray();
    }

    public static T GetFirstDependent<T>(this Element element) where T : Element
    {
      var ids = element.GetDependentElements
      (
        CompoundElementFilter.ExclusionFilter(element.Id).Intersect
        (CompoundElementFilter.ElementClassFilter(typeof(T)))
      );

      var doc = element.Document;
      return ids.Select(doc.GetElement).OfType<T>().FirstOrDefault();
    }
    #endregion

    #region Nomen

    // `Element.Name` does not always access the true denomination of the element.
    //
    // In cases like `ViewSheet` the true denomination is the "Sheet Number" parameter.
    // Denomination is used here as the element property that identifies it univocaly on the UI.
    // Is the property that produce a "Name" collision in case is duplicated.
    //
    // In other cases like 'Design Options' the Name parameter may come decorated
    // this makes `Element.Name` not useful for searching or comparing namesake elements.
    // Nomen is undecorated in this case.

    public static bool CanBeRenominated(this Element element)
    {
      if (element is null) return false;

      using (element.Document.RollBackScope())
      {
        try { element.SetElementNomen(Guid.NewGuid().ToString("N")); }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      return true;
    }

    public static bool IsNomenInUse(this Element element, string name)
    {
      if (element is null) return false;

      var nomen = element.GetElementNomen(out var nomenParameter);
      using (element.Document.RollBackScope())
      {
        try { element.SetElementNomen(nomenParameter, name); }
        // The caller should see this exception to know this element can not be renamed.
        //catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
        catch (Autodesk.Revit.Exceptions.ArgumentException) { return true; }
      }

      return nomen == name;
    }

    public static bool SetIncrementalNomen(this Element element, string prefix)
    {
      var prefixed = DocumentExtension.TryParseNomenId(element.Name, out var p, out var _);
      if (!prefixed || prefix != p)
      {
        var categoryId = element.Category is Category category &&
          category.Id.TryGetBuiltInCategory(out var builtInCategory) ?
          builtInCategory : default(BuiltInCategory?);

        var nextName = element.Document.NextIncrementalNomen
        (
          prefix,
          element.GetType(),
          element is ElementType type ?
          type.FamilyName :
          element is View view ? view.ViewType.ToString() :
          default,
          categoryId
        );

        if (nextName != element.GetElementNomen(out var nomenParameter))
        {
          element.SetElementNomen(nomenParameter, nextName);
          return true;
        }
      }

      return false;
    }

    internal static BuiltInParameter GetNomenParameter(Type type)
    {
      // `DB.Family` parameter `ALL_MODEL_FAMILY_NAME` use to be `null`.
      //
      // if (typeof(Family).IsAssignableFrom(type))
      //   return BuiltInParameter.ALL_MODEL_FAMILY_NAME;

      if (typeof(ElementType).IsAssignableFrom(type))
        return BuiltInParameter.ALL_MODEL_TYPE_NAME;

      if (typeof(DatumPlane).IsAssignableFrom(type))
        return BuiltInParameter.DATUM_TEXT;

      if (typeof(ViewSheet).IsAssignableFrom(type))
        return BuiltInParameter.SHEET_NUMBER;

      if (typeof(View).IsAssignableFrom(type))
        return BuiltInParameter.VIEW_NAME;

      if (typeof(Viewport).IsAssignableFrom(type))
        return BuiltInParameter.VIEWPORT_VIEW_NAME;

      if (typeof(PropertySetElement).IsAssignableFrom(type))
        return BuiltInParameter.PROPERTY_SET_NAME;

      if (typeof(Material).IsAssignableFrom(type))
        return BuiltInParameter.MATERIAL_NAME;

      if (typeof(DesignOption).IsAssignableFrom(type))
        return BuiltInParameter.OPTION_NAME;

      if (typeof(Phase).IsAssignableFrom(type))
        return BuiltInParameter.PHASE_NAME;

      if (typeof(AreaScheme).IsAssignableFrom(type))
        return BuiltInParameter.AREA_SCHEME_NAME;

      if (typeof(SpatialElement).IsAssignableFrom(type))
        return BuiltInParameter.ROOM_NUMBER;

      if (typeof(RevitLinkInstance).IsAssignableFrom(type))
        return BuiltInParameter.RVT_LINK_INSTANCE_NAME;

      // BuiltInParameter.IMPORT_SYMBOL_NAME is tagged as "Name" in te UI,
      // but in fact is the type-name not the instance name.
      //if (typeof(ImportInstance).IsAssignableFrom(type))
      //  return BuiltInParameter.IMPORT_SYMBOL_NAME;

      return BuiltInParameter.INVALID;
    }

    static BuiltInParameter GetNomenParameter(Element element)
    {
      var builtInParameter = GetNomenParameter(element.GetType());
      if (builtInParameter != BuiltInParameter.INVALID) return builtInParameter;

      if (element.Category is Category category)
      {
        if (category.Id.TryGetBuiltInCategory(out var builtInCategory) == true)
        {
          switch (builtInCategory)
          {
            case BuiltInCategory.OST_DesignOptionSets: return BuiltInParameter.OPTION_SET_NAME;
            case BuiltInCategory.OST_VolumeOfInterest: return BuiltInParameter.VOLUME_OF_INTEREST_NAME;
          }
        }
      }

      return BuiltInParameter.INVALID;
    }

    public static string GetElementNomen(this Element element) =>
      GetElementNomen(element, out var _);

    internal static string GetElementNomen(this Element element, out BuiltInParameter nomenParameter)
    {
      if ((nomenParameter = GetNomenParameter(element)) != BuiltInParameter.INVALID)
        return GetParameterValue<string>(element, nomenParameter);
      else
        return element.Name;
    }

    internal static string GetElementNomen(this Element element, BuiltInParameter nomenParameter)
    {
      if (nomenParameter != BuiltInParameter.INVALID)
        return GetParameterValue<string>(element, nomenParameter);
      else
        return element.Name;
    }

    internal static void SetElementNomen(this Element element, BuiltInParameter nomenParameter, string name)
    {
      if
      (
        !(element is ElementType) &&
        nomenParameter != BuiltInParameter.INVALID &&
        element.get_Parameter(nomenParameter) is Parameter parameter &&
        !parameter.IsReadOnly
      )
      {
        parameter.Update(name);
      }
      else if (element.Name != name)
      {
        element.Name = name;
      }
    }

    public static void SetElementNomen(this Element element, string nomen) =>
      SetElementNomen(element, GetNomenParameter(element), nomen);

    public static bool SwapElementNomenWith(this Element element, Element other, out BuiltInParameter nomenParameter)
    {
      if (element.GetType() != other.GetType())
        throw new InvalidOperationException($"{nameof(element)} type and {nameof(other)} type should match");

      var elementNomen = element.GetElementNomen(out var elementParameter);
      if (elementParameter != BuiltInParameter.INVALID)
      {
        var otherNomen = other.GetElementNomen(out var otherParameter);
        if (elementParameter != otherParameter)
          throw new InvalidOperationException($"{nameof(element)} nomen parameter {elementParameter} doesn't match with {nameof(other)} nomen parameter {otherParameter}");

        other.SetElementNomen(otherParameter, Guid.NewGuid().ToString());
        element.SetElementNomen(elementParameter, otherNomen);
        other.SetElementNomen(otherParameter, elementNomen);
        nomenParameter = elementParameter;
        return true;
      }

      nomenParameter = BuiltInParameter.INVALID;
      return false;
    }
    #endregion

    #region Parameter
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
            Union(element.Parameters.Cast<Parameter>().Where(x => x.StorageType != StorageType.None), ParameterEqualityComparer.SameDocument).
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
            Union(element.GetParameters(name), ParameterEqualityComparer.SameDocument).
            OrderBy(x => x.Id.IntegerValue);

        case ParameterClass.BuiltIn:
          return BuiltInParameterExtension.BuiltInParameterMap.TryGetValue(name, out var parameters) ?
            parameters.Select(element.get_Parameter).Where(x => x?.Definition is object) :
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
      return element.GetParameters(name, set).
        OrderByDescending(x => x.Id.IsBuiltInId()). // Ordered by IsBuiltInId to give priority to built-in parameters.
        ThenBy(x => x.IsReadOnly).                  // Then by IsReadOnly to give priority non read-only parameters.
        ThenByDescending                            // Then by storage-type to give priority ElementId parameters over String ones.
        (
          x => x.Id.TryGetBuiltInParameter(out var bip) ?
          x.Element.Document.get_TypeOfStorage(bip) :
          StorageType.None
        ).
        FirstOrDefault();
    }

    public static Parameter GetParameter(this Element element, string name, Schemas.DataType type, ParameterClass set)
    {
      return element.GetParameters(name, set).
        Where(x => (Schemas.DataType) x.Definition.GetDataType() == type).
        OrderByDescending(x => x.Id.IsBuiltInId()). // Ordered by IsBuiltInId to give priority to built-in parameters.
        ThenBy(x => x.IsReadOnly).                  // Then by IsReadOnly to give priority non read-only parameters.
        FirstOrDefault();
    }

    public static Parameter GetParameter(this Element element, string name, Schemas.DataType type, ParameterScope scope, ParameterClass set)
    {
      if (element is ElementType ? scope != ParameterScope.Type : scope != ParameterScope.Instance)
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
            internalDefinition.BuiltInParameter != BuiltInParameter.INVALID &&
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
              param.Update(to.Document.LookupElement(from.Document, previousParameter.AsElementId()));
              break;
          }
        }
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

        if (param.StorageType != StorageType.Integer || (Schemas.DataType) param.Definition.GetDataType() != Schemas.SpecType.Boolean.YesNo)
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
      var nomenParameter = BuiltInParameter.INVALID;
      if (from?.SwapElementNomenWith(to, out nomenParameter) == true)
        Debug.Assert(mask.Contains(nomenParameter));

      to.CopyParametersFrom(from, mask);
      return to;
    }
    #endregion

    #region CloneElement
    public static T CloneElement<T>(this T template, Document destinationDocument = null, View destinationView = null) where T : Element
    {
      try
      {
        var sourceDocument = template.Document;
        destinationDocument = destinationDocument ?? sourceDocument;

        var ids = default(ICollection<ElementId>);
        if (template.ViewSpecific)
        {
          var sourceView = sourceDocument.GetElement(template.OwnerViewId) as View;
          destinationView = destinationView ?? sourceView;

          if (!destinationDocument.Equals(destinationView.Document))
          {
            var bic = BuiltInCategory.INVALID;
            sourceView.Category?.Id.TryGetBuiltInCategory(out bic);
            destinationView = destinationDocument.
              GetNamesakeElements(sourceView.GetElementNomen(), sourceView.GetType(), parentName: sourceView.ViewType.ToString(), categoryId: bic).
              Cast<View>().FirstOrDefault();
          }

          if (destinationView is object)
          {
            ids = ElementTransformUtils.CopyElements
            (
              sourceView,
              new ElementId[] { template.Id },
              destinationView, default, default
            );
          }
        }
        else
        {
          ids = ElementTransformUtils.CopyElements
          (
            sourceDocument,
            new ElementId[] { template.Id },
            destinationDocument, default, default
          );
        }

        return ids.Select(destinationDocument.GetElement).OfType<T>().FirstOrDefault();
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      return null;
    }
    #endregion

    #region References
    public static Reference GetDefaultReference(this Element element)
    {
      var reference = default(Reference);
      switch (element)
      {
        case null:
          return null;

#if REVIT_2018
        case FamilyInstance instance:
          reference = instance.GetReferences(FamilyInstanceReferenceType.CenterLeftRight).FirstOrDefault();
          break;
#endif

        case CurveElement modelLine:
          reference = modelLine.GeometryCurve.Reference;
          break;

        default:
          using (var options = new Options() { ComputeReferences = true, IncludeNonVisibleObjects = true })
          {
            var geometry = element.get_Geometry(options);
            reference = geometry?.OfType<Solid>().
              SelectMany(x => x.Faces.Cast<Face>()).
              Select(x => x.Reference).
              OfType<Reference>().
              FirstOrDefault();
          }
          break;
      }

      return reference ?? new Reference(element);
    }
    #endregion
  }
}
