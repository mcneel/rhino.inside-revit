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
      int IEqualityComparer<Element>.GetHashCode(Element obj) => obj?.Id.GetHashCode() ?? 0;
    }

    struct InterDocumentComparer : IEqualityComparer<Element>
    {
      bool IEqualityComparer<Element>.Equals(Element x, Element y) =>  IsEquivalent(x, y);
      int IEqualityComparer<Element>.GetHashCode(Element obj) => (obj?.Id.GetHashCode() ?? 0) ^ (obj?.Document.GetHashCode() ?? 0);
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

      if (!(self?.IsValidObject is true) || !(other?.IsValidObject is true))
        return false;

      if (self?.Id != other?.Id)
        return false;

      return self.Document.Equals(other.Document);
    }
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

    #region Graphics
    public static Outline GetOutline(this Element element)
    {
      return element.GetBoundingBoxXYZ()?.ToOutLine();
    }

    public static Outline GetOutline(this Element element, bool boundVertically)
    {
      return GetOutline(element, boundVertically, boundVertically);
    }

    public static Outline GetOutline(this Element element, bool boundBottom, bool boundTop)
    {
      var outline = GetOutline(element);
      if (outline is object)
      {
        if (!boundBottom)
        {
          var (minX, minY, _) = outline.MinimumPoint;
          outline.MinimumPoint = new XYZ(minX, minY, -ElementFilters.BoundingBoxLimits);
        }

        if (!boundTop)
        {
          var (maxX, maxY, _) = outline.MaximumPoint;
          outline.MaximumPoint = new XYZ(maxX, maxY, +ElementFilters.BoundingBoxLimits);
        }
      }
      return outline;
    }

    public static bool HasBoundingBoxXYZ(this Element element)
    {
      using (var bbox = element.GetBoundingBoxXYZ())
        return bbox is object;
    }

    public static BoundingBoxXYZ GetBoundingBoxXYZ(this Element element, out View view)
    {
      view = element.ViewSpecific ? element.Document.GetElement(element.OwnerViewId) as View : default;
      return element.get_BoundingBox(view.IsModelView() ? view : default);
    }

    public static BoundingBoxXYZ GetBoundingBoxXYZ(this Element element)
    {
      using (var view = element.ViewSpecific ? element.Document.GetElement(element.OwnerViewId) as View : default)
        return element.get_BoundingBox(view.IsModelView() ? view : default);
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
            var transform = instance.Transform;
            foreach (var xyz in GetSamplePoints(instance.SymbolGeometry, view))
              yield return transform.OfPoint(xyz);

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
      var geometry = element?.get_Geometry(options);

      if (geometry?.Any() is false && element is CombinableElement combinable && !combinable.Combinations.IsEmpty)
      {
        geometry.Dispose();

        options.IncludeNonVisibleObjects = true;
        return element.get_Geometry(options);
      }

      return geometry;
    }
    #endregion

    #region Dependents

    internal static void Unproxy(ref Element element)
    {
      if (element.IsProxyElement(out var id))
        element = element.Document.GetElement(id);
    }

    public static bool IsProxyElement(this Element element, out ElementId elementId)
    {
      if (element.get_Parameter(BuiltInParameter.ID_PARAM)?.AsElementId() is ElementId id && id.IsValid())
      {
        if (element.Id != id)
        {
          elementId = id;
          return true;
        }
      }

      elementId = ElementIdExtension.Invalid;
      return false;
    }

    public static IList<ElementId> GetProxyElements(this Element element)
    {
      return element.GetDependentElements
      (
        ElementFilters.Intersect
        (
          new ExclusionFilter(new ElementId[] { element.Id }),
          new ElementParameterFilter
          (
            new FilterElementIdRule
            (
              new ParameterValueProvider(new ElementId(BuiltInParameter.ID_PARAM)),
              new FilterNumericEquals(),
              element.Id
            )
          )
        )
      );
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
      readonly UpdaterId UpdaterId;

      public ICollection<ElementId> DeletedElementIds { get; private set; } = ElementIdExtension.EmptySet;
      public ICollection<ElementId> ModifiedElementIds { get; private set; } = ElementIdExtension.EmptySet;

      public DeleteUpdater(Document document, ElementFilter filter)
      {
        UpdaterId = new UpdaterId
        (
          document.Application.ActiveAddInId,
          new Guid("9536C7C9-C58B-4D48-9103-5C8EBAA6F6C8")
        );

        UpdaterRegistry.RegisterUpdater(this, isOptional: true);

        if (filter is null)
          filter = ElementFilters.Universe;

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

    public static T[] GetDependents<T>(this Element element) where T : Element
    {
      var ids = element.GetDependentElements
      (
        ElementFilters.ExclusionFilter(element.Id).Intersect
        (ElementFilters.ElementClassFilter(typeof(T)))
      );

      var doc = element.Document;
      return ids.Select(doc.GetElement).OfType<T>().ToArray();
    }

    public static T GetFirstDependent<T>(this Element element) where T : Element
    {
      var ids = element.GetDependentElements
      (
        ElementFilters.ExclusionFilter(element.Id).Intersect
        (ElementFilters.ElementClassFilter(typeof(T)))
      );

      var doc = element.Document;
      return ids.Select(doc.GetElement).OfType<T>().FirstOrDefault();
    }

    public static bool DependsOn(this Element element, Element host)
    {
      if (!element.Document.IsEquivalent(host?.Document)) return false;
      return host?.GetDependentElements(ElementFilters.InclusionFilter(element)).Count == 1;
    }
    #endregion

    #region Parameter
    public static IEnumerable<Parameter> GetParameters(this Element element, ParameterClass set)
    {
      switch (set)
      {
        case ParameterClass.Any:
          return BuiltInParameters.Values.
            Select
            (
              x =>
              {
                try { return element.get_Parameter(x); }
                catch (Autodesk.Revit.Exceptions.InternalException) { return null; }
              }
            ).
            Where(x => x?.Definition is object).
            Union(element.Parameters.Cast<Parameter>().Where(x => x.Definition is object && x.StorageType != StorageType.None), ParameterEqualityComparer.SameDocument).
            OrderBy(x => x.Id.ToValue());

        case ParameterClass.BuiltIn:
          return BuiltInParameters.Values.
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
            Where(p => !p.IsShared && !p.Id.IsBuiltInId()).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 1).
            OrderBy(x => x.Id.ToValue());

        case ParameterClass.Family:
          return element.Parameters.Cast<Parameter>().
            Where(p => !p.IsShared && !p.Id.IsBuiltInId()).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 0).
            OrderBy(x => x.Id.ToValue());

        case ParameterClass.Shared:
          return element.Parameters.Cast<Parameter>().
            Where(p => p.IsShared).
            OrderBy(x => x.Id.ToValue());
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
            OrderBy(x => x.Id.ToValue());

        case ParameterClass.BuiltIn:
          return BuiltInParameters.TryGetByStringLocalized(name, out var parameters) ?
            parameters.Select(element.get_Parameter).Where(x => x?.Definition is object) :
            Enumerable.Empty<Parameter>();

        case ParameterClass.Project:
          return element.GetParameters(name).
            Where(p => !p.IsShared && !p.Id.IsBuiltInId()).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 1).
            OrderBy(x => x.Id.ToValue());

        case ParameterClass.Family:
          return element.GetParameters(name).
            Where(p => !p.IsShared && !p.Id.IsBuiltInId()).
            Where(p => (p.Element.Document.GetElement(p.Id) as ParameterElement)?.get_Parameter(BuiltInParameter.ELEM_DELETABLE_IN_FAMILY)?.AsInteger() == 0).
            OrderBy(x => x.Id.ToValue());

        case ParameterClass.Shared:
          return element.GetParameters(name).
            Where(p => p.IsShared).
            OrderBy(x => x.Id.ToValue());
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
      if (element is null) throw new System.ArgumentNullException("A non-optional argument was NULL", nameof(element));
      if (parameterId is null) throw new System.ArgumentNullException("A non-optional argument was NULL", nameof(parameterId));

      BuiltInParameter builtInParameter = parameterId;
      if (builtInParameter == BuiltInParameter.INVALID)
        throw new System.ArgumentException($"{nameof(parameterId)} does not identify a built-in parameter.", nameof(parameterId));

      return element.get_Parameter(builtInParameter);
    }
