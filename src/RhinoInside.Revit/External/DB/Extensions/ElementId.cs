using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementIdExtension
  {
    public static bool IsValid(this ElementId id) => id is object && id != ElementId.InvalidElementId;
    public static bool IsBuiltInId(this ElementId id) => id is object && id <= ElementId.InvalidElementId;

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to a <see cref="Autodesk.Revit.DB.Category"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsCategoryId(this ElementId id, Document doc)
    {
      // Check if is not a BuiltIn Category
      if (id.IntegerValue > ElementId.InvalidElementId.IntegerValue)
      {
        try { return Category.GetCategory(doc, id) is object; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      return ((BuiltInCategory) id.IntegerValue).IsValid();
    }

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to a <see cref="Autodesk.Revit.DB.Parameter"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsParameterId(this ElementId id, Document doc)
    {
      // Check if is not a BuiltIn Parameter
      if (id.IntegerValue > ElementId.InvalidElementId.IntegerValue)
      {
        try { return doc.GetElement(id) is ParameterElement; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      return ((BuiltInParameter) id.IntegerValue).IsValid();
    }

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to a <see cref="Autodesk.Revit.DB.LinePatternElement"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsLinePatternId(this ElementId id, Document doc)
    {
      // Check if is not a BuiltIn Parameter
      if (id.IntegerValue > ElementId.InvalidElementId.IntegerValue)
      {
        try { return doc.GetElement(id) is LinePatternElement; }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException) { return false; }
      }

      // Solid line pattern.
      return id.IntegerValue == -3000010;
    }

    /// <summary>
    /// Checks if <paramref name="id"/> corresponds to an <see cref="Autodesk.Revit.DB.ElementType"/> in <paramref name="doc"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    public static bool IsElementTypeId(this ElementId id, Document doc)
    {
      using (var filter = new ElementIsElementTypeFilter())
      {
        return filter.PassesFilter(doc, id);
      }
    }

    /// <summary>
    /// Checks if id corresponds to a valid <see cref="Autodesk.Revit.DB.BuiltInCategory"/> id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="builtInCategory"></param>
    /// <returns></returns>
    public static bool TryGetBuiltInCategory(this ElementId id, out BuiltInCategory builtInCategory)
    {
      builtInCategory = (BuiltInCategory) id.IntegerValue;
      if (builtInCategory.IsValid())
        return true;

      builtInCategory = BuiltInCategory.INVALID;
      return false;
    }

    /// <summary>
    /// Checks if id corresponds to a valid <see cref="Autodesk.Revit.DB.BuiltInParameter"/> id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="builtInParameter"></param>
    /// <returns></returns>
    public static bool TryGetBuiltInParameter(this ElementId id, out BuiltInParameter builtInParameter)
    {
      builtInParameter = (BuiltInParameter) id.IntegerValue;
      if (builtInParameter.IsValid())
        return true;

      builtInParameter = BuiltInParameter.INVALID;
      return false;
    }
  }
}
