using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementTypeExtensions
  {
    public static string GetFamilyName(this ElementType elementType) => UI.HostedApplication.Active.InvokeInHostContext(() => elementType.FamilyName);

    public static ISet<ElementId> GetInstances(this ElementType type)
    {
      using (var collector = new FilteredElementCollector(type.Document))
      {
        var elementCollector = collector.WhereElementIsNotElementType();

        if (type.Category?.Id is ElementId categoryId)
          elementCollector = elementCollector.WhereCategoryIdEqualsTo(categoryId);

        elementCollector.WhereTypeIdEqualsTo(type.Id);

        return elementCollector.ToReadOnlyElementIdSet();
      }
    }
  }
}