#endif

    public static Parameter GetParameter(this Element element, string name, ParameterClass set)
    {
      return element.GetParameters(name, set).
        OrderByDescending(x => x.HasValue).         // Order descending by HasValue to give priority to parameters that do have a value.
        ThenByDescending(x => x.Id.IsBuiltInId()).  // Then by IsBuiltInId to give priority to built-in parameters.
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

          return (T) (object) param.AsBoolean();
        }
        else if (typeof(T) == typeof(int))
        {
          if (param.StorageType != StorageType.Integer)
            throw new System.InvalidCastException();

          return (T) (object) param.AsInteger();
        }
        else if (typeof(T).IsSubclassOf(typeof(Enum)))
        {
          if (param.StorageType != StorageType.Integer)
            throw new System.InvalidCastException();

          return (T) (object) param.AsInteger();
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
      if (from?.SwapNomenWith(to, out nomenParameter) == true)
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

        using (var options = new CopyPasteOptions())
        {
          options.SetDuplicateTypeNamesAction(DuplicateTypeAction.UseDestinationTypes);

          var ids = default(ICollection<ElementId>);
          if (template.ViewSpecific)
          {
            var sourceView = sourceDocument.GetElement(template.OwnerViewId) as View;
            destinationView ??= sourceView;

            if (destinationDocument.TryGetNamesakeElement(destinationView.Document, destinationView.Id, out destinationView))
            {
              ids = ElementTransformUtils.CopyElements
              (
                sourceView, new ElementId[] { template.Id },
                destinationView, default, options
              );
            }
            else throw new InvalidOperationException($"Failed to found '{destinationView.Tooltip()}' on document '{destinationDocument.Tooltip()}'");
          }
          else
          {
            ids = ElementTransformUtils.CopyElements
            (
              sourceDocument, new ElementId[] { template.Id },
              destinationDocument, default, options
            );
          }

          return ids?.Select(destinationDocument.GetElement).OfType<T>().FirstOrDefault();
        }
      }
      catch (Autodesk.Revit.Exceptions.ApplicationException) { }

      return null;
    }
    #endregion

    #region Geometry References
    public static Reference GetDefaultReference(this Element element)
    {
      var reference = default(Reference);
      switch (element)
      {
        case null:
          return null;

#if REVIT_2018
        case FamilyInstance instance:
          reference = reference ?? instance.GetReferences(FamilyInstanceReferenceType.StrongReference).FirstOrDefault();
          reference = reference ?? instance.GetReferences(FamilyInstanceReferenceType.CenterLeftRight).FirstOrDefault();
          reference = reference ?? instance.GetReferences(FamilyInstanceReferenceType.CenterFrontBack).FirstOrDefault();
          reference = reference ?? instance.GetReferences(FamilyInstanceReferenceType.CenterElevation).FirstOrDefault();
          reference = reference ?? instance.GetReferences(FamilyInstanceReferenceType.WeakReference).FirstOrDefault();
          break;
#endif

        case CurveElement modelLine:
          reference = modelLine.GeometryCurve.Reference;
          break;

        case DatumPlane datum:
          reference = Reference.ParseFromStableRepresentation(datum.Document, $"{datum.UniqueId}:0:SURFACE");
          break;

        case SketchPlane sketchPlane:
          reference = sketchPlane.GetPlaneReference();
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

    public static GeometryObject GetGeometryObjectFromReference(this Element element, Reference reference, out Transform transform)
    {
      if (element is null)
        throw new ArgumentNullException(nameof(element));

      if (reference is null)
        throw new ArgumentNullException(nameof(reference));

      if (element.Id == reference.ElementId)
      {
        if (element is RevitLinkInstance link)
        {
          transform = link.GetTransform();
          reference = reference.CreateReferenceInLink(link);
          element = link.GetLinkDocument()?.GetElement(reference.LinkedElementId);
        }
        else transform = Transform.Identity;

        if (reference.ElementReferenceType != ElementReferenceType.REFERENCE_TYPE_NONE && element is Instance instance)
          transform *= instance.GetTransform();
      }
      else
      {
        transform = null;
        // `GetGeometryObjectFromReference` call below should rise the expected exception.
      }

      switch (element.GetGeometryObjectFromReference(reference))
      {
        case GeometryObject geometryObject: return geometryObject;
        default:

          switch (element)
          {
            case Dimension dimension:
              if (reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_LINEAR)
              {
                var stable = reference.ConvertToPersistentRepresentation(element.Document);
                if (ReferenceId.TryParse(stable, out var id, element.Document))
                {
                  if (id.Symbol.Index.Length == 1 && id.Symbol.Index[0] == int.MaxValue)
                    return dimension.GetBoundedCurve();
                }
              }
              break;
          }

          return null;
      }
    }
    #endregion
  }
}
