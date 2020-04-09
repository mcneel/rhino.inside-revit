using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyseBasicWallType : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("00A650ED-4CC7-4AD3-BF38-491507315AC5");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ABWT";

    public AnalyseBasicWallType() : base(
      name: "Analyse Base Wall Type",
      nickname: "A-BWT",
      description: "Analyse given Base Wall type",
      category: "Revit",
      subCategory: "Analyse"
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
        param: new Parameters.DataObject<DB.CompoundStructure>(),
        name: "Compound Structure",
        nickname: "CS",
        description: "Compound Structure definition of given Basic Wall type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.WallWrapping_ValueList(),
        name: "Wrapping at Insert",
        nickname: "WI",
        description: "Wrapping at Insert setting of given Basic Wall type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.WallWrapping_ValueList(),
        name: "Wrapping at End",
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
        param: new Parameters.WallFunction_ValueList(),
        name: "Wall Function",
        nickname: "WF",
        description: "Wall Function of given Basic Wall type",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.WallType wallType = default;
      if (!DA.GetData("Basic Wall Type", ref wallType))
        return;

      // make sure input wall type is a DB.WallKind.Basic
      if(wallType.Kind != DB.WallKind.Basic)
        return;

      // grab compound structure
      DA.SetData(
        "Compound Structure",
        new Types.DataObject<DB.CompoundStructure>(
          apiObject: wallType.GetCompoundStructure(),
          srcDocument: wallType.Document
          )
        );
      // pipe the wall type parameters directly to component outputs
      PipeHostParameter<Types.WallWrapping>(DA, wallType, DB.BuiltInParameter.WRAPPING_AT_INSERTS_PARAM, "Wrapping at Insert");
      PipeHostParameter<Types.WallWrapping>(DA, wallType, DB.BuiltInParameter.WRAPPING_AT_ENDS_PARAM, "Wrapping at End");
      PipeHostParameter(DA, wallType, DB.BuiltInParameter.WALL_ATTR_WIDTH_PARAM, "Width");
      PipeHostParameter<Types.WallFunction>(DA, wallType, DB.BuiltInParameter.FUNCTION_PARAM, "Wall Function");
    }
  }
}
