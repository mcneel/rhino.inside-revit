using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Hosts
{
  using External.DB;
  using External.DB.Extensions;

  public class ElementHost : Component
  {
    public override Guid ComponentGuid => new Guid("6723BEB1-DD99-40BE-8DA9-13B3812D6B46");
    protected override string IconTag => "H";

    public ElementHost() : base
    (
      name: "Element Host",
      nickname: "ElemHost",
      description: "Obtains the host of the specified element",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.GraphicalElement(), "Element", "E", "Element to query for a host", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Host", "H", "Element host object", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Element", out Types.Element element, x => x.IsValid)) return;

      // Special cases
      if (element is Types.IHostObjectAccess access)
      {
        DA.SetData("Host", access.Host);
        return;
      }
      else if (element.Value.get_Parameter(ARDB.BuiltInParameter.HOST_ID_PARAM) is ARDB.Parameter hostId)
      {
        DA.SetData("Host", Types.HostObject.FromElementId(element.Document, hostId.AsElementId()));
        return;
      }

      // Search geometrically
      if (element.Value.get_BoundingBox(null) is ARDB.BoundingBoxXYZ bbox)
      {
        using (var collector = new ARDB.FilteredElementCollector(element.Document))
        {
          var elementCollector = collector.OfClass(typeof(ARDB.HostObject));

          // Element should be at the same Design Option
          if (element.Value.DesignOption is ARDB.DesignOption designOption)
            elementCollector = elementCollector.WherePasses(new ARDB.ElementDesignOptionFilter(designOption.Id));
          else
            elementCollector = elementCollector.WherePasses(new ARDB.ElementDesignOptionFilter(ARDB.ElementId.InvalidElementId));

          if (element.Value.Category?.Parent is ARDB.Category hostCategory)
            elementCollector = elementCollector.OfCategoryId(hostCategory.Id);

          var bboxFilter = new ARDB.BoundingBoxIntersectsFilter(new ARDB.Outline(bbox.Min, bbox.Max));
          elementCollector = elementCollector.WherePasses(bboxFilter);

          using (var includesFilter = CompoundElementFilter.InclusionFilter(element.Value))
          {
            foreach (var host in elementCollector.Cast<ARDB.HostObject>())
            {
              if (host.Id == element.Id)
                continue;

              if (host.FindInserts(false, true, true, false).Contains(element.Id))
              {
                DA.SetData("Host", Types.HostObject.FromElement(host));
                break;
              }
              // Necessary to found Panel walls in a Curtain Wall
              else if (host.GetDependentElements(includesFilter).Count > 0)
              {
                DA.SetData("Host", Types.HostObject.FromElement(host));
                break;
              }
            }
          }
        }
      }
    }
  }
}
