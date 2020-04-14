using System;
using Grasshopper.Kernel;

using RhinoInside.Revit.External.DB.Extensions;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class WallBoundingGeometry : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("3396DBC4-0E8F-4402-969A-EF5A0E30E093");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "WBG";

    public WallBoundingGeometry() : base(
      name: "Wall Bounding Geometry",
      nickname: "WBG",
      description: "Bounding geometry of given Wall element",
      category: "Revit",
      subCategory: "Element"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Wall",
        nickname: "W",
        description: "Wall element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddBrepParameter(
        name: "Bounding Geometry",
        nickname: "BG",
        description: "Wall bounding geometry",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.Wall wallInstance = default;
      if (!DA.GetData("Wall", ref wallInstance))
        return;

      // extract the bounding geometry of the wall and set on output
      DA.SetData("Bounding Geometry", wallInstance.ComputeWallBoundingGeometry());
    }
  }
}
