using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Host
{
  public class HostObjectBoundaryProfile : Component
  {
    public override Guid ComponentGuid => new Guid("7CE0BD56-A2AC-4D49-A39B-7B34FE897265");
    protected override string IconTag => "B";

    public HostObjectBoundaryProfile() : base
    (
      name: "Host Boundary Profile",
      nickname: "BoundProf",
      description: "Get the boundary profile of the given host element",
      category: "Revit",
      subCategory: "Host"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.HostObject(), "Host", "H", "Host object to query for its boundary profile", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddPlaneParameter("Plane", "P", "Sketch plane", GH_ParamAccess.item);
      manager.AddCurveParameter("Profile", "PC", "Sketch profile curves", GH_ParamAccess.list);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.HostObject host = null;
      if (!DA.GetData("Host", ref host) || host is null)
        return;

      if(host.GetSketch() is DB.Sketch sketch)
      {
        DA.SetData("Plane", sketch.SketchPlane.GetPlane().ToPlane());
        DA.SetDataList("Profile", sketch.Profile.ToPolyCurves());
      }
    }
  }
}
