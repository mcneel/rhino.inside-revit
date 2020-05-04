using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ParameterFilterElementExtension
  {
#if !REVIT_2019
    public static ElementFilter GetElementFilter(this ParameterFilterElement parameterFilter)
    {
      var filters = new List<ElementFilter>()
      {
        new ElementMulticategoryFilter(parameterFilter.GetCategories())
      };

      foreach(var rule in parameterFilter.GetRules())
        filters.Add(new ElementParameterFilter(rule));

      return new LogicalAndFilter(filters);
    }
#endif
  }
}
