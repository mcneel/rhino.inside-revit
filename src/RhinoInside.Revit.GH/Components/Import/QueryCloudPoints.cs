using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
  using External.DB;
  using External.DB.Extensions;
  using Convert.Geometry;

  [ComponentVersion(introduced: "1.13")]
  public class QueryCloudPoints : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("72B92E6A-2B21-4A4D-8AE4-39837F4C6C8B");
    public override GH_Exposure Exposure => GH_Exposure.quinary;

    protected override string IconTag => string.Empty;

    public QueryCloudPoints() : base
    (
      name: "Query Cloud Points",
      nickname: "CloudPts",
      description: "Query Point Cloud points and colours.",
      category: "Revit",
      subCategory: "Model"
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
          Description = $"Convex shape to be used as a volume filter.{Environment.NewLine}Plane, Box and Mesh are accepted.",
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
      if (!Params.TryGetData(DA, "Average Distance", out double? averageDistance, x => NumericTolerance.IsFinite(x))) return;
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

      var cloud = pointCloud.Value;

      var filterValue = filter?.Value;
      if (filterValue is null)
      {
        // One plane should be enough.
        filter = new Types.PointCloudFilter();
        filterValue = ARDB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter
          (new ARDB.Plane[] { new Plane(corners[0], corners[1], corners[2]).ToPlane() });
      }

      var points = cloud.GetPoints(filterValue, GeometryEncoder.ToInternalLength(averageDistance ?? diagonal / 1000.0), numPoints.Value);
      var transform = cloud.GetTransform();

      Params.TrySetDataList(DA, "Points", () => points.Select(x => GeometryDecoder.ToPoint3d(transform.OfPoint(x))));
      if (cloud.HasColor())
      {
        var type = cloud.Document.GetElement(cloud.GetTypeId()) as ARDB.PointCloudType;
        switch (type.ColorEncoding)
        {
          case ARDB.PointClouds.PointCloudColorEncoding.ARGB:
            Params.TrySetDataList(DA, "Colours", () => points.Select(x => FromArgb(x.Color)));
            break;

          case ARDB.PointClouds.PointCloudColorEncoding.ABGR:
            Params.TrySetDataList(DA, "Colours", () => points.Select(x => FromAbgr(x.Color)));
            break;
        }
      }

      Params.TrySetData(DA, "Count", () => points.Count);
    }

    static System.Drawing.Color FromArgb(int color)
    {
      int r = color & 0xFF;
      color >>= 8;
      int g = color & 0xFF;
      color >>= 8;
      int b = color & 0xFF;
      color >>= 8;
      int a = color & 0xFF;

      return System.Drawing.Color.FromArgb(a, r, g, b);
    }

    static System.Drawing.Color FromAbgr(int color)
    {
      int b = color & 0xFF;
      color >>= 8;
      int g = color & 0xFF;
      color >>= 8;
      int r = color & 0xFF;
      color >>= 8;
      int a = color & 0xFF;

      return System.Drawing.Color.FromArgb(a, r, g, b);
    }
  }
}
