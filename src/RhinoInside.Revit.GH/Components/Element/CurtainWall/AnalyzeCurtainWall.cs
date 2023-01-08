using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using External.DB.Extensions;

  public class AnalyzeCurtainWall : Component
  {
    public override Guid ComponentGuid => new Guid("734B2DAC-1CD2-4D51-B7BD-D3D377CF62DE");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "ACW";

    public AnalyzeCurtainWall() : base
    (
      name: "Analyze Curtain Wall",
      nickname: "A-CW",
      description: "Analyze given Curtain Wall element",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.Wall(),
        name: "Curtain Wall",
        nickname: "CW",
        description: "Curtain Wall element",
        access: GH_ParamAccess.item
      );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter
      (
        param: new Parameters.CurtainGrid(),
        name: "Curtain Grid",
        nickname: "CG",
        description: "Curtain Grid definition associated with input Curtain Wall",
        access: GH_ParamAccess.item
      );

      manager.AddParameter
      (
        param: new Parameters.Wall(),
        name: "Host Wall",
        nickname: "HW",
        description: "Host Wall instance is the input Curtain Wall is embedded",
        access: GH_ParamAccess.item
      );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Curtain Wall", out Types.Wall wall, x => x.IsValid)) return;

      // only process curtain walls
      if (wall.Value.WallType.Kind == ARDB.WallKind.Curtain)
      {
        DA.SetData("Curtain Grid", wall.CurtainGrids?.FirstOrDefault());

        // determine if curtain wall is embeded in another wall
        // find all the wall elements that are intersecting the bbox of this wall
        using (var collector = new ARDB.FilteredElementCollector(wall.Document))
        {
          var embededWalls = collector.OfClass(typeof(ARDB.Wall)).
            WherePasses(new ARDB.BoundingBoxIntersectsFilter(wall.Value.GetOutline()));

          // ask for embedded wall inserts from these instances
          foreach (ARDB.Wall embededWall in embededWalls)
          {
            var embeddedWalls = embededWall.FindInserts(addRectOpenings: false, includeShadows: false, includeEmbeddedWalls: true, includeSharedEmbeddedInserts: false);
            if (embeddedWalls.Contains(embededWall.Id))
            {
              DA.SetData("Host Wall", Types.Wall.FromElement(embededWall));
              break;
            }
          }
        }
      }
    }
  }
}
