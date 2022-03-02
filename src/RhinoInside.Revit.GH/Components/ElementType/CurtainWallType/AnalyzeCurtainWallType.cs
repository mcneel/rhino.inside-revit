using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeCurtainWallType : Component
  {
    public override Guid ComponentGuid => new Guid("D0874F93-0946-42B1-95A9-92C654522BC8");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "ACWT";

    public AnalyzeCurtainWallType() : base
    (
      name: "Analyze Curtain Wall Type",
      nickname: "A-CWT",
      description: "Analyze given Curtain Wall Type",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

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
      ARDB.WallType wallType = default;
      if (!DA.GetData("Curtain Wall Type", ref wallType))
        return;

      if (wallType.Kind == ARDB.WallKind.Curtain)
      {
        // properties of the wall type
        DA.SetData("Function", wallType.get_Parameter(ARDB.BuiltInParameter.FUNCTION_PARAM).AsGoo());
        DA.SetData("Automatically Embed", wallType.get_Parameter(ARDB.BuiltInParameter.ALLOW_AUTO_EMBED).AsGoo());
        DA.SetData("Curtain Panel", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_PANEL_WALL).AsGoo());
        DA.SetData("Join Condition", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_JOIN_CONDITION_WALL).AsGoo());

        // layout (vertical)
        DA.SetData("Vertical Grid : Layout", wallType.get_Parameter(ARDB.BuiltInParameter.SPACING_LAYOUT_VERT).AsGoo());
        DA.SetData("Vertical Grid : Spacing", wallType.get_Parameter(ARDB.BuiltInParameter.SPACING_LENGTH_VERT).AsGoo());
        DA.SetData("Vertical Grid : Adjust for Mullion Size", wallType.get_Parameter(ARDB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_VERT).AsGoo());

        // layout (horizontal)
        DA.SetData("Horizontal Grid : Layout", wallType.get_Parameter(ARDB.BuiltInParameter.SPACING_LAYOUT_HORIZ).AsGoo());
        DA.SetData("Horizontal Grid : Spacing", wallType.get_Parameter(ARDB.BuiltInParameter.SPACING_LENGTH_HORIZ).AsGoo());
        DA.SetData("Horizontal Grid : Adjust for Mullion Size", wallType.get_Parameter(ARDB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_HORIZ).AsGoo());

        // mullion types(vertical)
        DA.SetData("Vertical Mullions : Interior Type", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_MULLION_INTERIOR_VERT).AsGoo());
        DA.SetData("Vertical Mullions : Border 1 Type", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_MULLION_BORDER1_VERT).AsGoo());
        DA.SetData("Vertical Mullions : Border 2 Type", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_MULLION_BORDER2_VERT).AsGoo());

        // mullion types (horizontal)
        DA.SetData("Horizontal Mullions : Interior Type", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_MULLION_INTERIOR_HORIZ).AsGoo());
        DA.SetData("Horizontal Mullions : Border 1 Type", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_MULLION_BORDER1_HORIZ).AsGoo());
        DA.SetData("Horizontal Mullions : Border 2 Type", wallType.get_Parameter(ARDB.BuiltInParameter.AUTO_MULLION_BORDER2_HORIZ).AsGoo());
      }
    }
  }
}
