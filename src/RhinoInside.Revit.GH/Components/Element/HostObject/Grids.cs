using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class HostObjectGrids : Component
  {
    public override Guid ComponentGuid => new Guid("4AD17D89-9044-4438-B468-7F3AB688BA68");
    protected override string IconTag => "#";

    public HostObjectGrids() : base
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
      manager.AddParameter(new Parameters.CurtainGrid(), "Curtain Grids", "CG", "Curtain grids hosted on Host element", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var host = default(Types.HostObject);
      if (!DA.GetData("Host", ref host) || host is null)
        return;

      DA.SetDataList("Curtain Grids", host.CurtainGrids);
    }
  }
}
