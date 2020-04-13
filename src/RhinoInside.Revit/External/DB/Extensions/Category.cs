using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class CategoryExtension
  {
    /// <summary>
    /// Set of valid BuiltInCategory enum values
    /// </summary>
    internal static readonly SortedSet<BuiltInCategory> BuiltInCategories =
      new SortedSet<BuiltInCategory>
      (
        Enum.GetValues(typeof(BuiltInCategory)).
        Cast<BuiltInCategory>().Where(x => Category.IsBuiltInCategoryValid(x))
      );

    /// <summary>
    /// Checks if a BuiltInCategory is valid
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public static bool IsValid(this BuiltInCategory category)
    {
      if (-3000000 < (int) category && (int) category < -2000000)
        return BuiltInCategories.Contains(category);

      return false;
    }

    /// <summary>
    /// Check if category is in the Document or in its parent CategoryNameMap
    /// </summary>
    /// <param name="category"></param>
    /// <returns>true in case is not found</returns>
    public static bool IsHidden(this Category category)
    {
      var map = (category.Parent is Category parent) ?
                 parent.SubCategories :
                 category.Document()?.Settings.Categories;

      if (map is null)
        return true;

      return !map.Cast<Category>().Where(x => x.Id.IntegerValue == category.Id.IntegerValue).Any();
    }

    public static Document Document(this Category category)
    {
      return category?.GetGraphicsStyle(GraphicsStyleType.Projection).Document;
    }
  }
}
