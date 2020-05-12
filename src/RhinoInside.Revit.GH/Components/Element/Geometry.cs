using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
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
      manager[manager.AddParameter(new Parameters.Element(), "Ignored", "I", "Elements to ignore while extracting the geometry", GH_ParamAccess.list)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddGeometryParameter("Geometry", "G", "Element geometry", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      bool IsEmpty(Rhino.Geometry.GeometryBase geometry)
      {
        if (geometry is Rhino.Geometry.Brep brep)
          return brep.Surfaces.Count == 0;

        return false;
      }

      var element = default(DB.Element);
      if (!DA.GetData("Element", ref element))
        return;

      var ignored = new List<DB.ElementId>();
      DA.GetDataList("Ignored", ignored);

      var detailLevel = DB.ViewDetailLevel.Undefined;
      DA.GetData(1, ref detailLevel);
      if (detailLevel == DB.ViewDetailLevel.Undefined)
        detailLevel = DB.ViewDetailLevel.Coarse;

      if (element.get_BoundingBox(null) is DB.BoundingBoxXYZ)
      {
        using (var transaction = ignored.Count > 0 ? new DB.Transaction(element.Document, Name) : default)
        {
          if (transaction is object)
          {
            transaction.Start();
            if (element.Document.Delete(ignored).Count > 0)
              element.Document.Regenerate();
          }

          // Extract the geometry
          {
            DB.Options options = null;
            using (var geometry = element?.GetGeometry(detailLevel, out options)) using (options)
            {
              var list = geometry?.
                ToGeometryBaseMany().
                OfType<Rhino.Geometry.GeometryBase>().
                Where(x => !IsEmpty(x)).
                ToList();

              if (list?.Count == 0)
              {
                foreach (var dependent in element.GetDependentElements(null).Select(x => element.Document.GetElement(x)))
                {
                  if (dependent.get_BoundingBox(null) is DB.BoundingBoxXYZ)
                  {
                    DB.Options dependentOptions = null;
                    using (var dependentGeometry = dependent?.GetGeometry(detailLevel, out dependentOptions)) using (dependentOptions)
                    {
                      if (dependentGeometry is object)
                        list.AddRange(dependentGeometry.ToGeometryBaseMany().OfType<Rhino.Geometry.GeometryBase>());
                    }
                  }
                }
              }

              var valid = list.Where(x => !IsEmpty(x));
              DA.SetDataList("Geometry", valid);
            }
          }

          if (transaction is object)
          {
            var failure = transaction.GetFailureHandlingOptions();
            failure.SetClearAfterRollback(true);
            transaction?.RollBack(failure);
          }
        }
      }
    }
  }
}
