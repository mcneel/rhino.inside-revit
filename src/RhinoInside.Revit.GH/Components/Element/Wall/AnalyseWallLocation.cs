using System;
using System.Linq;
using Grasshopper.Kernel;
using RhinoInside.Revit.Convert.Geometry;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components
{
  public class AnalyzeWallLocation : AnalysisComponent
  {
    public override Guid ComponentGuid => new Guid("4C5260C3-B15E-482B-8A1D-38CD868E3E72");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override string IconTag => "AWLC";

    public AnalyzeWallLocation() : base(
      name: "Analyze Wall Location Curve",
      nickname: "A-WLC",
      description: "Analyze location curve of given wall instance",
      category: "Revit",
      subCategory: "Analyze"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Element(),
        name: "Wall",
        nickname: "W",
        description: "Wall element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.WallLocationLine_ValueList(),
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

    private DB.WallType GetApplicableType(DB.Wall wall)
    {
      if (wall.WallType.Kind == DB.WallKind.Stacked)
      {
        DB.ElementId baseWallId = wall.GetStackedWallMemberIds().First();
        DB.Wall baseWall = wall.Document.GetElement(baseWallId) as DB.Wall;
        return baseWall.WallType;
      }
      else
        return wall.WallType;
    }

    private DB.XYZ GetOffsetPlaneNormal(DB.Wall wall)
    {
      var offsetPlaneNormal = -(DB.XYZ.BasisZ);
      return wall.Flipped ? -offsetPlaneNormal : offsetPlaneNormal;
    }

    private double GetOffsetForLocationCurve(DB.Wall wall)
    {
      var wallType = GetApplicableType(wall);
      var cstruct = wallType.GetCompoundStructure();
      if (cstruct != null)
      {
        int wallLocationLine = wall.get_Parameter(DB.BuiltInParameter.WALL_KEY_REF_PARAM).AsInteger();
        return cstruct.GetOffsetForLocationLine(
          (DB.WallLocationLine) Enum.ToObject(typeof(DB.WallLocationLine), wallLocationLine)
          );
      }
      else
        return 0;
    }

    private DB.Curve ComputeLocationCurve(DB.Curve centerCurve, double offsetValue, DB.XYZ offsetPlaneNormal)
    {
      return centerCurve.CreateOffset(offsetValue, offsetPlaneNormal);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // grab input wall type
      DB.Wall wallInstance = default;
      if (!DA.GetData("Wall", ref wallInstance))
        return;

      var centerCurve = wallInstance.Location as DB.LocationCurve;
      DA.SetData("Center Curve", centerCurve.Curve.ToCurve());
      PipeHostParameter<Types.WallLocationLine>(DA, wallInstance, DB.BuiltInParameter.WALL_KEY_REF_PARAM, "Location Line");

      var offsetPlaneNormal = GetOffsetPlaneNormal(wallInstance);
      var offsetValue = GetOffsetForLocationCurve(wallInstance);
      var locationCurve = ComputeLocationCurve(centerCurve.Curve, offsetValue, offsetPlaneNormal);
      DA.SetData("Offset Value", offsetValue);
      DA.SetData("Location Curve", locationCurve.ToCurve());
    }
  }
}
