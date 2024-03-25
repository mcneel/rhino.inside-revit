using System;
using System.Collections.Generic;
using System.Linq;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Schemas
{
  /// <summary>
  /// Represents an Autodesk.Revit.DB.BuiltInCategory
  /// </summary>
  public partial class CategoryId : DataType
  {
    public static new CategoryId Empty { get; } = new CategoryId(ARDB.BuiltInCategory.INVALID, string.Empty);

    public override string LocalizedLabel => IsNullOrEmpty(this) ? string.Empty :
#if REVIT_2020
        ARDB.LabelUtils.GetLabelFor((ARDB.BuiltInCategory) this);
#else
        Label;
#endif

    CategoryId(string id) : base(id) { }

    internal CategoryId(ARDB.BuiltInCategory bic, string id) : base(id) => BuiltInCategory = bic;

    #region IParsable
    public static bool TryParse(string s, IFormatProvider provider, out CategoryId result)
    {
      if (IsCategoryId(s))
      {
        result = new CategoryId(s);
        return true;
      }

      result = default;
      return false;
    }

    public static CategoryId Parse(string s, IFormatProvider provider)
    {
      if (!TryParse(s, provider, out var result)) throw new FormatException($"{nameof(s)} is not in the correct format.");
      return result;
    }

    static bool IsCategoryId(string id)
    {
      return id == string.Empty || // '<None>'
             id.StartsWith("autodesk.revit.category");
    }
    #endregion

    public static bool IsCategoryId(DataType value, out CategoryId categoryId)
    {
      var typeId = value.TypeId;
      if (IsCategoryId(typeId))
      {
        categoryId = new CategoryId(typeId);
        return true;
      }

      categoryId = default;
      return false;
    }

    private static readonly HashSet<CategoryId> Values = new HashSet<CategoryId>
    (
      typeof(CategoryId).
      GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).
      Where(x => x.PropertyType == typeof(CategoryId)).
      Select(x => (CategoryId) x.GetValue(null))
    );

    internal static readonly IReadOnlyDictionary<ARDB.BuiltInCategory, CategoryId> BuiltIn = Values.ToDictionary(k => k.BuiltInCategory);
    public static implicit operator CategoryId(ARDB.BuiltInCategory value) => BuiltIn.TryGetValue(value, out var id) ? id : Empty;

    private ARDB.BuiltInCategory BuiltInCategory = default;
    public static implicit operator ARDB.BuiltInCategory(CategoryId value) => value.BuiltInCategory != default ? value.BuiltInCategory :
      (value.BuiltInCategory = Values.TryGetValue(value, out var bi) ? bi.BuiltInCategory : ARDB.BuiltInCategory.INVALID);

#if REVIT_2021
    public static implicit operator CategoryId(ARDB.ForgeTypeId value) => value is null ? null : IsCategoryId(value.TypeId) ? new CategoryId(value.TypeId) : throw new InvalidCastException();
    public static implicit operator ARDB.ForgeTypeId(CategoryId value) => value is null ? null : new ARDB.ForgeTypeId(value.TypeId);
#endif
  }
}
