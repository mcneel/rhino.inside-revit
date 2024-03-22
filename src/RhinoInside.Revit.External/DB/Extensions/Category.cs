using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  internal static class CategoryEqualityComparer
  {
    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Category"/>
    /// that compares categories from different <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static readonly IEqualityComparer<Category> InterDocument = new InterDocumentComparer();

    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Category"/>
    /// that assumes all categories are from the same <see cref="Autodesk.Revit.DB.Document"/>.
    /// </summary>
    public static readonly IEqualityComparer<Category> SameDocument = new SameDocumentComparer();

    struct SameDocumentComparer : IEqualityComparer<Category>
    {
      bool IEqualityComparer<Category>.Equals(Category x, Category y) => ReferenceEquals(x, y) || x?.Id == y?.Id;
      int IEqualityComparer<Category>.GetHashCode(Category obj) => obj?.Id.GetHashCode() ?? 0;
    }

    struct InterDocumentComparer : IEqualityComparer<Category>
    {
      bool IEqualityComparer<Category>.Equals(Category x, Category y) => IsEquivalent(x, y);
      int IEqualityComparer<Category>.GetHashCode(Category obj) => (obj?.Id.GetHashCode() ?? 0) ^ (obj?.Document().GetHashCode() ?? 0);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Autodesk.Revit.DB.Category"/> equals
    /// to this <see cref="Autodesk.Revit.DB.Category"/>.
    /// </summary>
    /// <remarks>
    /// Two <see cref="Category"/> instances are considered equivalent
    /// if they represent the same category in this Revit session.
    /// </remarks>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsEquivalent(this Category self, Category other)
    {
      if (ReferenceEquals(self, other))
        return true;

      if (self?.Id != other?.Id)
        return false;

      return self.Document().Equals(other.Document());
    }
  }

  public static class CategoryExtension
  {
    /// <summary>
    /// Identifies if the category is visible to the user and should be displayed in UI.
    /// </summary>
    /// <param name="category"></param>
    /// <returns>True if the category should be displayed in UI.</returns>
    public static bool IsVisibleInUI(this Category category)
    {
#if REVIT_2020
      return category.IsVisibleInUI;
#else
      switch (category.Id.ToBuiltInCategory())
      {
        case BuiltInCategory.OST_Materials:
        case BuiltInCategory.OST_RvtLinks:
          return false;
      }

      var map = (category.Parent is Category parent) ?
                 parent.SubCategories :
                 category.Document()?.Settings.Categories;

      if (map is null)
        return false;

      // There are categories with a duplicate name so we also need to check is same Id.
      if (map.Contains(category.Name) && map.get_Item(category.Name).Id == category.Id)
        return true;

      // There are built in categories that are not indexed by name so we look for BuiltInCategory.
      if (map is Categories categories && category.Id.TryGetBuiltInCategory(out var bic))
      {
        try { return categories.get_Item(bic) is object; }
        catch { }
      }

      return false;
#endif
    }

    /// <summary>
    /// Returns the <see cref="Autodesk.Revit.DB.Document"/> in which <paramref name="category"/> resides.
    /// </summary>
    /// <param name="category"></param>
    public static Document Document(this Category category)
    {
      return category?.GetGraphicsStyle(GraphicsStyleType.Projection)?.Document;
    }

    /// <summary>
    /// Return the <paramref name="category"/> full name. If it is a subCategory this will be "{ParentName}\{SubcategoryName}"
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public static string FullName(this Category category)
    {
      return category.Parent is null ? category.Name : $"{category.Parent.Name}\\{category.Name}";
    }

    /// <summary>
    /// Gets the BuiltInCategory value for this category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns>BuiltInCategory value for the category or INVALID if the category is not a built-in category.</returns>
    public static BuiltInCategory ToBuiltInCategory(this Category category)
    {
#if REVIT_2023
      return category.BuiltInCategory;
#else
      return category.Id.ToBuiltInCategory();
#endif
    }
  }
}
