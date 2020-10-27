using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Element.HostObject
{
  public class HostGrids : Component
  {
    public override Guid ComponentGuid => new Guid("4AD17D89-9044-4438-B468-7F3AB688BA68");
    protected override string IconTag => "G";

    public HostGrids() : base
    (
      name: "Host Curtain Grids",
      nickname: "HostGrids",
      description: "Obtains the curtain grids of the specified host element",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Host", "H", "Host element to query for curtain grids", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Parameters.DataObject<DB.CurtainGrid>(), "Curtain Grids", "CG", "Curtain grids hosted on Host element", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var host = default(Types.HostObject);
      if (!DA.GetData("Host", ref host) || host is null)
        return;

      var curtainGrids = default(IEnumerable<DB.CurtainGrid>);
      switch (host.Value)
      {
        case DB.CurtainSystem curtainSystem: curtainGrids = curtainSystem.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
        case DB.ExtrusionRoof extrusionRoof: curtainGrids = extrusionRoof.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
        case DB.FootPrintRoof footPrintRoof: curtainGrids = footPrintRoof.CurtainGrids?.Cast<DB.CurtainGrid>(); break;
        case DB.Wall wall: curtainGrids = wall.CurtainGrid is null ? null : Enumerable.Repeat(wall.CurtainGrid, 1); break;
      }

      if(curtainGrids is object)
        DA.SetDataList("Curtain Grids", curtainGrids.Select(x => new Types.DataObject<DB.CurtainGrid>(x, host.Document)));
    }
  }
}
