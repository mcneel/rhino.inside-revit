using System;
using Grasshopper.Kernel;

using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeCurtainSystemType : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("83D08B81-B536-4A14-9E2D-F75E9652A824");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "ACST";

    public AnalyzeCurtainSystemType() : base(
      name: "Analyze Curtain System Type",
      nickname: "A-CST",
      description: "Analyze given Curtain System Type",
      category: "Revit",
      subCategory: "Analyze"
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
        param: new Parameters.Element(),
        name: "Curtain Panel Type",
        nickname: "CPT",
        description: "Cutain Panel Type of the Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.CurtianGridJoinCondition_ValueList(),
        name: "Curtain Grid Join Condition",
        nickname: "CGJC",
        description: "Join condition of the Curtain System Type at either direction",
        access: GH_ParamAccess.item
        );

      // layout (vertical)
      manager.AddParameter(
        param: new Parameters.CurtainGridLayout_ValueList(),
        name: "Grid 1 Grid Layout (U Axis)",
        nickname: "UGL",
        description: "Grid 1 Layout (U Axis) configurtion of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Grid 1 Grid Spacing (U Axis)",
        nickname: "UGS",
        description: "Grid 1 Grid (U Axis) spacing of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Grid 1 Grid Adjust for Mullion Size? (U Axis)",
        nickname: "UGAMS?",
        description: "Grid 1 Grid (U Axis) Adjust for Mullion Size configuration of given Curtain System Type",
        access: GH_ParamAccess.item
        );

      // layout (horizontal)
      manager.AddParameter(
        param: new Parameters.CurtainGridLayout_ValueList(),
        name: "Grid 2 Grid Layout (V Axis)",
        nickname: "VGL",
        description: "Grid 2 Layout (V Axis) configurtion of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Grid 2 Grid Spacing (V Axis)",
        nickname: "VGS",
        description: "Grid 2 Grid (V Axis) spacing of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Grid 2 Grid Adjust for Mullion Size? (V Axis)",
        nickname: "VGAMS?",
        description: "Grid 2 Grid (V Axis) Adjust for Mullion Size configuration of given Curtain System Type",
        access: GH_ParamAccess.item
        );

      // mullion types (vertical)
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Grid 1 Interior Mullion Type (U Axis)",
        nickname: "UIMT",
        description: "Grid 1 (U Axis) Interior Mullion Type of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Grid 1 Start Mullion Type (U Axis / Border 1)",
        nickname: "USMT",
        description: "Grid 1 (U Axis) Start Mullion Type (Border 1) of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Grid 1 End Mullion Type (U Axis / Border 2)",
        nickname: "UEMT",
        description: "Grid 1 (U Axis) End Mullion Type (Border 2) of given Curtain System Type",
        access: GH_ParamAccess.item
        );

      // mullion types (horizontal)
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Grid 2 Interior Mullion Type (V Axis)",
        nickname: "VIMT",
        description: "Grid 2 (V Axis) Interior Mullion Type of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Grid 2 Start Mullion Type (V Axis / Border 1)",
        nickname: "VSMT",
        description: "Grid 2 (V Axis) Start Mullion Type (Border 1) of given Curtain System Type",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Grid 2 End Mullion Type (V Axis / Border 2)",
        nickname: "VEMT",
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
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_PANEL, "Curtain Panel Type");
      PipeHostParameter<Types.CurtainGridJoinCondition>(DA, curtainSystemType, DB.BuiltInParameter.AUTO_JOIN_CONDITION, "Curtain Grid Join Condition");

      // layout (vertical)
      PipeHostParameter<Types.CurtainGridLayout>(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LAYOUT_VERT, "Grid 1 Grid Layout (U Axis)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LENGTH_VERT, "Grid 1 Grid Spacing (U Axis)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_VERT, "Grid 1 Grid Adjust for Mullion Size? (U Axis)");

      // layout (horizontal)
      PipeHostParameter<Types.CurtainGridLayout>(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LAYOUT_HORIZ, "Grid 2 Grid Layout (V Axis)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.SPACING_LENGTH_HORIZ, "Grid 2 Grid Spacing (V Axis)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.CURTAINGRID_ADJUST_BORDER_HORIZ, "Grid 2 Grid Adjust for Mullion Size? (V Axis)");

      // mullion types(vertical)
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_GRID1, "Grid 1 Interior Mullion Type (U Axis)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_GRID1, "Grid 1 Start Mullion Type (U Axis / Border 1)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_GRID1, "Grid 1 End Mullion Type (U Axis / Border 2)");

      // mullion types (horizontal)
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_INTERIOR_GRID2, "Grid 2 Interior Mullion Type (V Axis)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER1_GRID2, "Grid 2 Start Mullion Type (V Axis / Border 1)");
      PipeHostParameter(DA, curtainSystemType, DB.BuiltInParameter.AUTO_MULLION_BORDER2_GRID2, "Grid 2 End Mullion Type (V Axis / Border 2)");
    }
  }
}
