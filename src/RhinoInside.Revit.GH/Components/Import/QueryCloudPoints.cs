using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Annotations
{
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
        new Param_Plane()
        {
          Name = "Clipping Planes",
          NickName = "CP",
          Description = $"Planes which will be used for quick filtering cells of points.{Environment.NewLine}Usually the bounding box of the area to query.",
          Optional = true,
          Access = GH_ParamAccess.list
        }, ParamRelevance.Secondary
      ),
      new ParamDefinition
      (
        new Param_Plane()
        {
          Name = "Planes",
          NickName = "P",
          Description = "Planes which will be used for exact filtering of individual points.",
          Optional = true,
          Access = GH_ParamAccess.list
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
      if (!Params.TryGetDataList(DA, "Clipping Planes", out IList<Plane> clippingPlanes)) return;
      if (!Params.TryGetDataList(DA, "Planes", out IList<Plane> planes)) return;
      if (!Params.TryGetData(DA, "Average Distance", out double? averageDistance, x => !double.IsInfinity(x) && !double.IsNaN(x))) return;
      if (!Params.GetData(DA, "Limit", out int? numPoints)) return;

      if (numPoints < 1) return;

      var box = pointCloud.Box;
      var corners = box.GetCorners();
      var diagonal = corners[0].DistanceTo(corners[6]);

      // One plane should faster than all.
      clippingPlanes = clippingPlanes ?? new List<Plane> { new Plane(corners[0], corners[1], corners[2]) };
      planes = planes ?? new Plane[0];

      var cloud = pointCloud.Value;
      var type = cloud.Document.GetElement(cloud.GetTypeId()) as ARDB.PointCloudType;
      var transform = cloud.GetTransform();

      var filter = ARDB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter(planes.Concat(clippingPlanes).Select(x => x.ToPlane()).ToList(), planes.Count);
      var points = cloud.GetPoints(filter, GeometryEncoder.ToInternalLength(averageDistance ?? diagonal / 1000.0), numPoints.Value);

      Params.TrySetDataList(DA, "Points", () => points.Select(x => GeometryDecoder.ToPoint3d(transform.OfPoint(x))));
      if (cloud.HasColor())
      {
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
