using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class BuiltInParameterExtension
  {
    /// <summary>
    /// Checks if a BuiltInParameter is valid
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public static bool IsValid(this BuiltInParameter parameter)
    {
      if (-2000000 < (int) parameter && (int) parameter < -1000000)
        return Enum.IsDefined(typeof(BuiltInParameter), parameter);

      return false;
    }

    internal static readonly IDictionary<string, BuiltInParameter[]> BuiltInParameterMap =
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
      if (parameter.Id.IsBuiltInId())
        throw new InvalidOperationException("BuiltIn parameters can not be reseted");

      if (parameter.HasValue)
      {
#if REVIT_2020
        if (parameter.IsShared && (parameter.Definition as ExternalDefinition).HideWhenNoValue)
          return parameter.ClearValue();
#endif
        switch (parameter.StorageType)
        {
          case StorageType.Integer: parameter.Set(0); break;
          case StorageType.Double: parameter.Set(0.0); break;
          case StorageType.String: parameter.Set(string.Empty); break;
          case StorageType.ElementId: parameter.Set(ElementId.InvalidElementId); break;
        }
      }

      return true;
    }
  }
}
