using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class DocumentWalls : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("118F5744-292F-4BEC-9213-8073219D8563");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "W";
    protected override DB.ElementFilter ElementFilter => new DB.ElementClassFilter(typeof(DB.Wall));

    public DocumentWalls() : base
    (
      name: "Walls",
      nickname: "Walls",
      description: "Get all document walls",
      category: "Revit",
      subCategory: "Query"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.FromParam(DocumentComponent.CreateDocumentParam(), ParamVisibility.Voluntary),
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
      ParamDefinition.Create<Parameters.HostObject>
      (
        name: "Walls",
        nickname: "W",
        description: "Walls, of the given wall system family",
        access: GH_ParamAccess.list
      )
    };

    protected override void TrySolveInstance(IGH_DataAccess DA, DB.Document doc)
    {
      // grab wall system family from input
      var wallKind = DB.WallKind.Unknown;
      if (!DA.GetData("Wall System Family", ref wallKind))
        return;

      // collect wall instances based on the given wallkind
      using (var collector = new DB.FilteredElementCollector(doc))
      {
        IEnumerable<Types.Element> walls;

        if (wallKind == DB.WallKind.Basic)
        {
          walls = collector.OfClass(typeof(DB.Wall))
                                   .Cast<DB.Wall>()
                                   .Where(x => x.WallType.Kind == wallKind && !x.IsStackedWallMember)
                                   .Select(x => Types.Element.FromElement(x));
        }
        else
        {
          walls = collector.OfClass(typeof(DB.Wall))
                                   .Where(x => ((DB.Wall) x).WallType.Kind == wallKind)
                                   .Select(x => Types.Element.FromElement(x));
        }

        DA.SetDataList("Walls", walls);
      }
    }
  }
}
