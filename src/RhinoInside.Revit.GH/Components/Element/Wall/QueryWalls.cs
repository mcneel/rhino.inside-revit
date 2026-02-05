using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  [ComponentVersion(introduced: "1.0", updated: "1.36")]
  public class QueryWalls : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("118F5744-292F-4BEC-9213-8073219D8563");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => "W";
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementClassFilter(typeof(ARDB.Wall));

    public QueryWalls() : base
    (
      name: "Query Walls",
      nickname: "Walls",
      description: "Get all document walls",
      category: "Revit",
      subCategory: "Architecture"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.ModelInstance(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.Param_Enum<Types.WallSystemFamily>>
      (
        name: "Wall System Family",
        nickname: "WSF",
        description: "Wall system family",
        access: GH_ParamAccess.item
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Wall>
      (
        name: "Walls",
        nickname: "W",
        description: "Walls, of the given wall system family",
        access: GH_ParamAccess.list
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.ModelInstance.TryGetOrCurrent(this, DA, "Model", out Types.IGH_ModelInstance model)) return;

      // grab wall system family from input
      var wallKind = ARDB.WallKind.Unknown;
      if (!DA.GetData("Wall System Family", ref wallKind))
        return;

      // collect wall instances based on the given wallkind
      using (var collector = new ARDB.FilteredElementCollector(model.ModelDocument.Value))
      {
        var wallsCollector = collector.WherePasses(ElementFilter);
        var walls = wallsCollector.Cast<ARDB.Wall>();

        if (wallKind == ARDB.WallKind.Basic)
          walls = walls.Where(x => x.WallType.Kind == wallKind && !x.IsStackedWallMember);
        else
          walls = walls.Where(x => x.WallType.Kind == wallKind);

        DA.SetDataList
        (
          "Walls",
          walls.
          Select(x => new Types.Wall(x)).
          AtModel(model).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
