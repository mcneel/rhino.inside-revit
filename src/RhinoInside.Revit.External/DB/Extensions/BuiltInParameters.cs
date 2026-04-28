using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  static class BuiltInParameters
  {
    private static readonly SortedSet<BuiltInParameter> _Values =
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
    public static IReadOnlyCollection<BuiltInParameter> Values => _Values;

    /// <summary>
    /// Checks if a <see cref="Autodesk.Revit.DB.BuiltInParameter"/> is valid.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValid(this BuiltInParameter value)
    {
      if (-2000000 < (int) value && (int) value < -1000000)
        return _Values.Contains(value);

      return false;
    }

    internal static bool IsColor(this BuiltInParameter value)
    {
      var name = value.ToString();
      return name.EndsWith("_COLOR") || name.EndsWith("_COLOR_PARAM");
    }

    /// <summary>
    /// Internal Dictionary that maps <see cref="BuiltInParameter"/> by name.
    /// Results are implicitly ordered by value in the <see cref="BuiltInParameter"/> enum.
    /// </summary>
    static readonly IReadOnlyDictionary<string, IReadOnlyList<BuiltInParameter>> Localized =
      Values.
      Where
      (
        x =>
        {
          try { return !string.IsNullOrEmpty(LabelUtils.GetLabelFor(x)); }
          catch { return false; }
        }
      ).
      GroupBy(LabelUtils.GetLabelFor).
      ToDictionary(x => x.Key, x => (IReadOnlyList<BuiltInParameter>) x.ToArray());

    /// <summary>
    /// Search for all <see cref="Autodesk.Revit.DB.BuiltInParameter"/> that has the provided user-visible name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static bool TryGetByStringLocalized(string name, out IReadOnlyList<BuiltInParameter> parameters)
    {
      return Localized.TryGetValue(name, out parameters);
    }

    /// <summary>
    /// Private Dictionary that maps parameter labels by <see cref="BuiltInParameter"/>.
    /// </summary>
    static readonly IReadOnlyDictionary<BuiltInParameter, string> LocalizedValues =
      Values.
      ToDictionary(x => x, x =>
      {
        try { return LabelUtils.GetLabelFor(x); }
        catch { return string.Empty; }
      });

    /// <summary>
    /// Gets the user-visible name for a <see cref="Autodesk.Revit.DB.BuiltInParameter"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToStringLocalized(this BuiltInParameter value)
    {
      return LocalizedValues.TryGetValue(value, out var label) ? label : string.Empty;
    }

    /// <summary>
    /// <see cref="Autodesk.Revit.DB.BuiltInParameter"/> has duplicate values.
    /// This method returns the string representation of the most generic form.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToStringGeneric(this BuiltInParameter value)
    {
      switch (value)
      {
        case BuiltInParameter.GENERIC_THICKNESS: return "GENERIC_THICKNESS";
        case BuiltInParameter.GENERIC_WIDTH: return "GENERIC_WIDTH";
        case BuiltInParameter.GENERIC_HEIGHT: return "GENERIC_HEIGHT";
        case BuiltInParameter.GENERIC_DEPTH: return "GENERIC_DEPTH";
        case BuiltInParameter.GENERIC_FINISH: return "GENERIC_FINISH";
        case BuiltInParameter.GENERIC_CONSTRUCTION_TYPE: return "GENERIC_CONSTRUCTION_TYPE";
        case BuiltInParameter.FIRE_RATING: return "FIRE_RATING";
        case BuiltInParameter.ALL_MODEL_COST: return "ALL_MODEL_COST";
        case BuiltInParameter.ALL_MODEL_MARK: return "ALL_MODEL_MARK";
        case BuiltInParameter.ALL_MODEL_FAMILY_NAME: return "ALL_MODEL_FAMILY_NAME";
        case BuiltInParameter.ALL_MODEL_TYPE_NAME: return "ALL_MODEL_TYPE_NAME";
        case BuiltInParameter.ALL_MODEL_TYPE_MARK: return "ALL_MODEL_TYPE_MARK";
      }

      return value.ToString();
    }
  }
}
