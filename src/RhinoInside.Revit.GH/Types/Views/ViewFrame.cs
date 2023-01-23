using System;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB.Extensions;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class ViewFrame : GH_GeometricGoo<ViewportInfo>, IGH_PreviewData
  {
    public ViewFrame() { }
    public ViewFrame(ViewportInfo info) : base(info) { }
    public ViewFrame(ViewportInfo info, string title) : base(info) => Title = title;

    public ViewFrame(ViewFrame other)
    {
      if (other is null) throw new ArgumentNullException(nameof(other));

      ReferenceID = other.ReferenceID;
      if (other.Value is object) Value = new ViewportInfo(other.Value);
      Title = other.Title;
    }

    public override bool IsValid => Value is object;

    public override string TypeName => "View Frame";

    public override string TypeDescription => "View frame";

    public string Title { get; set; } = string.Empty;

    [Flags]
    enum ClippingPlanes
    {
      None      = 0,
      Left      = 1 << 0,
      Right     = 1 << 1,
      Bottom    = 1 << 2,
      Top       = 1 << 3,
      Far       = 1 << 4,
      Near      = 1 << 5,
      Default   = Left | Right | Bottom | Top,
    }

    ClippingPlanes EnabledClipPlanes = ClippingPlanes.Default;

    public bool ClipLeft { get => EnabledClipPlanes.HasFlag(ClippingPlanes.Left); set => EnabledClipPlanes = EnabledClipPlanes.WithFlag(ClippingPlanes.Left, value); }
    public bool ClipRight { get => EnabledClipPlanes.HasFlag(ClippingPlanes.Right); set => EnabledClipPlanes = EnabledClipPlanes.WithFlag(ClippingPlanes.Right, value); }
    public bool ClipBottom { get => EnabledClipPlanes.HasFlag(ClippingPlanes.Bottom); set => EnabledClipPlanes = EnabledClipPlanes.WithFlag(ClippingPlanes.Bottom, value); }
    public bool ClipTop { get => EnabledClipPlanes.HasFlag(ClippingPlanes.Top); set => EnabledClipPlanes = EnabledClipPlanes.WithFlag(ClippingPlanes.Top, value); }
    public bool ClipFar { get => EnabledClipPlanes.HasFlag(ClippingPlanes.Far); set => EnabledClipPlanes = EnabledClipPlanes.WithFlag(ClippingPlanes.Far, value); }
    public bool ClipNear { get => EnabledClipPlanes.HasFlag(ClippingPlanes.Near); set => EnabledClipPlanes = EnabledClipPlanes.WithFlag(ClippingPlanes.Near, value); }

    public override object ScriptVariable() => Value is null ? null : new ViewportInfo(Value);

    public override string ToString()
    {
      if (Value == null)
        return "Null View Frame";

      if (!Value.IsValid)
        return "Invalid View Frame";

      if (!string.IsNullOrWhiteSpace(Title))
        return Title;

      if (Value.IsTwoPointPerspectiveProjection)
        return "Two-point perspective";

      if (Value.IsPerspectiveProjection)
      {
        Value.GetCameraAngles(out var _, out var _, out var h);
        return $"Perspective ({Math.Round(Value.Camera35mmLensLength, 0)}mm. | {Math.Round(h * 2.0 * 180.0 / Math.PI, 0)}°)";
      }

      if (Value.IsParallelProjection)
      {
        switch (Value.CameraDirection.IsParallelTo(Vector3d.ZAxis))
        {
          case -1: return "Top view";
          case +1: return "Bottom view";
        }
        switch (Value.CameraDirection.IsParallelTo(Vector3d.YAxis))
        {
          case -1: return "Back view";
          case +1: return "Front view";
        }
        switch (Value.CameraDirection.IsParallelTo(Vector3d.XAxis))
        {
          case -1: return "Right view";
          case +1: return "Left view";
        }

        return "Parallel";
      }

      return "Unrecogized projection";
    }

    public override bool CastFrom(object source)
    {
      if (source is ViewportInfo view)
      {
        Value = new ViewportInfo(view);
        return true;
      }
      else if (source is GH_Goo<ViewportInfo> info)
      {
        Value = info.Value is null ? null : new ViewportInfo(info.Value);
        return true;
      }

      if (source is IGH_Goo goo)
        source = goo.ScriptVariable();

      switch (source)
      {
        case Plane plane:
        {
          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(plane.Origin);
          vport.SetCameraDirection(-plane.ZAxis);
          vport.SetCameraUp(plane.YAxis);
          var radius = Grasshopper.CentralSettings.PreviewPlaneRadius;
          var width = radius * 2;
          var height = radius * 2;

          var target = Math.Sqrt(width * width + height * height);
          var near = double.Epsilon;

          if (vport.SetFrustum(-radius, +radius, -radius, +radius, near, near + target))
          {
            EnabledClipPlanes = ClippingPlanes.None;
            Value = vport;
            return true;
          }
          else return false;
        }

        case Rectangle3d rect:
        {
          var target = Math.Sqrt(rect.Width * rect.Width + rect.Height * rect.Height);
          var near = 0.5 * Math.Sqrt(3.0) * rect.Width;
          var nearPlane = rect.Plane;

          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(nearPlane.Origin + nearPlane.ZAxis * near);
          vport.SetCameraDirection(nearPlane.ZAxis * -(near + target));
          vport.SetCameraUp(nearPlane.YAxis);
          vport.TargetPoint = nearPlane.Origin - nearPlane.ZAxis * (target * 0.5);

          if (vport.SetFrustum(rect.X.T0, rect.X.T1, rect.Y.T0, rect.Y.T1, near, near + target))
          {
            EnabledClipPlanes = ClippingPlanes.None;
            Value = vport;
            return true;
          }
          else return false;
        }

        case Box box:
        {
          var vport = new ViewportInfo { IsParallelProjection = true };
          vport.SetCameraLocation(box.Plane.Origin);
          vport.SetCameraDirection(-box.Plane.ZAxis);
          vport.SetCameraUp(box.Plane.YAxis);

          if (vport.SetFrustum(box.X.T0, box.X.T1, box.Y.T0, box.Y.T1, -box.Z.T0, -box.Z.T1))
          {
            EnabledClipPlanes = ClippingPlanes.Default | ClippingPlanes.Far | ClippingPlanes.Near;
            Value = vport;
            return true;
          }
          else return false;
        }
      }

      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (!IsValid)
        return false;

      if (typeof(ViewportInfo).IsAssignableFrom(typeof(Q)))
      {
        target = (Q) (object) Value;
        return true;
      }
      else if (target is GH_Goo<ViewportInfo> info)
      {
        info.Value = Value is null ? null : new ViewportInfo(Value);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Interval)))
      {
        target = (Q) (object) new GH_Interval(new Interval(-Value.FrustumNear, -Value.FrustumFar));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Interval2D)))
      {
        var intervalU = new Interval(Value.FrustumLeft, Value.FrustumRight);
        var intervalV = new Interval(Value.FrustumBottom, Value.FrustumTop);
        target = (Q) (object) new GH_Interval2D(new UVInterval(intervalU, intervalV));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        target = (Q) (object) new GH_Point(Value.CameraLocation);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        target = (Q) (object) new GH_Vector(Value.CameraDirection);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(Plane)))
      {
        target = (Q) (object) new Plane(Value.CameraLocation, Value.CameraX, Value.CameraY);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        target = (Q) (object) new GH_Plane(new Plane(Value.CameraLocation, Value.CameraX, Value.CameraY));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Transform)))
      {
        target = (Q) (object) new GH_Transform(Value.GetXform(CoordinateSystem.World, CoordinateSystem.Camera));
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Rectangle)))
      {
        var corners = Value.GetNearPlaneCorners();
        var rectangle = new Rectangle3d(Value.FrustumNearPlane, corners[0], corners[3]);
        target = (Q) (object) new GH_Rectangle(rectangle);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Box)))
      {
        var box = new Box
        (
          new Plane(Value.CameraLocation, Value.CameraX, Value.CameraY),
          new Interval(Value.FrustumLeft, Value.FrustumRight),
          new Interval(Value.FrustumBottom, Value.FrustumTop),
          new Interval(-Value.FrustumFar, -Value.FrustumNear)
        );
        target = (Q) (object) new GH_Box(box);
        return true;
      }

      return false;
    }

    internal ARDB.BoundingBoxXYZ ToBoundingBoxXYZ()
    {
      if (Value is null) return null;

      var transform = ARDB.Transform.Identity;
      {
        transform.Origin = Value.CameraLocation.ToXYZ();
        transform.BasisX = Value.CameraX.ToXYZ();
        transform.BasisY = Value.CameraY.ToXYZ();
        transform.BasisZ = Value.CameraZ.ToXYZ();
      }

      var box = new ARDB.BoundingBoxXYZ()
      {
        Transform = transform,
        Min = new Point3d(Value.FrustumLeft, Value.FrustumBottom, -Value.FrustumFar).ToXYZ(),
        Max = new Point3d(Value.FrustumRight, Value.FrustumTop, -Value.FrustumNear).ToXYZ(),
      };

      box.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisX, ClipLeft);
      box.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisX, ClipRight);
      box.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisY, ClipBottom);
      box.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisY, ClipTop);
      box.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMin, BoundingBoxXYZExtension.AxisZ, ClipFar);
      box.set_BoundEnabled(BoundingBoxXYZExtension.BoundsMax, BoundingBoxXYZExtension.AxisZ, ClipNear);

      return box;
    }

    #region IGH_PreviewData
    BoundingBox IGH_PreviewData.ClippingBox
    {
      get
      {
        if (IsValid)
        {
          var clippingBox = Boundingbox;
          if (Value.TargetPoint.IsValid)
            clippingBox.Union(Value.TargetPoint);

          return clippingBox;
        }

        return BoundingBox.Empty;
      }
    }

    void IGH_PreviewData.DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    void IGH_PreviewData.DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value == null)
        return;

      var pN = Value.GetNearPlaneCorners();
      var pF = Value.GetFarPlaneCorners();

      args.Pipeline.DrawPoint(Value.CameraLocation, PointStyle.RoundSimple, 4, args.Color);
      var cameraDirection = new Line(Value.CameraLocation, Value.CameraDirection);
      args.Pipeline.DrawArrow(cameraDirection, args.Color, args.Pipeline.DpiScale * 15, 0);

      if (Value.TargetPoint.IsValid)
        args.Pipeline.DrawPoint(Value.TargetPoint, PointStyle.X, 8, args.Color);

      if (pN?.Length == 4 && pF?.Length == 4)
      {
        args.Pipeline.DrawPoints(pN, PointStyle.RoundSimple, 4, args.Color);

        args.Pipeline.DrawLine(Value.CameraLocation, pN[0], args.Color);
        args.Pipeline.DrawLine(Value.CameraLocation, pN[1], args.Color);
        args.Pipeline.DrawLine(Value.CameraLocation, pN[2], args.Color);
        args.Pipeline.DrawLine(Value.CameraLocation, pN[3], args.Color);

        args.Pipeline.DrawDottedLine(pN[0], pN[1], args.Color);
        args.Pipeline.DrawDottedLine(pN[1], pN[3], args.Color);
        args.Pipeline.DrawDottedLine(pN[3], pN[2], args.Color);
        args.Pipeline.DrawDottedLine(pN[2], pN[0], args.Color);

        args.Pipeline.DrawDottedLine(pF[0], pF[1], args.Color);
        args.Pipeline.DrawDottedLine(pF[1], pF[3], args.Color);
        args.Pipeline.DrawDottedLine(pF[3], pF[2], args.Color);
        args.Pipeline.DrawDottedLine(pF[2], pF[0], args.Color);

        args.Pipeline.DrawDottedLine(pN[0], pF[0], args.Color);
        args.Pipeline.DrawDottedLine(pN[1], pF[1], args.Color);
        args.Pipeline.DrawDottedLine(pN[2], pF[2], args.Color);
        args.Pipeline.DrawDottedLine(pN[3], pF[3], args.Color);

        var near = Value.FrustumNearPlane;
        var lineUp = new Line(near.Origin, near.YAxis, 0.5 * pN[2].DistanceTo(pN[0]));
        //var lineIn = new Line(near.Origin, -near.ZAxis, 1.0 * pN[2].DistanceTo(pN[0]));

        args.Pipeline.DrawArrow(lineUp, args.Color, args.Pipeline.DpiScale * 15, 0);
        //args.Pipeline.DrawArrow(lineIn, args.Color, 15, 0);
      }
    }
    #endregion

    #region IGH_GeometricGoo
    public override Guid ReferenceID { get; set; } = Guid.Empty;

    public override void ClearCaches()
    {
      if (IsReferencedGeometry)
      {
        Title = string.Empty;
        ClipLeft = ClipRight = ClipBottom = ClipTop = true;
        ClipFar = ClipNear = false;
      }

      base.ClearCaches();
    }

    public override bool IsGeometryLoaded => m_value is object;

    public override bool LoadGeometry(RhinoDoc doc)
    {
      if (ReferenceID != Guid.Empty)
      {
        if (doc.Views.Find(ReferenceID) is RhinoView rhinoView)
        {
          Value = new ViewportInfo(rhinoView.MainViewport);
          Title = rhinoView.MainViewport.Name;
          return true;
        }
      }

      return false;
    }

    public override IGH_GeometricGoo DuplicateGeometry() => Value is null ? null : new ViewFrame(this);

    public override BoundingBox Boundingbox
    {
      get
      {
        if (Value == null)
          return BoundingBox.Empty;

        var near = Value.GetNearPlaneCorners();
        var far = Value.GetFarPlaneCorners();

        var boxNear = new BoundingBox(near);
        var boxFar = new BoundingBox(far);
        var boxAll = BoundingBox.Union(boxNear, boxFar);

        boxAll.Union(Value.CameraLocation);

        return boxAll;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (IsValid)
      {
        var bbox = BoundingBox.Empty;

        foreach (var point in Value.GetNearPlaneCorners())
        {
          point.Transform(xform);
          bbox.Union(point);
        }

        foreach (var point in Value.GetFarPlaneCorners())
        {
          point.Transform(xform);
          bbox.Union(point);
        }

        {
          var point = Value.CameraLocation;
          point.Transform(xform);
          bbox.Union(point);
        }

        return bbox;
      }

      return BoundingBox.Unset;
    }

    public override IGH_GeometricGoo Transform(Transform xform)
    {
      if (Value.IsValid && Value.TransformCamera(xform))
        return this;

      return default;
    }

    public override IGH_GeometricGoo Morph(SpaceMorph xmorph) => default;
    #endregion

    #region GH_ISerializable
    public override bool Write(GH_IWriter writer)
    {
      writer.SetGuid("RefID", ReferenceID);
      if (!string.IsNullOrWhiteSpace(Title)) writer.SetString("Title", Title);
      if (EnabledClipPlanes != ClippingPlanes.Default) writer.SetInt32("EnabledClipPlanes", (int) EnabledClipPlanes);

      if (ReferenceID == Guid.Empty && m_value is object)
      {
        var data = GH_Convert.CommonObjectToByteArray(m_value);
        if (data is object)
          writer.SetByteArray("ON_Data", data);
      }

      return true;
    }

    public override bool Read(GH_IReader reader)
    {
      ClearCaches();
      ReferenceID = Guid.Empty;
      Value = null;

      ReferenceID = reader.GetGuid("RefID");

      var title = string.Empty;
      reader.TryGetString("Title", ref title);
      Title = title;

      var enabledClipPlanes = (int) ClippingPlanes.Default;
      reader.TryGetInt32("EnabledClipPlanes", ref enabledClipPlanes);
      EnabledClipPlanes = (ClippingPlanes) enabledClipPlanes;

      if (reader.ItemExists("ON_Data"))
      {
        var data = reader.GetByteArray("ON_Data");
        m_value = GH_Convert.ByteArrayToCommonObject<ViewportInfo>(data);
      }

      if (ReferenceID != Guid.Empty) LoadGeometry();

      return true;
    }
    #endregion
  }
}
