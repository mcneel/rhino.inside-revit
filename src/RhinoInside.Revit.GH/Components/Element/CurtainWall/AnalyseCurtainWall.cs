using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyseCurtainWall : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("734B2DAC-1CD2-4D51-B7BD-D3D377CF62DE");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ACW";

    public AnalyseCurtainWall() : base(
      name: "Analyse Curtain Wall",
      nickname: "A-CW",
      description: "Analyse given Curtain Wall element",
      category: "Revit",
      subCategory: "Analyse"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Curtain Wall",
        nickname: "CW",
        description: "Curtain Wall element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.DataObject<DB.CurtainGrid>(),
        name: "Curtain Grid",
        nickname: "CG",
        description: "Curtain Grid definition associated with input Curtain Wall",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Host Wall",
        nickname: "HW",
        description: "Host Wall instance is the input Curtain Wall is embedded",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.Wall wallInstance = default;
      if (!DA.GetData("Curtain Wall", ref wallInstance))
        return;

      // only process curtain walls
      if (wallInstance.WallType.Kind == DB.WallKind.Curtain)
      {
        DA.SetData("Curtain Grid", new Types.DataObject<DB.CurtainGrid>(wallInstance.CurtainGrid));

        // determine if curtain wall is embeded in another wall
        // find all the wall elements that are intersecting the bbox of this wall
        var bbox = wallInstance.get_BoundingBox(null);
        var outline = new DB.Outline(bbox.Min, bbox.Max);
        var bbf = new DB.BoundingBoxIntersectsFilter(outline);
        var walls = new DB.FilteredElementCollector(wallInstance.Document).WherePasses(bbf).OfClass(typeof(DB.Wall)).ToElements();
        // ask for embedded wall inserts from these instances
        foreach (DB.Wall wall in walls)
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
