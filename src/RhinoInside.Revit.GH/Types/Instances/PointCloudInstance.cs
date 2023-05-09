using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Point Cloud")]
  public class PointCloudInstance : Instance
  {
    protected override Type ValueType => typeof(ARDB.PointCloudInstance);
    public new ARDB.PointCloudInstance Value => base.Value as ARDB.PointCloudInstance;

    public PointCloudInstance() { }
    public PointCloudInstance(ARDB.PointCloudInstance instance) : base(instance) { }

    static Rhino.Display.DisplayBitmap DotBitmap = default;

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (!IsValid) return;

      var cloud = Value;
      var box = Box;
      var corners = box.GetCorners();
      var diagonal = GeometryEncoder.ToInternalLength(corners[0].DistanceTo(corners[6]));
      args.Viewport.GetWorldToScreenScale(box.Center, out var pixelPerUnit);

      // Frustum planes
      args.Viewport.GetFrustumLeftPlane  (out var left);
      args.Viewport.GetFrustumRightPlane (out var right);
      args.Viewport.GetFrustumBottomPlane(out var bottom);
      args.Viewport.GetFrustumTopPlane   (out var top);
      var near = new Plane(args.Viewport.CameraLocation, args.Viewport.CameraDirection);
      var clippingPlanes = new List<ARDB.Plane>(6)
      {
        left.ToPlane(),   right.ToPlane(),
        bottom.ToPlane(), top.ToPlane(),
        near.ToPlane()
      };

      // Clipping planes
      var clipingPlaneObjects = args.Viewport?.ParentView?.Document.Objects.FindClippingPlanesForViewport(args.Viewport);
      foreach (var clipPlaneObject in clipingPlaneObjects ?? Enumerable.Empty<ClippingPlaneObject>())
        clippingPlanes.Add(clipPlaneObject.ClippingPlaneGeometry.Plane.ToPlane());

      var filter = ARDB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter(clippingPlanes);
      var averageDistance = GeometryEncoder.ToInternalLength(1.0 / pixelPerUnit);
      var numPoints = args.Pipeline.IsDynamicDisplay ? 0xFFFF / 8 : (int) Math.Min(1_000_000 - 1, diagonal / 3.0 * 0x7FFF);

      var points = cloud.GetPoints(filter, averageDistance, numPoints);
      if (points.Count > 0)
      {
        var hasColor = cloud.HasColor();
        var type = cloud.Document.GetElement(cloud.GetTypeId()) as ARDB.PointCloudType;
        var colorEncoding = type.ColorEncoding;
        var dotColor = System.Drawing.Color.FromArgb(0xFF, System.Drawing.Color.LightGray); // Ignore alpha component.
        var dotSize = (float) 2.0f * args.Pipeline.DpiScale;

        args.Pipeline.PushModelTransform(cloud.GetTransform().ToTransform());
        {
          var particles = new ParticleSystem();
          foreach (var point in points)
            particles.Add
            (
              new Particle()
              {
                Location = GeometryDecoder.ToPoint3d(point),
                Color = hasColor ? System.Drawing.Color.FromArgb(0xFF, ToColor(colorEncoding, point.Color)) : dotColor,
                Size = dotSize,
                DisplayBitmapIndex = 0,
              }
            );

          if (DotBitmap is null)
          {
            using (var bitmap = new System.Drawing.Bitmap(32, 32))
            {
              using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                graphics.FillRectangle(System.Drawing.Brushes.White, 0, 0, bitmap.Width, bitmap.Height);

              DotBitmap = new Rhino.Display.DisplayBitmap(bitmap);
            }
          }

          args.Pipeline.DrawParticles(particles, DotBitmap);
        }
        args.Pipeline.PopModelTransform();
      }

      base.DrawViewportWires(args);
    }

    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }

    #region PointCloudColorEncoding
    internal static System.Drawing.Color ToColor(ARDB.PointClouds.PointCloudColorEncoding encoding, int value)
    {
      switch (encoding)
      {
        case ARDB.PointClouds.PointCloudColorEncoding.ARGB: return FromArgb(value);
        case ARDB.PointClouds.PointCloudColorEncoding.ABGR: return FromAbgr(value);
      }

      return System.Drawing.Color.Empty;
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
    #endregion
  }

  [Kernel.Attributes.Name("Point Cloud Filter")]
  public class PointCloudFilter : GH_Goo<ARDB.PointClouds.PointCloudFilter>
  {
    public PointCloudFilter() { }
    public PointCloudFilter(ARDB.PointClouds.PointCloudFilter filter) : base(filter) { }

    public override string ToString() => TypeName;

    public override bool IsValid => Value is object;

    public override string TypeName => "Point Cloud Filter";

    public override string TypeDescription => "A Revit point cloud filter";

    public override IGH_Goo Duplicate() => new PointCloudFilter(Value?.Clone());

    public override bool CastFrom(object value)
    {
      if (value is GH_Plane ghPlane)
      {
        if (!ghPlane.Value.IsValid) return false;

        var plane = ghPlane.Value;
        plane.Flip();

        var planes = new List<ARDB.Plane>(2)
        {
          plane.ToPlane(),
          plane.ToPlane()
        };

        Value = ARDB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter(planes, 1);
        return true;
      }

      if (value is GH_Box ghBox)
      {
        if (!(ghBox.Value.IsValid is true)) return false;

        var bbox = ghBox.Value.ToBoundingBoxXYZ();
        bbox.GetPlaneEquations(out var clippingPlanes);
        var planes = new List<ARDB.Plane>(6)
        {
          ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.X.Min.Value.Normal, clippingPlanes.X.Min.Value.Point),
          ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.X.Max.Value.Normal, clippingPlanes.X.Max.Value.Point),
          ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Y.Min.Value.Normal, clippingPlanes.Y.Min.Value.Point),
          ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Y.Max.Value.Normal, clippingPlanes.Y.Max.Value.Point),
          ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Z.Min.Value.Normal, clippingPlanes.Z.Min.Value.Point),
          ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Z.Max.Value.Normal, clippingPlanes.Z.Max.Value.Point)
        };

        Value = ARDB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter(planes);
        return true;
      }

      if (value is GH_Mesh ghMesh)
      {
        if (!(ghMesh.Value.IsValid is true)) return false;

        var mesh = ghMesh.Value.DuplicateMesh();
        var meshIsClosed = mesh.IsClosed;

        var tol = GeometryTolerance.Model;
        mesh.Faces.ConvertNonPlanarQuadsToTriangles(tol.VertexTolerance, tol.AngleTolerance, 0);
        mesh.FaceNormals.ComputeFaceNormals();
        var meshVertices = mesh.Vertices.ToPoint3dArray();

        // Check if is convex
        for (int f = 0; f < mesh.Faces.Count; ++f)
        {
          var plane = new Plane(meshVertices[mesh.Faces[f].A], mesh.FaceNormals[f]);
          for (int v = 0; v < meshVertices.Length; ++v)
          {
            if (plane.DistanceTo(meshVertices[v]) > tol.VertexTolerance)
              return false;
          }
        }

        var planes = new List<ARDB.Plane>(mesh.Faces.Count + 6);

        int index = 0;
        foreach (var face in mesh.Faces)
          planes.Add(new Plane(meshVertices[face[0]], -mesh.FaceNormals[index++]).ToPlane());

        var exactPlaneCount = planes.Count;
        if (meshIsClosed)
        {
          var coordSystem = Transform.Identity;
          if (Plane.FitPlaneToPoints(meshVertices, out var fitPlane) != PlaneFitResult.Failure)
            coordSystem = Transform.ChangeBasis(Plane.WorldXY, fitPlane);
          else
            fitPlane = Plane.WorldXY;

          var bbox = mesh.GetBoundingBox(coordSystem).ToBoundingBoxXYZ();
          bbox.Transform = Transform.ChangeBasis(fitPlane, Plane.WorldXY).ToTransform();

          bbox.GetPlaneEquations(out var clippingPlanes);
          planes.Add(ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.X.Min.Value.Normal, clippingPlanes.X.Min.Value.Point));
          planes.Add(ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.X.Max.Value.Normal, clippingPlanes.X.Max.Value.Point));
          planes.Add(ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Y.Min.Value.Normal, clippingPlanes.Y.Min.Value.Point));
          planes.Add(ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Y.Max.Value.Normal, clippingPlanes.Y.Max.Value.Point));
          planes.Add(ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Z.Min.Value.Normal, clippingPlanes.Z.Min.Value.Point));
          planes.Add(ARDB.Plane.CreateByNormalAndOrigin(clippingPlanes.Z.Max.Value.Normal, clippingPlanes.Z.Max.Value.Point));
        }

        Value = ARDB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter(planes, exactPlaneCount);
        return true;
      }

      return base.CastFrom(value);
    }
  }
}
