using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
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
      DB.Element element = null;
      if (!DA.GetData("Element", ref element) || element is null)
        return;

      // Special cases
      if (element is DB.Panel || element is DB.Mullion)
      {
        var hostId = element.get_Parameter(DB.BuiltInParameter.CURTAIN_WALL_PANEL_HOST_ID).AsElementId();
        DA.SetData("Host", Types.HostObject.FromElementId(element.Document, hostId));
        return;
      }
      else if (element is DB.Opening opening)
      {
        DA.SetData("Host", Types.HostObject.FromElement(opening.Host));
        return;
      }
      // Door & Windows
      else if (element.get_Parameter(DB.BuiltInParameter.HOST_ID_PARAM) is DB.Parameter hostId)
      {
        DA.SetData("Host", Types.HostObject.FromElementId(element.Document, hostId.AsElementId()));
        return;
      }

      // Search geometrically
      if (element.get_BoundingBox(null) is DB.BoundingBoxXYZ bbox)
      {
        using (var collector = new DB.FilteredElementCollector(element.Document))
        {
          var elementCollector = collector.OfClass(typeof(DB.HostObject));
          var bboxFilter = new DB.BoundingBoxIntersectsFilter(new DB.Outline(bbox.Min, bbox.Max));
          elementCollector = elementCollector.WherePasses(bboxFilter);

          var classFilter = default(DB.ElementFilter);
          if (element is DB.FamilyInstance instance) classFilter = new DB.FamilyInstanceFilter(element.Document, instance.GetTypeId());
          else if (element is DB.Area) classFilter = new DB.AreaFilter();
          else if (element is DB.AreaTag) classFilter = new DB.AreaTagFilter();
          else if (element is DB.Architecture.Room) classFilter = new DB.Architecture.RoomFilter();
          else if (element is DB.Architecture.RoomTag) classFilter = new DB.Architecture.RoomTagFilter();
          else
          {
            if (element is DB.CurveElement)
              classFilter = new DB.ElementClassFilter(typeof(DB.CurveElement));
            else
              classFilter = new DB.ElementClassFilter(element.GetType());
          }

          foreach (var host in elementCollector.ToElements().OfType<DB.HostObject>())
          {
            if (host.Id == element.Id)
              continue;

            if(host.FindInserts(false, true, true, false).Contains(element.Id))
            {
              DA.SetData("Host", Types.HostObject.FromElement(host));
              break;
            }
            // Necessary to found Panel walls ina Curtain Wall
            else if (host.GetDependentElements(classFilter).Contains(element.Id))
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
