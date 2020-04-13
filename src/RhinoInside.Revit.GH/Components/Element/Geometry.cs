using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class ElementGeometry : Component
  {
    public override Guid ComponentGuid => new Guid("B7E6A82F-684F-4045-A634-A4AA9F7427A8");

    public ElementGeometry()
    : base("Element Geometry", "Geometry", "Get the geometry of the specified Element", "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", "Element to query", GH_ParamAccess.item);
      manager[manager.AddParameter(new Parameters.Param_Enum<Types.ViewDetailLevel>(), "DetailLevel", "LOD", "Geometry Level of detail LOD [1, 3]", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddGeometryParameter("Geometry", "G", "Element geometry", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(DB.Element);
      if (!DA.GetData("Element", ref element))
        return;

      var detailLevel = DB.ViewDetailLevel.Undefined;
      DA.GetData(1, ref detailLevel);
      if (detailLevel == DB.ViewDetailLevel.Undefined)
        detailLevel = DB.ViewDetailLevel.Coarse;

      if (element.get_BoundingBox(null) is DB.BoundingBoxXYZ)
      {
        DB.Options options = null;
        using (var geometry = element?.GetGeometry(detailLevel, out options)) using (options)
        {
          var list = geometry?.ToRhino().Where(x => x is object).ToList();

          if (!list.Any())
          {
            foreach (var dependent in element.GetDependentElements(null).Select(x => element.Document.GetElement(x)))
            {
              if (dependent.get_BoundingBox(null) is DB.BoundingBoxXYZ)
              {
                DB.Options dependentOptions = null;
                using (var dependentGeometry = dependent?.GetGeometry(detailLevel, out dependentOptions)) using (dependentOptions)
                {
                  if (dependentGeometry is object)
                    list.AddRange(dependentGeometry.ToRhino().Where(x => x is object));
                }
              }
            }
          }

          DA.SetDataList("Geometry", list);
        }
      }
    }
  }

}
