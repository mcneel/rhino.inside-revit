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
    public static IEnumerable<Parameter> GetParameters(this Element element, ParameterClass set)
    {
      switch (set)
      {
        case ParameterClass.Any:
          return Enum.GetValues(typeof(BuiltInParameter)).
            Cast<BuiltInParameter>().
            Select
            (
              x =>
              {
                try { return element.get_Parameter(x); }
                catch (Autodesk.Revit.Exceptions.InternalException) { return null; }
              }
            ).
            Where(x => x?.Definition is object).
            Union(element.Parameters.Cast<Parameter>().Where(x => x.StorageType != StorageType.None).OrderBy(x => x.Id.IntegerValue)).
            GroupBy(x => x.Id).
            Select(x => x.First());
        case ParameterClass.BuiltIn:
          return Enum.GetValues(typeof(BuiltInParameter)).
            Cast<BuiltInParameter>().
            GroupBy(x => x).
            Select(x => x.First()).
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
            Union(element.GetParameters(name).OrderBy(x => x.Id.IntegerValue)).
            GroupBy(x => x.Id).
            Select(x => x.First());

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
      if (ReferenceEquals(to, from) || from is null || to is null)
        return;

      if (!from.Document.Equals(to.Document))
        throw new System.InvalidOperationException();

      if (to.Id == from.Id)
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
            case StorageType.Integer: param.Set(previousParameter.AsInteger()); break;
            case StorageType.Double: param.Set(previousParameter.AsDouble()); break;
            case StorageType.String: param.Set(previousParameter.AsString()); break;
            case StorageType.ElementId: param.Set(previousParameter.AsElementId()); break;
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

    public static void SetParameterValue(this Element element, BuiltInParameter paramId, bool value)
    {
      using (var param = element.get_Parameter(paramId))
      {
        if (param is null)
          throw new System.InvalidOperationException();

        if(param.StorageType != StorageType.Integer || (Schemas.DataType) param.Definition.GetDataType() != Schemas.SpecType.Boolean.YesNo)
          throw new System.InvalidCastException();

        param.Set(value ? 1 : 0);
      }
    }

    public static void SetParameterValue(this Element element, BuiltInParameter paramId, object value)
    {
      var param = element.get_Parameter(paramId);
      if (param != null)
      {
        switch (value)
        {
          case int intVal:
            if (StorageType.Integer == param.StorageType)
              param.Set(intVal);
            break;
          case string strVal:
            if (StorageType.String == param.StorageType)
              param.Set(strVal);
            break;
          case double dblVal:
            if (StorageType.Double == param.StorageType)
              param.Set(dblVal);
            break;
          case ElementId idVal:
            if (StorageType.ElementId == param.StorageType)
              param.Set(idVal);
            break;
        }
      }
    }
    #endregion
  }
}
