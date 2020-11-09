using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeCurtainWallType : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("D0874F93-0946-42B1-95A9-92C654522BC8");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "ACWT";

    public AnalyzeCurtainWallType() : base(
      name: "Analyze Curtain Wall Type",
      nickname: "A-CWT",
      description: "Analyze given Curtain Wall Type",
      category: "Revit",
      subCategory: "Wall"
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
        param: new Parameters.Param_Enum<Types.WallFunction>(),
        name: "Function",
        nickname: "F",
        description: "Wall Function of given Curtain Wall type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Automatically Embed",
        nickname: "AE",
        description: "Whether given Curtain Wall type is configured to automatically embed",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Curtain Panel",
        nickname: "CP",
        description: "Cutain Panel Type of the Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainGridJoinCondition>(),
        name: "Join Condition",
        nickname: "JC",
        description: "Join condition of the Curtain Wall Type at either direction",
        access: GH_ParamAccess.item
        );

      // layout (vertical)
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainGridLayout>(),
        name: "Vertical Grid : Layout",
        nickname: "VGL",
        description: "Vertical Layout (U Axis) configurtion of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Vertical Grid : Spacing",
        nickname: "VGS",
        description: "Vertical Grid (U Axis) spacing of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Vertical Grid : Adjust for Mullion Size",
        nickname: "VGAMS",
        description: "Vertical Grid (U Axis) Adjust for Mullion Size configuration of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );

      // layout (horizontal)
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainGridLayout>(),
        name: "Horizontal Grid : Layout",
        nickname: "HGL",
        description: "Horizontal Layout (V Axis) configurtion of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Horizontal Grid : Spacing",
        nickname: "HGS",
        description: "Horizontal Grid (V Axis) spacing of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Horizontal Grid : Adjust for Mullion Size",
        nickname: "HGAMS",
        description: "Horizontal Grid (V Axis) Adjust for Mullion Size configuration of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );

      // mullion types (vertical)
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Vertical Mullions : Interior Type",
        nickname: "VIMT",
        description: "Vertical (U Axis) Interior Mullion Type of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Vertical Mullions : Border 1 Type",
        nickname: "VMB1T",
        description: "Vertical (U Axis) Start Mullion Type (Border 1) of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Vertical Mullions : Border 2 Type",
        nickname: "VMB2T",
        description: "Vertical (U Axis) End Mullion Type (Border 2) of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );

      // mullion types (horizontal)
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Horizontal Mullions : Interior Type",
        nickname: "HMIT",
        description: "Horizontal (V Axis) Interior Mullion Type of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Horizontal Mullions : Border 1 Type",
        nickname: "HMB1T",
        description: "Horizontal (V Axis) Bottom Mullion Type (Border 1) of given Curtain Wall Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Horizontal Mullions : Border 2 Type",
        nickname: "HMB2T",
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
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.FUNCTION_PARAM, "Function");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.ALLOW_AUTO_EMBED, "Automatically Embed");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_PANEL_WALL, "Curtain Panel");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_JOIN_CONDITION_WALL, "Join Condition");

        // layout (vertical)
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.SPACING_LAYOUT_VERT, "Vertical Grid : Layout");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.SPACING_LENGTH_VERT, "Vertical Grid : Spacing");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_VERT, "Vertical Grid : Adjust for Mullion Size");

        // layout (horizontal)
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.SPACING_LAYOUT_HORIZ, "Horizontal Grid : Layout");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.SPACING_LENGTH_HORIZ, "Horizontal Grid : Spacing");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_HORIZ, "Horizontal Grid : Adjust for Mullion Size");

        // mullion types(vertical)
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_VERT, "Vertical Mullions : Interior Type");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_VERT, "Vertical Mullions : Border 1 Type");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_VERT, "Vertical Mullions : Border 2 Type");

        // mullion types (horizontal)
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_HORIZ, "Horizontal Mullions : Interior Type");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_HORIZ, "Horizontal Mullions : Border 1 Type");
        PipeHostParameter(DA, wallType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_HORIZ, "Horizontal Mullions : Border 2 Type");
      }
    }
  }
}
