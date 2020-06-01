using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.External.DB.Extensions;

namespace RhinoInside.Revit.GH.Components
{
  // TODO: improve AnalyzeWallProfile to work on curtain walls
  // TODO: improve AnalyzeWallProfile to work on curved walls
  // TODO: improve AnalyzeWallProfile to return profile curves at WallLocationLine
  public class AnalyzeWallProfile : Component
  {
    public override Guid ComponentGuid => new Guid("9D2E9D8D-E794-4202-B725-82E78317892F");
    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public AnalyzeWallProfile() : base(
      name:"Analyze Wall Profile",
      nickname: "A-WP",
      description: "Get the vertical profile of the given wall",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Wall(),
        name: "Wall",
        nickname: "W",
        description: "Wall element to extract the profile",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddCurveParameter(
        name: "Profile Curves",
        nickname: "PC",
        description: "Profile curves of given wall element",
        access: GH_ParamAccess.list
        );
    }

    private List<Rhino.Geometry.Curve> ExtractDependentCurves(DB.Element element)
    {
      return element.GetDependentElements(new DB.ElementClassFilter(typeof(DB.CurveElement)))
             .Select(x => element.Document.GetElement(x))
             .Cast<DB.CurveElement>()
             .Select(x => x.GeometryCurve.ToCurve())
             .ToList();
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      DB.Wall wall = null;
      if (!DA.GetData("Wall", ref wall))
        return;

      if (wall.WallType.Kind != DB.WallKind.Curtain)
        DA.SetDataList("Profile Curves", ExtractDependentCurves(wall));
    }
  }
}
