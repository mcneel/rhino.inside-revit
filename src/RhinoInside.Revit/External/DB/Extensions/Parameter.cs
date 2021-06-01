using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  static class BuiltInParameterExtension
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

  static class ParameterExtension
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

#if !REVIT_2022
    public static Schemas.ParameterId GetTypeId(this Parameter parameter)
    {
      if (parameter.Id.TryGetBuiltInParameter(out var builtInParameter))
        return builtInParameter;

      if(parameter.IsShared)
        return new Schemas.ParameterId($"autodesk.parameter.aec.revit.external.-1:{parameter.GUID:N}");
      else
        return new Schemas.ParameterId($"autodesk.parameter.aec.revit.project:{parameter.Id.IntegerValue}");
    }
#endif

    public static IConvertible ToConvertible(this Parameter parameter)
    {
      switch (parameter.StorageType)
      {
        case StorageType.Integer:
          var integer = parameter.AsInteger();

          if (parameter.Definition is Definition definition)
          {
            var dataType = definition.GetDataType();

            if (dataType == Schemas.SpecType.Boolean.YesNo)
              return integer != 0;

            if (parameter.Id.TryGetBuiltInParameter(out var builtInInteger))
            {
              var builtInIntegerName = builtInInteger.ToString();
              if (builtInIntegerName.Contains("COLOR_") || builtInIntegerName.Contains("_COLOR_") || builtInIntegerName.Contains("_COLOR"))
              {
                int r = integer % 256;
                integer /= 256;
                int g = integer % 256;
                integer /= 256;
                int b = integer % 256;

                return System.Drawing.Color.FromArgb(r, g, b).ToArgb();
              }
            }
          }

          return integer;

        case StorageType.Double:
          var value = parameter.AsDouble();
          return Schemas.SpecType.IsMeasurableSpec(parameter.Definition.GetDataType(), out var spec) ?
            Convert.Geometry.UnitConverter.InRhinoUnits(value, spec) :
            value;

        case StorageType.String:
          return parameter.AsString();

        case StorageType.ElementId:

          var document = parameter.Element?.Document;
          var documentGUID = document.GetFingerprintGUID();
          var elementId = parameter.AsElementId();

          return elementId.IsBuiltInId() ?
            FullUniqueId.Format(documentGUID, UniqueId.Format(Guid.Empty, elementId.IntegerValue)) :
            document?.GetElement(elementId) is Element element ?
            FullUniqueId.Format(documentGUID, element.UniqueId) :
            FullUniqueId.Format(Guid.Empty, UniqueId.Format(Guid.Empty, ElementId.InvalidElementId.IntegerValue));

        default:
          throw new NotImplementedException();
      }
    }
  }
}
