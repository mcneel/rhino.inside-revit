using System;
using Grasshopper.Kernel;

using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeBasicWallType : Component
  {
    public override Guid ComponentGuid => new Guid("00A650ED-4CC7-4AD3-BF38-491507315AC5");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "ABWT";

    public AnalyzeBasicWallType() : base
    (
      name: "Analyze Basic Wall Type",
      nickname: "A-BWT",
      description: "Analyze given Basic Wall type",
      category: "Revit",
      subCategory: "Wall"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Basic Wall Type",
        nickname: "BWT",
        description: "Basic Wall type",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.CompoundStructure(),
        name: "Structure",
        nickname: "S",
        description: "Compound Structure definition of given Basic Wall type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.WallWrapping>(),
        name: "Wrapping at Inserts",
        nickname: "WI",
        description: "Wrapping at Insert setting of given Basic Wall type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.WallWrapping>(),
        name: "Wrapping at Ends",
        nickname: "WE",
        description: "Wrapping at End setting of given Basic Wall type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Width",
        nickname: "W",
        description: "Total width of the given Basic Wall type structure",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.WallFunction>(),
        name: "Function",
        nickname: "F",
        description: "Wall Function of given Basic Wall type",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      ARDB.WallType wallType = default;
      if (!DA.GetData("Basic Wall Type", ref wallType))
        return;

      // make sure input wall type is a DB.WallKind.Basic
      if(wallType.Kind != ARDB.WallKind.Basic)
        return;

      // grab compound structure
      DA.SetData("Structure", new Types.CompoundStructure(wallType.Document, wallType.GetCompoundStructure()));

      // pipe the wall type parameters directly to component outputs
      DA.SetData("Wrapping at Inserts", wallType.get_Parameter(ARDB.BuiltInParameter.WRAPPING_AT_INSERTS_PARAM).AsGoo());
      DA.SetData("Wrapping at Ends", wallType.get_Parameter(ARDB.BuiltInParameter.WRAPPING_AT_ENDS_PARAM).AsGoo());
      DA.SetData("Width", wallType.get_Parameter(ARDB.BuiltInParameter.WALL_ATTR_WIDTH_PARAM).AsGoo());
      DA.SetData("Function", wallType.get_Parameter(ARDB.BuiltInParameter.FUNCTION_PARAM).AsGoo());
    }
  }
}
