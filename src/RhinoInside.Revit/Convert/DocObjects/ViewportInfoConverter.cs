using System;
using Rhino.DocObjects;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;
using RhinoInside.Revit.Convert.System.Drawing;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.UI.Extensions;

namespace RhinoInside.Revit.Convert.DocObjects
{
  public static class ViewportInfoConverter
  {
    class CameraInfo : DB.IExportContext
    {
      bool DB.IExportContext.Start() => true;
      void DB.IExportContext.Finish() { }
      bool DB.IExportContext.IsCanceled() => false;
      DB.RenderNodeAction DB.IExportContext.OnElementBegin(DB.ElementId elementId) => DB.RenderNodeAction.Skip;
      void DB.IExportContext.OnElementEnd(DB.ElementId elementId) { }
      DB.RenderNodeAction DB.IExportContext.OnFaceBegin(DB.FaceNode node) => DB.RenderNodeAction.Skip;
      void DB.IExportContext.OnFaceEnd(DB.FaceNode node) { }
      DB.RenderNodeAction DB.IExportContext.OnInstanceBegin(DB.InstanceNode node) => DB.RenderNodeAction.Skip;
      void DB.IExportContext.OnInstanceEnd(DB.InstanceNode node) { }
      void DB.IExportContext.OnLight(DB.LightNode node) { }
      DB.RenderNodeAction DB.IExportContext.OnLinkBegin(DB.LinkNode node) => DB.RenderNodeAction.Skip;
      void DB.IExportContext.OnLinkEnd(DB.LinkNode node) { }
      void DB.IExportContext.OnMaterial(DB.MaterialNode node) { }
      void DB.IExportContext.OnPolymesh(DB.PolymeshTopology node) { }
      void DB.IExportContext.OnRPC(DB.RPCNode node) { }

      DB.RenderNodeAction DB.IExportContext.OnViewBegin(DB.ViewNode node)
      {
        var cameraInfo = node.GetCameraInfo();

        IsPerspective = cameraInfo.IsPerspective;
        UpOffset = cameraInfo.UpOffset;
        RightOffset = cameraInfo.RightOffset;
        TargetDistance = cameraInfo.TargetDistance;
        NearDistance = cameraInfo.NearDistance;
        FarDistance = cameraInfo.FarDistance;
        HorizontalExtent = cameraInfo.HorizontalExtent;
        VerticalExtent = cameraInfo.VerticalExtent;

        return DB.RenderNodeAction.Skip;
      }

      void DB.IExportContext.OnViewEnd(DB.ElementId elementId) { }

      public static CameraInfo GetCameraInfo(DB.View view)
      {
        var camera = new CameraInfo()
        {
          EyePosition = view.Origin,
          ViewDirection = view.ViewDirection,
          UpDirection = view.UpDirection
        };

        if (view is DB.View3D view3D)
        {
          using (var exporter = new DB.CustomExporter(view3D.Document, camera))
          {
            exporter.Export(view3D);
            return camera;
          }
        }
        else
        {
          var min = view.CropBox.Min;
          var max = view.CropBox.Max;
          camera.IsPerspective    = false;
          camera.HorizontalExtent = max.X - min.X;
          camera.VerticalExtent   = max.Y - min.Y;
          camera.RightOffset      = min.X + 0.5 * camera.HorizontalExtent;
          camera.UpOffset         = min.Y + 0.5 * camera.VerticalExtent;
          camera.NearDistance     = -max.Z;
          camera.FarDistance      = -min.Z;
          camera.TargetDistance   = 1e30;
          return camera;
        }
      }

      public bool IsPerspective       = default;
      public double HorizontalExtent  = double.NaN;
      public double VerticalExtent    = double.NaN;
      public double RightOffset       = double.NaN;
      public double UpOffset          = double.NaN;
      public double NearDistance      = double.NaN;
      public double FarDistance       = double.NaN;
      public double TargetDistance    = double.NaN;
      public DB.XYZ EyePosition       = DB.XYZ.Zero;
      public DB.XYZ ViewDirection     = DB.XYZ.BasisZ;
      public DB.XYZ UpDirection       = DB.XYZ.BasisY;
    }

