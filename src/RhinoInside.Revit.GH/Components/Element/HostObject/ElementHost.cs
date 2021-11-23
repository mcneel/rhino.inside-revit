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
      ARDB.Element element = null;
      if (!DA.GetData("Element", ref element) || element is null)
        return;

      // Special cases
      if (element is ARDB.FamilyInstance familyInstace)
      {
        DA.SetData("Host", Types.HostObject.FromElement(familyInstace.Host));
        return;
      }
      else if (element is ARDB.Opening opening)
      {
        DA.SetData("Host", Types.HostObject.FromElement(opening.Host));
        return;
      }
      else if (element is ARDB.Sketch sketch)
      {
        DA.SetData("Host", Types.HostObject.FromElement(sketch.GetHostObject()));
        return;
      }
      else if (element.get_Parameter(ARDB.BuiltInParameter.HOST_ID_PARAM) is ARDB.Parameter hostId)
      {
        DA.SetData("Host", Types.HostObject.FromElementId(element.Document, hostId.AsElementId()));
        return;
      }

      // Search geometrically
      if (element.get_BoundingBox(null) is ARDB.BoundingBoxXYZ bbox)
      {
        using (var collector = new ARDB.FilteredElementCollector(element.Document))
        {
          var elementCollector = collector.OfClass(typeof(ARDB.HostObject));

          // Element should be at the same Design Option
          if (element.DesignOption is ARDB.DesignOption designOption)
            elementCollector = elementCollector.WherePasses(new ARDB.ElementDesignOptionFilter(designOption.Id));
          else
            elementCollector = elementCollector.WherePasses(new ARDB.ElementDesignOptionFilter(ARDB.ElementId.InvalidElementId));

          if (element.Category?.Parent is ARDB.Category hostCategory)
            elementCollector = elementCollector.OfCategoryId(hostCategory.Id);

          var bboxFilter = new ARDB.BoundingBoxIntersectsFilter(new ARDB.Outline(bbox.Min, bbox.Max));
          elementCollector = elementCollector.WherePasses(bboxFilter);

          var classFilter = default(ARDB.ElementFilter);
          if (element is ARDB.FamilyInstance instance) classFilter = new ARDB.FamilyInstanceFilter(element.Document, instance.GetTypeId());
          else if (element is ARDB.Area) classFilter = new ARDB.AreaFilter();
          else if (element is ARDB.AreaTag) classFilter = new ARDB.AreaTagFilter();
          else if (element is ARDB.Architecture.Room) classFilter = new ARDB.Architecture.RoomFilter();
          else if (element is ARDB.Architecture.RoomTag) classFilter = new ARDB.Architecture.RoomTagFilter();
          else if (element is ARDB.Mechanical.Space) classFilter = new ARDB.Mechanical.SpaceFilter();
          else if (element is ARDB.Mechanical.SpaceTag) classFilter = new ARDB.Mechanical.SpaceTagFilter();
          else
          {
            if (element is ARDB.CurveElement)
              classFilter = new ARDB.ElementClassFilter(typeof(ARDB.CurveElement));
            else
              classFilter = new ARDB.ElementClassFilter(element.GetType());
          }

          foreach (var host in elementCollector.Cast<ARDB.HostObject>())
          {
            if (host.Id == element.Id)
              continue;

            if(host.FindInserts(false, true, true, false).Contains(element.Id))
            {
              DA.SetData("Host", Types.HostObject.FromElement(host));
              break;
            }
            // Necessary to found Panel walls in a Curtain Wall
            else if (host.GetDependentElements(classFilter.ThatIncludes(element.Id)).Count > 0)
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
