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
    /// Checks if id corresponds to a Category in doc
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
    /// Checks if id corresponds to a Parameter in doc
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
    /// Checks if id corresponds to a valid BuiltIn Category id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="builtInParameter"></param>
    /// <returns></returns>
    public static bool TryGetBuiltInCategory(this ElementId id, out BuiltInCategory builtInParameter)
    {
      builtInParameter = (BuiltInCategory) id.IntegerValue;
      if (builtInParameter.IsValid())
        return true;

      builtInParameter = BuiltInCategory.INVALID;
      return false;
    }

    /// <summary>
    /// Checks if id corresponds to a valid BuiltIn Parameter id
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
