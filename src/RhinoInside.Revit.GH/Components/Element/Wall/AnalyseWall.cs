using System;
using Grasshopper.Kernel;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.Convert.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeWall : Component
  {
    public override Guid ComponentGuid => new Guid("1169CEB6-381C-4353-8ACE-874938755694");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "AW";

    public AnalyzeWall() : base
    (
      name: "Analyze Wall",
      nickname: "A-W",
      description: "Analyze given Wall element",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Wall(),
        name: "Wall",
        nickname: "W",
        description: "Wall element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.WallSystemFamily>(),
        name: "Wall System Family",
        nickname: "WSF",
        description: "System family (DB.WallKind) of the given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Wall Type",
        nickname: "WT",
        description: "Element type (DB.WallType) of the given wall instance",
        access: GH_ParamAccess.item
        );

      manager.AddParameter(
        param: new Parameters.Wall(),
        name: "Parent Stacked Wall",
        nickname: "PSW",
        description: "Parent Stacked Wall instance if given wall is a member of a Stacked Wall",
        access: GH_ParamAccess.item
        );

      manager.AddParameter(
        param: new Parameters.Level(),
        name: "Base Level",
        nickname: "BL",
        description: "Base level (constraint) of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Base Level Offset",
        nickname: "BLO",
        description: "Base level offset of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Bottom Is Attached",
        nickname: "BLA",
        description: "Whether the wall instance is attached to the base level",
        access: GH_ParamAccess.item
        );

      manager.AddParameter(
        param: new Parameters.Level(),
        name: "Top Level",
        nickname: "TL",
        description: "Top level (constraint) of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Top Level Offset",
        nickname: "TLO",
        description: "Top level offset of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddBooleanParameter(
        name: "Top Is Attached",
        nickname: "TLA",
        description: "Whether the wall instance is attached to the top level",
        access: GH_ParamAccess.item
        );

      manager.AddNumberParameter(
        name: "Height",
        nickname: "H",
        description: "Height of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Length",
        nickname: "L",
        description: "Length of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Width",
        nickname: "W",
        description: "Width of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Angle From Vertical",
        nickname: "AFV",
        description: "Slant angle of the wall",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Area",
        nickname: "A",
        description: "Area of given wall instance",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Volume",
        nickname: "V",
        description: "Volume of given wall instance",
        access: GH_ParamAccess.item
        );

      manager.AddBooleanParameter(
        name: "Is Room Bounding",
        nickname: "RB",
        description: "Whether given wall instance is room bounding",
        access: GH_ParamAccess.item
        );


      manager.AddBooleanParameter (
        name: "Structural",
        nickname: "ST",
        description: "Whether given wall instance is structural",
        access: GH_ParamAccess.item
        );
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.StructuralWallUsage>(),
        name: "Structural Usage",
        nickname: "STU",
        description: "Structural usage of given wall instance",
        access: GH_ParamAccess.item
        );

      manager.AddVectorParameter(
        name: "Orientation",
        nickname: "O",
        description: "Orientation vector of given wall instance",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      ARDB.Wall wall = default;
      if (!DA.GetData("Wall", ref wall))
        return;

      DA.SetData("Wall System Family", new Types.WallSystemFamily(wall.WallType.Kind));
      DA.SetData("Wall Type", Types.ElementType.FromElement(wall.WallType));
      if (wall.IsStackedWallMember)
        DA.SetData("Parent Stacked Wall", Types.Element.FromElement(wall.Document.GetElement(wall.StackedWallOwnerId)));

      DA.SetData("Base Level", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_CONSTRAINT).AsGoo());
      DA.SetData("Base Level Offset", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_BASE_OFFSET).AsGoo());
      DA.SetData("Bottom Is Attached", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_BOTTOM_IS_ATTACHED).AsGoo());

      DA.SetData("Top Level", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_HEIGHT_TYPE).AsGoo());
      DA.SetData("Top Level Offset", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_TOP_OFFSET).AsGoo());
      DA.SetData("Top Is Attached", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_TOP_IS_ATTACHED).AsGoo());

#if REVIT_2021
      DA.SetData("Angle From Vertical", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_SINGLE_SLANT_ANGLE_FROM_VERTICAL).AsGoo());
#else
      DA.SetData("Angle From Vertical", 0.0);
#endif
      DA.SetData("Height", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsGoo());
      DA.SetData("Length", wall?.get_Parameter(ARDB.BuiltInParameter.CURVE_ELEM_LENGTH).AsGoo());
      DA.SetData("Width", wall.GetWidth() * GeometryDecoder.ModelScaleFactor);

      DA.SetData("Area", wall?.get_Parameter(ARDB.BuiltInParameter.HOST_AREA_COMPUTED).AsGoo());
      DA.SetData("Volume", wall?.get_Parameter(ARDB.BuiltInParameter.HOST_VOLUME_COMPUTED).AsGoo());

      DA.SetData("Is Room Bounding", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_ATTR_ROOM_BOUNDING).AsGoo());

      DA.SetData("Structural", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).AsGoo());
      DA.SetData("Structural Usage", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_STRUCTURAL_USAGE_PARAM).AsGoo());

      DA.SetData("Orientation", wall.GetOrientationVector().ToVector3d());
    }
  }
}
