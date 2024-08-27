using ARDB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB;
using RhinoInside.Revit.External.DB.Extensions;
using System;

namespace RhinoInside.Revit.Convert.DocObjects
{
  internal static class NameConverter
  {
    const string PS = "::"; // Path Separator

    /// <summary>
    /// Escape an <see cref="ARDB.Element"/> name using
    /// "Project::CategoryFullName[::FamilyName::TypeName][::Nomen] FullUniqueId" format.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    internal static string EscapeName(ARDB.Element element, out string description)
    {
      description = string.Empty;

      var hidden = false;
      var modelName = element.Document.GetTitle();
      var categoryName = element.Category?.FullName() ?? "~";
      var familyName = "~";
      var typeName = "~";
      var familyAndType = $"{PS}~{PS}~";
      var elementNomen = string.Empty;
      var uniqueId = FullUniqueId.Format(element.Document.GetPersistentGUID(), element.UniqueId);

      if (element is ARDB.ElementType type)
      {
        if (!string.IsNullOrWhiteSpace(type.FamilyName)) familyName = type.FamilyName;
        if (!string.IsNullOrWhiteSpace(type.Name)) typeName = type.Name;

        familyAndType = $"{PS}{familyName}{PS}{typeName}";
        description = element.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString() ?? string.Empty;
        hidden = type.Category?.CategoryType != ARDB.CategoryType.Model;
      }
      else
      {
        if (element.CanHaveTypeAssigned())
        {
          if (element.Document.GetElement(element.GetTypeId()) is ARDB.ElementType elementType)
          {
            if (!string.IsNullOrWhiteSpace(elementType.FamilyName)) familyName = elementType.FamilyName;
            if (!string.IsNullOrWhiteSpace(elementType.Name)) typeName = elementType.Name;

            familyAndType = $"{PS}{familyName}{PS}{typeName}";
            description = elementType.get_Parameter(ARDB.BuiltInParameter.ALL_MODEL_DESCRIPTION)?.AsString() ?? string.Empty;
            hidden = elementType.Category?.CategoryType != ARDB.CategoryType.Model;
          }
          else hidden = true;
        }
        else familyAndType = string.Empty;

        elementNomen = $"{PS}{element.GetElementNomen(out var nomenParameter)}";
        if (nomenParameter == ARDB.BuiltInParameter.INVALID) elementNomen = string.Empty;
      }

      var name = $"{(hidden ? "*" : "")}{modelName}{PS}{categoryName.Escape(':')}{familyAndType}{elementNomen} {{{uniqueId}}}";
      return name.ToControlEscaped();
    }

    internal static bool TryUnescapeName(string value, out string project, out string category, out string familyName, out string typeName, out string nomen, out string uniqueId)
    {
      project = category = familyName = typeName = nomen = uniqueId = null;
      if (!string.IsNullOrEmpty(value))
      {
        value = value.ToControlUnescaped();

        var index = value.LastIndexOf(' ');
        if (index < 0) return false;

        var tokens = value.Substring(0, index).Split(new string[] { PS }, StringSplitOptions.None);
        if (2 <= tokens.Length && tokens.Length <= 5)
        {
          project = tokens[0];
          category = tokens[1].Unescape(':');

          if (tokens.Length < 2)        return false;
          else if (tokens.Length == 3)  { nomen = tokens[2]; }
          else if (tokens.Length >= 4)  { familyName = tokens[2]; typeName = tokens[3]; }
          if (tokens.Length == 5)       { nomen = tokens[4]; }
          else                          return false;

          return true;
        }
      }

      return false;
    }
  }
}
