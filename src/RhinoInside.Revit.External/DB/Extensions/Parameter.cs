using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using External.DB.Schemas;

  internal static class ParameterEqualityComparer
  {
    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Parameter"/>
    /// that compares categories from different <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static readonly IEqualityComparer<Parameter> InterDocument = default(InterDocumentComparer);

    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Parameter"/>
    /// that assumes all parameters are from the same <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static readonly IEqualityComparer<Parameter> SameDocument = default(SameDocumentComparer);

    struct SameDocumentComparer : IEqualityComparer<Parameter>
    {
      public bool Equals(Parameter x, Parameter y) => x.Id == y.Id;
      public int GetHashCode(Parameter obj) => obj?.Id.GetHashCode() ?? 0;
    }

    struct InterDocumentComparer : IEqualityComparer<Parameter>
    {
      bool IEqualityComparer<Parameter>.Equals(Parameter x, Parameter y) => IsEquivalent(x, y);
      int IEqualityComparer<Parameter>.GetHashCode(Parameter obj) => (obj?.Id.GetHashCode() ?? 0) ^ (obj?.Element.Document.GetHashCode() ?? 0);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.Parameter"/> equals
    /// to this <see cref="Autodesk.Revit.DB.Parameter"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Parameter"/> instances are considered equivalent
    /// if they represent the same element parameter in this Revit session.
    /// </remarks>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsEquivalent(this Parameter self, Parameter other)
    {
      if (ReferenceEquals(self, other))
        return true;

      if (self?.Id != other?.Id)
        return false;

      if (!self.Element.IsValidObject || !other.Element.IsValidObject)
        return false;

      return self.Element.Document.Equals(other.Element.Document);
    }
  }

  static class BuiltInParameterExtension
  {
    private static readonly SortedSet<BuiltInParameter> _BuiltInParameters =
      new SortedSet<BuiltInParameter>
      (
        Enum.GetValues(typeof(BuiltInParameter)).
        Cast<BuiltInParameter>().
        Distinct().                               // Removes Duplicates
        Where(x => x != BuiltInParameter.INVALID) // Removes INVALID
      );

    /// <summary>
    /// Set of valid <see cref="Autodesk.Revit.DB.BuiltInParameter"/> enum values.
    /// </summary>
    public static IReadOnlyCollection<BuiltInParameter> BuiltInParameters => _BuiltInParameters;

    /// <summary>
    /// Checks if a <see cref="Autodesk.Revit.DB.BuiltInParameter"/> is valid.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValid(this BuiltInParameter value)
    {
      if (-2000000 < (int) value && (int) value < -1000000)
        return _BuiltInParameters.Contains(value);

      return false;
    }

    /// <summary>
    /// Internal Dictionary that maps <see cref="BuiltInParameter"/> by name.
    /// Results are implicitly orderd by value in the <see cref="BuiltInParameter"/> enum.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<BuiltInParameter>> BuiltInParameterMap =
      BuiltInParameters.
      Where
      (
        x =>
        {
          try { return !string.IsNullOrEmpty(LabelUtils.GetLabelFor(x)); }
          catch { return false; }
        }
      ).
      GroupBy(x => LabelUtils.GetLabelFor(x)).
      ToDictionary(x => x.Key, x => (IReadOnlyList<BuiltInParameter>) x.ToArray());

    /// <summary>
    /// <see cref="Autodesk.Revit.DB.BuiltInParameter"/> has duplicate values.
    /// This method returns the string representatiopn of the most generic form.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToStringGeneric(this BuiltInParameter value)
    {
      switch (value)
      {
        case BuiltInParameter.GENERIC_THICKNESS:          return "GENERIC_THICKNESS";
        case BuiltInParameter.GENERIC_WIDTH:              return "GENERIC_WIDTH";
        case BuiltInParameter.GENERIC_HEIGHT:             return "GENERIC_HEIGHT";
        case BuiltInParameter.GENERIC_DEPTH:              return "GENERIC_DEPTH";
        case BuiltInParameter.GENERIC_FINISH:             return "GENERIC_FINISH";
        case BuiltInParameter.GENERIC_CONSTRUCTION_TYPE:  return "GENERIC_CONSTRUCTION_TYPE";
        case BuiltInParameter.FIRE_RATING:                return "FIRE_RATING";
        case BuiltInParameter.ALL_MODEL_COST:             return "ALL_MODEL_COST";
        case BuiltInParameter.ALL_MODEL_MARK:             return "ALL_MODEL_MARK";
        case BuiltInParameter.ALL_MODEL_FAMILY_NAME:      return "ALL_MODEL_FAMILY_NAME";
        case BuiltInParameter.ALL_MODEL_TYPE_NAME:        return "ALL_MODEL_TYPE_NAME";
        case BuiltInParameter.ALL_MODEL_TYPE_MARK:        return "ALL_MODEL_TYPE_MARK";
      }

      return value.ToString();
    }
  }

  static class ParameterExtension
  {
    public static bool AsBoolean(this Parameter parameter)
    {
      if (parameter.Definition.GetDataType() != SpecType.Boolean.YesNo)
        throw new InvalidCastException();

      return parameter.AsInteger() != 0;
    }

    public static T AsEnum<T>(this Parameter parameter) where T : Enum
    {
      if (parameter.Definition.GetDataType() != SpecType.Int.Integer)
        throw new InvalidCastException();

      return (T) (object) parameter.AsInteger();
    }

    public static Color AsColor(this Parameter parameter)
    {
      if (parameter.Definition.GetDataType() != SpecType.Int.Integer || !parameter.Id.ToBuiltInParameter().ToString().EndsWith("_COLOR"))
        throw new InvalidCastException();

      var abgr = parameter.AsInteger();
      return new Color((byte) ((abgr >> 0) & byte.MaxValue), (byte) ((abgr >> 8) & byte.MaxValue), (byte) ((abgr >> 16) & byte.MaxValue));
    }

    public static bool Update(this Parameter parameter, bool value)
    {
      if (parameter.HasValue && parameter.AsInteger() == (value ? 1 : 0)) return true;
      return parameter.Set(value ? 1 : 0);
    }

    public static bool Update<T>(this Parameter parameter, T value) where T : Enum
    {
      if (parameter.HasValue && parameter.AsInteger() == (int) (object) value) return true;
      return parameter.Set((int) (object) value);
    }

    public static bool Update(this Parameter parameter, int value)
    {
      if (parameter.HasValue && parameter.AsInteger() == value) return true;
      return parameter.Set(value);
    }

    public static bool Update(this Parameter parameter, Color value)
    {
      if (parameter.Definition.GetDataType() != SpecType.Int.Integer || !parameter.Id.ToBuiltInParameter().ToString().EndsWith("_COLOR"))
        throw new InvalidCastException();

      var abgr = (value.Blue << 16) | (value.Green << 8) | (value.Red << 0);
      if (parameter.HasValue && parameter.AsInteger() == abgr) return true;
      return parameter.Set(abgr);
    }

    public static bool Update(this Parameter parameter, double value)
    {
      if (parameter.HasValue && parameter.AsDouble() == value) return true;
      return parameter.Set(value);
    }

    public static bool Update(this Parameter parameter, string value)
    {
      if (parameter.HasValue && parameter.AsString() == value) return true;
      return parameter.Set(value);
    }

    public static bool Update(this Parameter parameter, ElementId value)
    {
      if (parameter.HasValue && parameter.AsElementId() == value) return true;

      // `DB.Parameter.Set` does not validate `value` is a valid type for `parameter.Element`.
      // Revit editor crashes when that element with a wrong type is selected.
      if (parameter.GetTypeId() == Schemas.ParameterId.ElemTypeParam)
      {
        if (!parameter.Element.GetValidTypes().Contains(value))
          return false;

        if
        (
          parameter.Element.Document.GetElement(parameter.Element.GetTypeId()).GetType() !=
          parameter.Element.Document.GetElement(value).GetType()
        )
          return false;

        return parameter.Element.ChangeTypeId(value) == ElementId.InvalidElementId;
      }

      return parameter.Set(value);
    }

    public static bool ResetValue(this Parameter parameter)
    {
      if (!parameter.HasValue)
        return true;

#if REVIT_2020
      if
      (
        parameter.IsShared &&
        (parameter.Element.Document.GetElement(parameter.Id) as SharedParameterElement)?.ShouldHideWhenNoValue() == true
      )
        return parameter.ClearValue();
#endif

      switch (parameter.StorageType)
      {
        case StorageType.Integer:   return parameter.AsInteger() == 0                            || parameter.Set(0);
        case StorageType.Double:    return parameter.AsDouble() == 0.0                           || parameter.Set(0.0);
        case StorageType.String:    return parameter.AsString() == string.Empty                  || parameter.Set(string.Empty);
        case StorageType.ElementId: return parameter.AsElementId() == ElementId.InvalidElementId || parameter.Set(ElementId.InvalidElementId);
      }

      return false;
    }

#if !REVIT_2022
    public static Schemas.ParameterId GetTypeId(this Parameter parameter)
    {
      if (parameter.Id.TryGetBuiltInParameter(out var builtInParameter))
        return builtInParameter;

      if(parameter.IsShared)
        return new Schemas.ParameterId($"autodesk.parameter.aec.revit.external.-1:{parameter.GUID:N}");
      else
        return new Schemas.ParameterId($"autodesk.parameter.aec.revit.project:{parameter.Id.ToValue()}");
    }
#endif
  }

  static class InternalDefinitionExtension
  {
    public static ParameterScope GetParameterScope(this InternalDefinition self, Document doc)
    {
      if (doc is object)
      {
        if (doc.IsFamilyDocument)
        {
          if (doc.FamilyManager.get_Parameter(self) is FamilyParameter parameter)
            return parameter.IsInstance ? ParameterScope.Instance : ParameterScope.Type;
        }
        else if (!self.Id.IsBuiltInId())
        {
          if (doc.GetElement(self.Id) is GlobalParameter) return ParameterScope.Global;
          switch (doc.ParameterBindings.get_Item(self))
          {
            case InstanceBinding _: return ParameterScope.Instance;
            case TypeBinding _:     return ParameterScope.Type;
          }
        }
      }

      return ParameterScope.Unknown;
    }
  }
}
