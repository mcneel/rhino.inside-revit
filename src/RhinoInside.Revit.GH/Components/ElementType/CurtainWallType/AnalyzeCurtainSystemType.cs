using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeCurtainSystemType : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("83D08B81-B536-4A14-9E2D-F75E9652A824");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "ACST";

    public AnalyzeCurtainSystemType() : base(
      name: "Analyze Curtain System Type",
      nickname: "A-CST",
      description: "Analyze given Curtain System Type",
      category: "Revit",
      subCategory: "Wall"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Curtain System Type",
        nickname: "CST",
        description: "Curtain System Type to be analyzed",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      // properties of the system type
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Curtain Panel",
        nickname: "CP",
        description: "Cutain Panel Type of the Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainGridJoinCondition>(),
        name: "Join Condition",
        nickname: "JC",
        description: "Join condition of the Curtain System Type at either direction",
        access: GH_ParamAccess.item
        );

      // layout (vertical)
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainGridLayout>(),
        name: "Grid 1 : Layout",
        nickname: "G1L",
        description: "Grid 1 Layout (U Axis) configurtion of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Grid 1 : Spacing",
        nickname: "G1S",
        description: "Grid 1 Grid (U Axis) spacing of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Grid 1 : Adjust for Mullion Size",
        nickname: "G1AMS",
        description: "Grid 1 Grid (U Axis) Adjust for Mullion Size configuration of given Curtain System Type",
        access: GH_ParamAccess.item
        );

      // layout (horizontal)
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainGridLayout>(),
        name: "Grid 2 : Layout",
        nickname: "G2L",
        description: "Grid 2 Layout (V Axis) configurtion of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Grid 2 : Spacing",
        nickname: "G2S",
        description: "Grid 2 Grid (V Axis) spacing of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Grid 2 : Adjust for Mullion Size",
        nickname: "G2AMS",
        description: "Grid 2 Grid (V Axis) Adjust for Mullion Size configuration of given Curtain System Type",
        access: GH_ParamAccess.item
        );

      // mullion types (vertical)
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Grid 1 Mullions : Interior Type",
        nickname: "G1MIT",
        description: "Grid 1 (U Axis) Interior Mullion Type of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Grid 1 Mullions : Border 1 Type",
        nickname: "G1MB1T",
        description: "Grid 1 (U Axis) Start Mullion Type (Border 1) of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Grid 1 Mullions : Border 2 Type",
        nickname: "G1MB2T",
        description: "Grid 1 (U Axis) End Mullion Type (Border 2) of given Curtain System Type",
        access: GH_ParamAccess.item
        );

      // mullion types (horizontal)
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Grid 2 Mullions : Interior Type",
        nickname: "G2MIT",
        description: "Grid 2 (V Axis) Interior Mullion Type of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Grid 2 Mullions : Border 1 Type",
        nickname: "G2MB1T",
        description: "Grid 2 (V Axis) Start Mullion Type (Border 1) of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Grid 2 Mullions : Border 2 Type",
        nickname: "G2MB2T",
        description: "Grid 2 (V Axis) End Mullion Type (Border 2) of given Curtain System Type",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input system type
      DB.CurtainSystemType curtainSystemType = default;
      if (!DA.GetData("Curtain System Type", ref curtainSystemType))
        return;

      // properties of the system type
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_PANEL, "Curtain Panel");
      PipeHostParameter<Types.CurtainGridJoinCondition>(DA, curtainSystemType, DB.BuiltInParameter.AUTO_JOIN_CONDITION, "Join Condition");

      // layout (vertical)
      PipeHostParameter<Types.CurtainGridLayout>(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LAYOUT_VERT, "Grid 1 : Layout");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LENGTH_VERT, "Grid 1 : Spacing");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_VERT, "Grid 1 : Adjust for Mullion Size");

      // layout (horizontal)
      PipeHostParameter<Types.CurtainGridLayout>(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LAYOUT_HORIZ, "Grid 2 : Layout");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LENGTH_HORIZ, "Grid 2 : Spacing");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_HORIZ, "Grid 2 : Adjust for Mullion Size");

      // mullion types(vertical)
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_GRID1, "Grid 1 Mullions : Interior Type");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_GRID1, "Grid 1 Mullions : Border 1 Type");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_GRID1, "Grid 1 Mullions : Border 2 Type");

      // mullion types (horizontal)
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_GRID2, "Grid 2 Mullions : Interior Type");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_GRID2, "Grid 2 Mullions : Border 1 Type");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_GRID2, "Grid 2 Mullions : Border 2 Type");
    }
  }
}