    public static bool TryGetViewportInfo(this DB.View view, bool useUIView, out ViewportInfo vport)
    {
      vport = default;

      if (!view.IsGraphicalView())
        return false;

      if (useUIView)
      {
        if (view.TryGetOpenUIView(out var uiView))
        {
          using (uiView)
          {
            var screenPort = uiView.GetWindowRectangle().ToRectangle();
            screenPort.X = 0;
            screenPort.Y = 0;

            var camera = CameraInfo.GetCameraInfo(view);
            var origin = camera.EyePosition.ToPoint3d();
            var zDirection = camera.ViewDirection.ToVector3d();
            var yDirection = camera.UpDirection.ToVector3d();
            var xDirection = Vector3d.CrossProduct(yDirection, zDirection);
            xDirection.Unitize();

            var near = camera.IsPerspective ?
              Math.Max(1e-6, camera.TargetDistance * Revit.ModelUnits) :
              Math.Max(1e-6, camera.NearDistance * Revit.ModelUnits);
            var far = Math.Max(near + 1e-6, camera.FarDistance * Revit.ModelUnits);

            vport = new ViewportInfo()
            {
              ScreenPort = screenPort,
              FrustumAspect = (double) screenPort.Width / (double) screenPort.Height,
              IsPerspectiveProjection = camera.IsPerspective,
            };
            vport.SetCameraLocation(origin);
            vport.SetCameraDirection(-zDirection);
            vport.SetCameraUp(yDirection);

            // Set Frustum
            {
              var nearPlane = new Plane(origin - zDirection * near, xDirection, yDirection);
              double left = double.PositiveInfinity, right = double.NegativeInfinity;
              double bottom = double.PositiveInfinity, top = double.NegativeInfinity;
              foreach (var corner in uiView.GetZoomCorners().Convert(GeometryDecoder.ToPoint3d))
              {
                nearPlane.ClosestParameter(corner, out var u, out var v);

                left = Math.Min(left, u);
                right = Math.Max(right, u);
                bottom = Math.Min(bottom, v);
                top = Math.Max(top, v);
              }

              vport.SetFrustum(left, right, bottom, top, near, far);
            }

            vport.SetCameraDirection(-zDirection * far);
          }
        }
      }
      else
      {
        var screenPort = view.GetOutlineRectangle();
        var camera = CameraInfo.GetCameraInfo(view);
        var origin = camera.EyePosition.ToPoint3d();
        var zDirection = camera.ViewDirection.ToVector3d();
        var yDirection = camera.UpDirection.ToVector3d();
        var xDirection = Vector3d.CrossProduct(yDirection, zDirection);
        xDirection.Unitize();

        var near = camera.IsPerspective ?
          Math.Max(1e-6, camera.TargetDistance * Revit.ModelUnits) :
          Math.Max(1e-6, camera.NearDistance * Revit.ModelUnits);
        var far = Math.Max(near + 1e-6, camera.FarDistance * Revit.ModelUnits);

        vport = new ViewportInfo()
        {
          ScreenPort = screenPort,
          FrustumAspect = (double) screenPort.Width / (double) screenPort.Height,
          IsPerspectiveProjection = camera.IsPerspective,
        };
        vport.SetCameraLocation(origin);
        vport.SetCameraDirection(-zDirection);
        vport.SetCameraUp(yDirection);

        // Set Frustum
        {
          var left   = ((-0.5 * camera.HorizontalExtent) + camera.RightOffset) * Revit.ModelUnits;
          var right  = ((+0.5 * camera.HorizontalExtent) + camera.RightOffset) * Revit.ModelUnits;
          var bottom = ((-0.5 * camera.VerticalExtent) + camera.UpOffset) * Revit.ModelUnits;
          var top    = ((+0.5 * camera.VerticalExtent) + camera.UpOffset) * Revit.ModelUnits;
          vport.SetFrustum(left, right, bottom, top, near, far);
        }

        vport.SetCameraDirection(-zDirection * far);
      }

      return vport is object;
    }
  }
}
