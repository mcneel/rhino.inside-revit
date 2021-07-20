using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ParameterFilterElementExtension
  {
#if !REVIT_2019
    public static ElementFilter GetElementFilter(this ParameterFilterElement self)
    {
      var filters = new List<ElementFilter>()
      {
        new ElementMulticategoryFilter(self.GetCategories())
      };

      foreach(var rule in self.GetRules())
        filters.Add(new ElementParameterFilter(rule));

      return new LogicalAndFilter(filters);
    }

    public static bool SetElementFilter(this ParameterFilterElement self, ElementFilter elementFilter)
    {
      return false;
    }
#endif
  }
}
