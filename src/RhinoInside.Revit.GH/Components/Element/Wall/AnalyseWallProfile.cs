using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  // TODO: improve AnalyseWallProfile to work on curtain walls
  // TODO: improve AnalyseWallProfile to work on curved walls
  // TODO: improve AnalyseWallProfile to return profile curves at WallLocationLine
  public class AnalyseWallProfile : Component
  {
    public override Guid ComponentGuid => new Guid("9D2E9D8D-E794-4202-B725-82E78317892F");

    public AnalyseWallProfile() : base(
      name:"Analyse Wall Profile",
      nickname: "A-WP",
      description: "Get the vertical profile of the given wall",
      category: "Revit",
      subCategory: "Analyse"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
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

    private List<DB.ElementId> ExtractDependentElemnets(DB.Element element, DB.ElementFilter filter)
    {
#if (REVIT_2019)
      // if Revit 2019 and above
      return element.GetDependentElements(filter).ToList();
#else
      // otherwise
      var dependentElements = new List<DB.ElementId>();
      try
      {
        // start a dry transaction that will be rolled back later
        var t = new DB.Transaction(element.Document, "Dry Transaction");
        t.Start();
        dependentElements = element.Document.Delete(element.Id)?.ToList();
        // rollback the changes now
        t.RollBack();
      }
      catch { }
      return dependentElements.Where(x => filter.PassesFilter(element.Document, x)).ToList();
#endif
    }

    private List<Rhino.Geometry.Curve> ExtractDependentCurves(DB.Element element)
    {
      return ExtractDependentElemnets(element, new DB.ElementClassFilter(typeof(DB.CurveElement)))
             .Select(x => element.Document.GetElement(x))
             .Cast<DB.CurveElement>()
             .Select(x => x.GeometryCurve.ToRhino())
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
