using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BuiltInParameterExtension
  {
    private static readonly SortedSet<BuiltInParameter> builtInParameters =
      new SortedSet<BuiltInParameter>
      (
        Enum.GetValues(typeof(BuiltInParameter)).
        Cast<BuiltInParameter>().Where( x => x != BuiltInParameter.INVALID)
      );

    /// <summary>
    /// Set of valid <see cref="Autodesk.Revit.DB.BuiltInParameter"/> enum values.
    /// </summary>
    public static IReadOnlyCollection<BuiltInParameter> BuiltInParameters => builtInParameters;

    /// <summary>
    /// Checks if a <see cref="Autodesk.Revit.DB.BuiltInParameter"/> is valid.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValid(this BuiltInParameter value)
    {
      if (-2000000 < (int) value && (int) value < -1000000)
        return builtInParameters.Contains(value);

      return false;
    }

    /// <summary>
    /// Internal Dictionary that maps <see cref="BuiltInParameter"/> by name.
    /// Results are implicitly orderd by value in the <see cref="BuiltInParameter"/> enum.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, BuiltInParameter[]> BuiltInParameterMap =
      Enum.GetValues(typeof(BuiltInParameter)).
      Cast<BuiltInParameter>().
      Where
      (
        x =>
        {
          try { return !string.IsNullOrEmpty(LabelUtils.GetLabelFor(x)); }
          catch { return false; }
        }
      ).
      GroupBy(x => LabelUtils.GetLabelFor(x)).
      ToDictionary(x => x.Key, x=> x.ToArray());

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

  public static class ParameterTypeExtension
  {
    public static StorageType ToStorageType(this ParameterType parameterType)
    {
      switch (parameterType)
      {
        case ParameterType.Invalid:
          return StorageType.None;
        case ParameterType.Text:
        case ParameterType.MultilineText:
        case ParameterType.URL:
          return StorageType.String;
        case ParameterType.YesNo:
        case ParameterType.Integer:
        case ParameterType.LoadClassification:
          return StorageType.Integer;
        case ParameterType.Material:
        case ParameterType.FamilyType:
        case ParameterType.Image:
          return StorageType.ElementId;
        case ParameterType.Number:
        default:
          return StorageType.Double;
      }
    }
  }

  public static class ParameterExtension
  {
    public static bool ResetValue(this Parameter parameter)
    {
      if (!parameter.HasValue)
        return true;

#if REVIT_2020
      if (parameter.IsShared && (parameter.Definition as ExternalDefinition).HideWhenNoValue)
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
  }
}
