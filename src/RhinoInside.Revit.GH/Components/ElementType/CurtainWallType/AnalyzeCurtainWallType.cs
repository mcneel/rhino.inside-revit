using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeCurtainWallType : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("D0874F93-0946-42B1-95A9-92C654522BC8");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ACWT";

    public AnalyzeCurtainWallType() : base(
      name: "Analyze Curtain Wall Type",
      nickname: "A-CWT",
      description: "Analyze given Curtain Wall Type",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Curtain Wall Type",
        nickname: "CWT",
        description: "Curtain Wall Type to be analyzed",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      // properties of the wall type
      manager.AddParameter(
        param: new Parameters.WallFunction_ValueList(),
        name: "Wall Function",
        nickname: "WF",
        description: "Wall Function of given Curtain Wall type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Automatically Embed?",
        nickname: "AE?",
        description: "Whether given Curtain Wall type is configured to automatically embed",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Curtain Panel Type",
        nickname: "CPT",
        description: "Cutain Panel Type of the Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.CurtianGridJoinCondition_ValueList(),
        name: "Curtain Grid Join Condition",
        nickname: "CGJC",
        description: "Join condition of the Curtain Wall Type at either direction",
        access: GH_ParamAccess.item
        );

      // layout (vertical)
      manager.AddParameter(
        param: new Parameters.CurtainGridLayout_ValueList(),
        name: "Vertical Grid Layout (U Axis)",
        nickname: "UGL",
        description: "Vertical Layout (U Axis) configurtion of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Vertical Grid Spacing (U Axis)",
        nickname: "UGS",
        description: "Vertical Grid (U Axis) spacing of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Vertical Grid Adjust for Mullion Size? (U Axis)",
        nickname: "UGAMS?",
        description: "Vertical Grid (U Axis) Adjust for Mullion Size configuration of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );

      // layout (horizontal)
      manager.AddParameter(
        param: new Parameters.CurtainGridLayout_ValueList(),
        name: "Horizontal Grid Layout (V Axis)",
        nickname: "VGL",
        description: "Horizontal Layout (V Axis) configurtion of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Horizontal Grid Spacing (V Axis)",
        nickname: "VGS",
        description: "Horizontal Grid (V Axis) spacing of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Horizontal Grid Adjust for Mullion Size? (V Axis)",
        nickname: "VGAMS?",
        description: "Horizontal Grid (V Axis) Adjust for Mullion Size configuration of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );

      // mullion types (vertical)
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Vertical Interior Mullion Type (U Axis)",
        nickname: "UIMT",
        description: "Vertical (U Axis) Interior Mullion Type of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Vertical Start Mullion Type (U Axis / Border 1)",
        nickname: "USMT",
        description: "Vertical (U Axis) Start Mullion Type (Border 1) of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Vertical End Mullion Type (U Axis / Border 2)",
        nickname: "UEMT",
        description: "Vertical (U Axis) End Mullion Type (Border 2) of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );

      // mullion types (horizontal)
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Horizontal Interior Mullion Type (V Axis)",
        nickname: "VIMT",
        description: "Horizontal (V Axis) Interior Mullion Type of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Horizontal Bottom Mullion Type (V Axis / Border 1)",
        nickname: "VBMT",
        description: "Horizontal (V Axis) Bottom Mullion Type (Border 1) of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Horizontal Top Mullion Type (V Axis / Border 2)",
        nickname: "VTMT",
        description: "Horizontal (V Axis) Top Mullion Type (Border 2) of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.WallType wallType = default;
      if (!DA.GetData("Curtain Wall Type", ref wallType))
        return;

      if (wallType.Kind == DB.WallKind.Curtain)
      {
        // properties of the wall type
        PipeHostParameter<Types.WallFunction>(DA, wallType, DB.BuiltInParameter.FUNCTION_PARAM, "Wall Function");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.ALLOW_AUTO_EMBED, "Automatically Embed?");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_PANEL_WALL, "Curtain Panel Type");
        PipeHostParameter<Types.CurtainGridJoinCondition>(DA, wallType, DB.BuiltInParameter.AUTO_JOIN_CONDITION_WALL, "Curtain Grid Join Condition");

        // layout (vertical)
        PipeHostParameter<Types.CurtainGridLayout>(DA, wallType, DB.BuiltInParameter.SPACING_LAYOUT_VERT, "Vertical Grid Layout (U Axis)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.SPACING_LENGTH_VERT, "Vertical Grid Spacing (U Axis)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_VERT, "Vertical Grid Adjust for Mullion Size? (U Axis)");

        // layout (horizontal)
        PipeHostParameter<Types.CurtainGridLayout>(DA, wallType, DB.BuiltInParameter.SPACING_LAYOUT_HORIZ, "Horizontal Grid Layout (V Axis)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.SPACING_LENGTH_HORIZ, "Horizontal Grid Spacing (V Axis)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_HORIZ, "Horizontal Grid Adjust for Mullion Size? (V Axis)");

        // mullion types(vertical)
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_VERT, "Vertical Interior Mullion Type (U Axis)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_VERT, "Vertical Start Mullion Type (U Axis / Border 1)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_VERT, "Vertical End Mullion Type (U Axis / Border 2)");

        // mullion types (horizontal)
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_HORIZ, "Horizontal Interior Mullion Type (V Axis)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_HORIZ, "Horizontal Bottom Mullion Type (V Axis / Border 1)");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_HORIZ, "Horizontal Top Mullion Type (V Axis / Border 2)");
      }
    }
  }
}
