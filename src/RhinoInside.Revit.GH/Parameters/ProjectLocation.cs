using System;
using System.Drawing;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Parameters
{
  public class ProjectLocation : GraphicalElementT<Types.ProjectLocation, ARDB.ProjectLocation>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("1A2A68F4-7F16-4EF6-829D-83531A5C043E");
    protected override string IconTag => "📍";

    public ProjectLocation() : base
    (
      name: "Shared Site",
      nickname: "Shared Site",
      description: "Contains a collection of Revit shared site elements",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }

  public class SiteLocation : ElementType<Types.SiteLocation, ARDB.SiteLocation>
  {
    public override GH_Exposure Exposure => GH_Exposure.senary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("30DADC5F-CCD3-4E37-9BA7-4CF3612D88C5");
    protected override string IconTag => "📍";

    public SiteLocation() : base
    (
      name: "Site Location",
      nickname: "Site Location",
      description: "Contains a collection of Revit site location elements",
      category: "Params",
      subcategory: "Revit"
    )
    { }
  }
}
