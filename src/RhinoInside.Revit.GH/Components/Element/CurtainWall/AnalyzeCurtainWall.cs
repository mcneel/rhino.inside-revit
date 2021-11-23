using System;
using Grasshopper.Kernel;

using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeCurtainWall : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("734B2DAC-1CD2-4D51-B7BD-D3D377CF62DE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "ACW";

    public AnalyzeCurtainWall() : base(
      name: "Analyze Curtain Wall",
      nickname: "A-CW",
      description: "Analyze given Curtain Wall element",
      category: "Revit",
      subCategory: "Wall"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Wall(),
        name: "Curtain Wall",
        nickname: "CW",
        description: "Curtain Wall element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.CurtainGrid(),
        name: "Curtain Grid",
        nickname: "CG",
        description: "Curtain Grid definition associated with input Curtain Wall",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Wall(),
        name: "Host Wall",
        nickname: "HW",
        description: "Host Wall instance is the input Curtain Wall is embedded",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input
      ARDB.Wall wallInstance = default;
      if (!DA.GetData("Curtain Wall", ref wallInstance))
        return;

      // only process curtain walls
      if (wallInstance.WallType.Kind == ARDB.WallKind.Curtain)
      {
        DA.SetData("Curtain Grid", new Types.CurtainGrid(wallInstance, wallInstance.CurtainGrid));

        // determine if curtain wall is embeded in another wall
        // find all the wall elements that are intersecting the bbox of this wall
        var bbox = wallInstance.get_BoundingBox(null);
        var outline = new ARDB.Outline(bbox.Min, bbox.Max);
        var bbf = new ARDB.BoundingBoxIntersectsFilter(outline);
        var walls = new ARDB.FilteredElementCollector(wallInstance.Document).WherePasses(bbf).OfClass(typeof(ARDB.Wall)).ToElements();
        // ask for embedded wall inserts from these instances
        foreach (ARDB.Wall wall in walls)
        {
          var embeddedWalls = wall.FindInserts(addRectOpenings: false, includeShadows: false, includeEmbeddedWalls: true, includeSharedEmbeddedInserts: false);
          if (embeddedWalls.Contains(wallInstance.Id))
          {
            DA.SetData("Host Wall", Types.Element.FromElement(wall));
            break;
          }
        }
      }
    }
  }
}
