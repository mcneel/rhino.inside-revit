using System;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents a Revit parameter discipline
  /// </summary>
  public partial class DisciplineType : DataType
  {
    public static new DisciplineType Empty { get; } = new DisciplineType();

    public override string LocalizedLabel => IsNullOrEmpty(this) ? string.Empty :
#if REVIT_2022
      Autodesk.Revit.DB.LabelUtils.GetLabelForDiscipline(this);
#else
      Label;
#endif

    public DisciplineType() { }
    public DisciplineType(string id) : base(id)
    {
      if (!IsDisciplineType(id, empty: true))
        throw new ArgumentException("Invalid argument value", nameof(id));
    }

    #region IParsable
    public static bool TryParse(string s, IFormatProvider provider, out DisciplineType result)
    {
      if (IsDisciplineType(s, empty: true))
      {
        result = new DisciplineType(s);
        return true;
      }

      result = default;
      return false;
    }

    public static DisciplineType Parse(string s, IFormatProvider provider)
    {
      if (!TryParse(s, provider, out var result)) throw new FormatException($"{nameof(s)} is not in the correct format.");
      return result;
    }

    static bool IsDisciplineType(string id, bool empty)
    {
      return (empty && id == string.Empty) || // '<None>'
             id.StartsWith("autodesk.spec:discipline") || id.StartsWith("autodesk.spec.discipline");
    }
    #endregion

    public static bool IsDisciplineType(DataType value, out DisciplineType disciplineType)
    {
      switch (value)
      {
        case DisciplineType dt: disciplineType = dt; return true;
        default:

          var typeId = value.TypeId;
          if (IsDisciplineType(typeId, empty: false))
          {
            disciplineType = new DisciplineType(typeId);
            return true;
          }

          disciplineType = default;
          return false;
      }
    }

#if REVIT_2021
    public static implicit operator Autodesk.Revit.DB.ForgeTypeId(DisciplineType value) => value is null ? null : new Autodesk.Revit.DB.ForgeTypeId(value.TypeId);
    public static implicit operator DisciplineType(Autodesk.Revit.DB.ForgeTypeId value)
    {
      if (value is null) return null;
      var typeId = value.TypeId;
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
      return IsDisciplineType(typeId, empty: true) ?
        new DisciplineType(typeId) :
        throw new InvalidCastException($"'{typeId}' is not a valid {typeof(DisciplineType)}");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
#endif
  }
}
