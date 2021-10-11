using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class QueryWalls : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("118F5744-292F-4BEC-9213-8073219D8563");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "W";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Wall));

    public QueryWalls() : base
    (
      name: "Query Walls",
      nickname: "Walls",
      description: "Get all document walls",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
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
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc))
        return;

      // grab wall system family from input
      var wallKind = DB.WallKind.Unknown;
      if (!DA.GetData("Wall System Family", ref wallKind))
        return;

      // collect wall instances based on the given wallkind
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        var wallsCollector = collector.WherePasses(ElementFilter);
        var walls = wallsCollector.Cast<DB.Wall>();

        if (wallKind == DB.WallKind.Basic)
          walls = walls.Where(x => x.WallType.Kind == wallKind && !x.IsStackedWallMember);
        else
          walls = walls.Where(x => x.WallType.Kind == wallKind);

        DA.SetDataList
        (
          "Walls",
          walls.
          Select(Types.Element.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
