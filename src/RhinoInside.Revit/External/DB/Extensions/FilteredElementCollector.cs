using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class FilteredElementCollectorExtension
  {
    public static FilteredElementCollector OfTypeId(this FilteredElementCollector collector, ElementId typeId)
    {
      using (var provider = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM)))
      using (var evaluator = new FilterNumericEquals())
      using (var rule = new FilterElementIdRule(provider, evaluator, typeId))
      using (var filter = new ElementParameterFilter(rule))
        return collector.WherePasses(filter);
    }
  }
}
