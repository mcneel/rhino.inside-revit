using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;
using OS = System.Environment;

namespace RhinoInside.Revit.GH.Components.Insert
{
  using Numerical;
  using Convert.Geometry;
  using External.DB.Extensions;
  using System.Data;

  [ComponentVersion(introduced: "1.13")]
  public class QueryCloudPoints : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("72B92E6A-2B21-4A4D-8AE4-39837F4C6C8B");
    public override GH_Exposure Exposure => GH_Exposure.septenary;

    protected override string IconTag => string.Empty;

    public QueryCloudPoints() : base
    (
      name: "Query Cloud Points",
      nickname: "CloudPts",
      description: "Query Point Cloud points and colours.",
      category: "Revit",
      subCategory: "Insert"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition
      (
        new Parameters.PointCloudInstance()
        {
          Name = "Point Cloud",
          NickName = "PC",
        }
      ),
      new ParamDefinition
      (
        new Parameters.PointCloudFilter
        {
          Name = "Filter",
          NickName = "F",
          Description = $"Convex shape to be used as a volume filter.{OS.NewLine}Plane, Box and Mesh are accepted.",
          Optional = true
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Number
        {
          Name = "Average Distance",
          NickName = "AD",
          Description = "Average distance on the resulting points.",
          Optional = true
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Integer
        {
          Name = "Limit",
          NickName = "L",
          Description = "Maximum number of points collected.",
        }.SetDefaultVale(4096)
      ),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      new ParamDefinition
      (
        new Param_Point()
        {
          Name = "Points",
          NickName = "P",
          Access= GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Colour()
        {
          Name = "Colours",
          NickName = "C",
          Access= GH_ParamAccess.list
        }, ParamRelevance.Primary
      ),
      new ParamDefinition
      (
        new Param_Integer
        {
          Name = "Count",
          NickName = "C",
          Description = "Number of points collected.",
        }
      ),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Point Cloud", out Types.PointCloudInstance pointCloud, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Filter", out Types.PointCloudFilter filter)) return;
      if (!Params.TryGetData(DA, "Average Distance", out double? averageDistance, x => Arithmetic.IsFinite(x))) return;
      if (!Params.GetData(DA, "Limit", out int? numPoints, x => x >= 0)) return;

      if (numPoints < 1)
      {
        Params.TrySetData(DA, "Count", () => 0);
        return;
      }

      if (numPoints == 1) averageDistance = 0.0;

      var box = pointCloud.Box;
      var corners = box.GetCorners();
      var diagonal = corners[0].DistanceTo(corners[6]);

      var filterValue = filter?.Value;
      if (filterValue is null)
      {
        // One plane should be enough.
        filterValue = ARDB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter
          (new ARDB.Plane[] { new Plane(corners[0], corners[1], corners[2]).ToPlane() });
      }
      else if(pointCloud.IsLinked)
        throw new System.ComponentModel.WarningException("Complex filtering is not supported on linked models.");

      var cloud = pointCloud.Value;
      var points = cloud.GetPoints(filterValue, GeometryEncoder.ToInternalLength(averageDistance ?? diagonal / 1000.0), numPoints.Value);
      var transform = cloud.GetTransform().ToTransform();
      var modelUnits = Revit.ModelUnits;

      Params.TrySetDataList(DA, "Points", pointCloud, x => points.Select(x => transform * (modelUnits * new Point3d(x.X, x.Y, x.Z))));
      if (cloud.HasColor())
      {
        var colorEncoding = cloud.Document.GetElement<ARDB.PointCloudType>(cloud.GetTypeId()).ColorEncoding;
        Params.TrySetDataList(DA, "Colours", () => points.Select(x => Types.PointCloudInstance.ToColor(colorEncoding, x.Color)));
      }

      Params.TrySetData(DA, "Count", () => points.Count);
    }
  }
}
