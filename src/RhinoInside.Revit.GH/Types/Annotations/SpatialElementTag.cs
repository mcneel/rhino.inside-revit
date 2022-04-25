using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Grasshopper.Kernel;
  using Rhino.Display;

  [Kernel.Attributes.Name("Spatial Element Tag")]
  public class SpatialElementTag : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.SpatialElementTag);
    public new ARDB.SpatialElementTag Value => base.Value as ARDB.SpatialElementTag;

    public SpatialElementTag() { }
    public SpatialElementTag(ARDB.SpatialElementTag element) : base(element) { }

    public override string DisplayName => Value is ARDB.SpatialElementTag tag && tag.IsOrphaned ?
      base.DisplayName + " (Orphaned)" : base.DisplayName;

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.SpatialElementTag tag && tag.Location is ARDB.LocationPoint point)
        {
          var plane = new Plane(point.Point.ToPoint3d(), Vector3d.XAxis, Vector3d.YAxis);
          plane.Rotate(point.Rotation, tag.View.ViewDirection.ToVector3d());
          return plane;
        }

        return NaN.Plane;
      }
    }

    public override Level Level => Level.FromElement(Value?.View.GenLevel) as Level;
    #endregion

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.SpatialElementTag tag)
      {
        var head = tag.TagHeadPosition.ToPoint3d();
        if (args.Viewport.IsParallelProjection && args.Viewport.CameraDirection.IsParallelTo(Vector3d.ZAxis) != 0)
        {
          if (tag.HasLeader)
          {
            var end = tag.LeaderEnd.ToPoint3d();
            if (tag.HasElbow)
            {
              var elbow = tag.LeaderElbow.ToPoint3d();
              args.Pipeline.DrawPolyline(new Point3d[] { head, elbow, end }, args.Color);
            }
            else args.Pipeline.DrawLine(new Line(head, end), args.Color);
          }
        }

        var pixelSize = ((1.0 / args.Pipeline.Viewport.PixelsPerUnit(head).X) / Revit.ModelUnits) / args.Pipeline.DpiScale;
        var tagSize = 1.0; // feet
        var dotPixels = 20.0 * args.Pipeline.DpiScale;
        if (dotPixels * pixelSize > tagSize)
        {
          var color = System.Drawing.Color.FromArgb(128, System.Drawing.Color.White);
          switch (Value)
          {
            case ARDB.AreaTag _:              color = System.Drawing.Color.FromArgb(254, 251, 219); break;
            case ARDB.Architecture.RoomTag _: color = System.Drawing.Color.FromArgb(216, 238, 247); break;
            case ARDB.Mechanical.SpaceTag _:  color = System.Drawing.Color.FromArgb(216, 255, 216); break;
          }

          args.Pipeline.DrawPoint
          (
            head, PointStyle.Tag,
            args.Color,
            color,
            (float) (tagSize / pixelSize),
            1.0f, 0.0f, 0.0f,
            diameterIsInPixels: true,
            autoScaleForDpi: false
          );
        }
        else args.Pipeline.DrawDot(head, tag.TagText, args.Color, System.Drawing.Color.White);
      }
    }
  }

  [Kernel.Attributes.Name("Area Tag")]
  public class AreaElementTag : SpatialElementTag
  {
    protected override Type ValueType => typeof(ARDB.AreaTag);
    public new ARDB.AreaTag Value => base.Value as ARDB.AreaTag;

    public AreaElementTag() { }
    public AreaElementTag(ARDB.AreaTag element) : base(element) { }
  }

  [Kernel.Attributes.Name("Room Tag")]
  public class RoomElementTag : SpatialElementTag
  {
    protected override Type ValueType => typeof(ARDB.Architecture.RoomTag);
    public new ARDB.Architecture.RoomTag Value => base.Value as ARDB.Architecture.RoomTag;

    public RoomElementTag() { }
    public RoomElementTag(ARDB.Architecture.RoomTag element) : base(element) { }
  }

  [Kernel.Attributes.Name("Space Tag")]
  public class SpaceElementTag : SpatialElementTag
  {
    protected override Type ValueType => typeof(ARDB.Mechanical.SpaceTag);
    public new ARDB.Mechanical.SpaceTag Value => base.Value as ARDB.Mechanical.SpaceTag;

    public SpaceElementTag() { }
    public SpaceElementTag(ARDB.Mechanical.SpaceTag element) : base(element) { }
  }
}
