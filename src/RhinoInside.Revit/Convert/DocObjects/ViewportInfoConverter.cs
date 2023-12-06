using System;
using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.Convert.DocObjects
{
  using Convert.Geometry;
  using Convert.System.Drawing;
  using External.DB.Extensions;
  using External.UI.Extensions;

  /// <summary>
  /// Represents a converter for converting <see cref="ViewportInfo"/> values
  /// back and forth Revit and Rhino.
  /// </summary>
  static class ViewportInfoConverter
  {
    class CameraInfo : ARDB.IExportContext
    {
      bool ARDB.IExportContext.Start() => true;
      void ARDB.IExportContext.Finish() { }
      bool ARDB.IExportContext.IsCanceled() => false;
      ARDB.RenderNodeAction ARDB.IExportContext.OnElementBegin(ARDB.ElementId elementId) => ARDB.RenderNodeAction.Skip;
      void ARDB.IExportContext.OnElementEnd(ARDB.ElementId elementId) { }
      ARDB.RenderNodeAction ARDB.IExportContext.OnFaceBegin(ARDB.FaceNode node) => ARDB.RenderNodeAction.Skip;
      void ARDB.IExportContext.OnFaceEnd(ARDB.FaceNode node) { }
      ARDB.RenderNodeAction ARDB.IExportContext.OnInstanceBegin(ARDB.InstanceNode node) => ARDB.RenderNodeAction.Skip;
      void ARDB.IExportContext.OnInstanceEnd(ARDB.InstanceNode node) { }
      void ARDB.IExportContext.OnLight(ARDB.LightNode node) { }
      ARDB.RenderNodeAction ARDB.IExportContext.OnLinkBegin(ARDB.LinkNode node) => ARDB.RenderNodeAction.Skip;
      void ARDB.IExportContext.OnLinkEnd(ARDB.LinkNode node) { }
      void ARDB.IExportContext.OnMaterial(ARDB.MaterialNode node) { }
      void ARDB.IExportContext.OnPolymesh(ARDB.PolymeshTopology node) { }
      void ARDB.IExportContext.OnRPC(ARDB.RPCNode node) { }

      ARDB.RenderNodeAction ARDB.IExportContext.OnViewBegin(ARDB.ViewNode node)
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

        return ARDB.RenderNodeAction.Skip;
      }

      void ARDB.IExportContext.OnViewEnd(ARDB.ElementId elementId) { }

      public static CameraInfo GetCameraInfo(ARDB.View view) => Rhinoceros.InvokeInHostContext(() =>
      {
        var camera = new CameraInfo()
        {
          EyePosition = view.Origin,
          ViewDirection = view.ViewDirection,
          UpDirection = view.UpDirection
        };

        if (view is ARDB.View3D view3D && view3D.IsPerspective)
        {
          try
          {
            using (var exporter = new ARDB.CustomExporter(view3D.Document, camera))
            {
              exporter.Export(view3D);
              return camera;
            }
          }
          catch { }
        }

        {
          var ((minX, minY), (maxX, maxY)) = view.GetModelOutline();
          var((_, _, minZ), (_, _, maxZ))  = view.CropBox;

          camera.IsPerspective    = (view as ARDB.View3D)?.IsPerspective ?? false;
          camera.HorizontalExtent = maxX - minX;
          camera.VerticalExtent   = maxY - minY;
          camera.RightOffset      = minX + 0.5 * camera.HorizontalExtent;
          camera.UpOffset         = minY + 0.5 * camera.VerticalExtent;
          camera.NearDistance     = camera.IsPerspective ? 0.1 : -maxZ;
          camera.FarDistance      = -minZ;
          camera.TargetDistance   = camera.IsPerspective ? camera.NearDistance : 1e30;
          return camera;
        }
      });

      public bool IsPerspective       = default;
      public double HorizontalExtent  = double.NaN;
      public double VerticalExtent    = double.NaN;
      public double RightOffset       = double.NaN;
      public double UpOffset          = double.NaN;
      public double NearDistance      = double.NaN;
      public double FarDistance       = double.NaN;
      public double TargetDistance    = double.NaN;
      public ARDB.XYZ EyePosition     = ARDB.XYZ.Zero;
      public ARDB.XYZ ViewDirection   = ARDB.XYZ.BasisZ;
      public ARDB.XYZ UpDirection     = ARDB.XYZ.BasisY;
    }

    public static bool TryGetViewportInfo(this ARDB.View view, bool useUIView, out ViewportInfo vport)
    {
      vport = default;

      if (!view.IsModelView())
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
            vport.TargetPoint = origin - zDirection * camera.FarDistance * 0.5 * Revit.ModelUnits;

            // Set Frustum
            {
              var nearPlane = new Plane(origin - zDirection * near, xDirection, yDirection);
              double left = double.PositiveInfinity, right = double.NegativeInfinity;
              double bottom = double.PositiveInfinity, top = double.NegativeInfinity;
              foreach (var corner in uiView.GetZoomCorners().Select(GeometryDecoder.ToPoint3d))
              {
                nearPlane.ClosestParameter(corner, out var u, out var v);

                left = Math.Min(left, u);
                right = Math.Max(right, u);
                bottom = Math.Min(bottom, v);
                top = Math.Max(top, v);
              }

              vport.SetFrustum(left, right, bottom, top, near, far);
            }

            vport.FrustumAspect = vport.FrustumWidth / vport.FrustumHeight;
            vport.SetCameraDirection(-zDirection * far);
          }
        }
      }
      else
      {
        var camera = CameraInfo.GetCameraInfo(view);
        var origin = camera.EyePosition.ToPoint3d();
        var zDirection = camera.ViewDirection.ToVector3d();
        var yDirection = camera.UpDirection.ToVector3d();
        var xDirection = Vector3d.CrossProduct(yDirection, zDirection);
        xDirection.Unitize();

        var bounds = Revit.MainWindow.Bounds;
        var ratio = camera.HorizontalExtent / camera.VerticalExtent;
        var screenPort = new global::System.Drawing.Rectangle
        (
          0, 0, (int) Math.Round(bounds.Height * ratio), bounds.Height
        );

        var near = camera.IsPerspective ?
          Math.Max(1e-6, camera.TargetDistance * Revit.ModelUnits) :
          Math.Max(1e-6, camera.NearDistance * Revit.ModelUnits);
        var far = Math.Max(near + 1e-6, camera.FarDistance * Revit.ModelUnits);

        vport = new ViewportInfo()
        {
          ScreenPort = screenPort,
          FrustumAspect = camera.HorizontalExtent / camera.VerticalExtent,
          IsPerspectiveProjection = camera.IsPerspective,
        };
        vport.SetCameraLocation(origin);
        vport.SetCameraDirection(-zDirection);
        vport.SetCameraUp(yDirection);
        vport.TargetPoint = origin - zDirection * camera.FarDistance * 0.5 * Revit.ModelUnits;

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
