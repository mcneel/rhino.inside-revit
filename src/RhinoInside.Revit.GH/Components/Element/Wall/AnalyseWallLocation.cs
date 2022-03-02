using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using RhinoInside.Revit.GH.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  public class AnalyzeWallLocationCurve : Component
  {
    public override Guid ComponentGuid => new Guid("4C5260C3-B15E-482B-8A1D-38CD868E3E72");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override string IconTag => "AWLC";

    public AnalyzeWallLocationCurve() : base(
      name: "Analyze Wall Location Curve",
      nickname: "A-WLC",
      description: "Analyze location curve of given wall instance",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    // in and out params
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
        param: new Param_Enum<Types.WallLocationLine>(),
        name: "Location Line",
        nickname: "LL",
        description: "Location line setting of the wall element",
        access: GH_ParamAccess.item
        );
      manager.AddCurveParameter(
        name: "Center Curve",
        nickname: "CC",
        description: "Center curve of the wall element",
        access: GH_ParamAccess.item
        );
      manager.AddCurveParameter(
        name: "Location Curve",
        nickname: "LC",
        description: "Wall base curve at the location curve of the wall element",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Offset Value",
        nickname: "O",
        description: "Offset value of location curve from center curve of wall element",
        access: GH_ParamAccess.item
        );
    }

    // support methods
    private ARDB.WallType GetApplicableType(ARDB.Wall wall)
    {
      if (wall.WallType.Kind == ARDB.WallKind.Stacked)
      {
        ARDB.ElementId baseWallId = wall.GetStackedWallMemberIds().First();
        ARDB.Wall baseWall = wall.Document.GetElement(baseWallId) as ARDB.Wall;
        return baseWall.WallType;
      }
      else
        return wall.WallType;
    }

    private ARDB.XYZ GetOffsetPlaneNormal(ARDB.Wall wall)
    {
      var offsetPlaneNormal = -(ARDB.XYZ.BasisZ);
      return wall.Flipped ? -offsetPlaneNormal : offsetPlaneNormal;
    }

    private double GetOffsetForLocationCurve(ARDB.Wall wall)
    {
      var wallType = GetApplicableType(wall);
      var cstruct = wallType.GetCompoundStructure();
      if (cstruct != null)
      {
        int wallLocationLine = wall.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).AsInteger();
        return cstruct.GetOffsetForLocationLine(
          (ARDB.WallLocationLine) Enum.ToObject(typeof(ARDB.WallLocationLine), wallLocationLine)
          );
      }
      else
        return 0;
    }

    private ARDB.Curve OffsetLocationCurve(ARDB.Curve centerCurve, double offsetValue, ARDB.XYZ offsetPlaneNormal)
    {
      return centerCurve.CreateOffset(offsetValue, offsetPlaneNormal);
    }

    // solver
    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      ARDB.Wall wall = default;
      if (!DA.GetData("Wall", ref wall))
        return;

      DA.SetData("Center Curve", wall.GetCenterCurve().ToCurve());
      DA.SetData("Location Line", wall?.get_Parameter(ARDB.BuiltInParameter.WALL_KEY_REF_PARAM).AsGoo());

      var offsetPlaneNormal = GetOffsetPlaneNormal(wall);
      var offsetValue = GetOffsetForLocationCurve(wall);
      var locationCurve = OffsetLocationCurve(wall.GetLocationCurve().Curve, offsetValue, offsetPlaneNormal);
      DA.SetData("Offset Value", offsetValue);
      DA.SetData("Location Curve", locationCurve.ToCurve());
    }
  }
}
