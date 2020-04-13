using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static class ElementExtension
  {
    public static GeometryElement GetGeometry(this Element element, ViewDetailLevel viewDetailLevel, out Options options)
    {
      options = new Options { ComputeReferences = true, DetailLevel = viewDetailLevel };
      var geometry = element.get_Geometry(options);

      if (!(geometry?.Any() ?? false) && element is GenericForm form && !form.Combinations.IsEmpty)
      {
        geometry.Dispose();

        options.IncludeNonVisibleObjects = true;
        return element.get_Geometry(options);
      }

      return geometry;
    }

#if !REVIT_2019
    public static IList<ElementId> GetDependentElements(this Element element, ElementFilter filter)
    {
      try
      {
        using (var transaction = new Transaction(element.Document, nameof(GetDependentElements)))
        {
          transaction.Start();

          var collection = element.Document.Delete(element.Id);
          if (filter is null)
            return collection?.ToList();

          return collection?.Where(x => filter.PassesFilter(element.Document, x)).ToList();
        }
      }
      catch { }

      return default;
    }
#endif

  }
}
