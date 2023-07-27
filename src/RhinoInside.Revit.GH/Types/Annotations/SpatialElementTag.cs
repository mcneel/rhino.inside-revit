using System;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;
  using Numerical;

  [Kernel.Attributes.Name("Spatial Element Tag")]
  public abstract class SpatialElementTag : TagElement
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

    public override ARDB.ElementId LevelId => Value?.View.GenLevel.Id;
    #endregion

    #region IAnnotationLeadersAcces
    public override bool? HasLeader
    {
      get => Value?.HasLeader;
      set
      {
        if (Value is ARDB.SpatialElementTag tag && value is object && value.Value != tag.HasLeader)
        {
          tag.HasLeader = value.Value;
          InvalidateGraphics();
        }
      }
    }
    public override AnnotationLeader[] Leaders => new AnnotationLeader[] { new MonoLeader(this) };

    class MonoLeader : AnnotationLeader
    {
      protected readonly SpatialElementTag tag;

      public MonoLeader(SpatialElementTag t) => tag = t;

      public override Point3d HeadPosition => tag.Value.TagHeadPosition.ToPoint3d();

      public override bool Visible
      {
        get => tag.HasLeader is true;
        set { tag.HasLeader = value; tag.InvalidateGraphics(); }
      }

#if REVIT_2018
      public override bool HasElbow => tag.Value?.HasElbow ?? false;
#else
      public override bool HasElbow => false;
#endif
      public override Point3d ElbowPosition
      {
        get => tag.Value.LeaderElbow.ToPoint3d();
        set { tag.Value.LeaderElbow = value.ToXYZ(); tag.InvalidateGraphics(); }
      }

      public override Point3d EndPosition
      {
        get => tag.Value.LeaderEnd.ToPoint3d();
        set { tag.Value.LeaderEnd = value.ToXYZ(); tag.InvalidateGraphics(); }
      }

      public override bool IsTextPositionAdjustable => false;
      public override Point3d TextPosition
      {
        get => tag.Value.TagHeadPosition.ToPoint3d();
        set
        {
          var position = value.ToXYZ();
          if (!position.AlmostEqualPoints(tag.Value.TagHeadPosition))
          {
            try { tag.Value.TagHeadPosition = position; }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            { throw new Exceptions.RuntimeArgumentException($"Tag is outside of its boundary.\nEnable Leader or move Tag within its boundary.{{{tag.Id.ToString("D")}}}"); }

            tag.InvalidateGraphics();
          }
        }
      }
    }
    #endregion

    public SpatialElement SpatialElement => IsValid ?
      GetElementFromReference<SpatialElement>(References.FirstOrDefault()?.GetReference()) : null;

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.SpatialElementTag tag)
      {
        var head = tag.TagHeadPosition.ToPoint3d();
        //if (args.Viewport.IsParallelProjection && args.Viewport.CameraDirection.IsParallelTo(Vector3d.ZAxis) != 0)
        {
          if (tag.HasLeader)
          {
            //var curve = Segments[0].LeaderCurve;
            //args.Pipeline.DrawCurve(curve, args.Color, args.Thickness);
            var end = tag.LeaderEnd.ToPoint3d();
#if REVIT_2018
            if (tag.HasElbow)
            {
              var elbow = tag.LeaderElbow.ToPoint3d();
              args.Pipeline.DrawPolyline(new Point3d[] { head, elbow, end }, args.Color);
            }
            else
#endif
            {
              args.Pipeline.DrawLine(new Line(head, end), args.Color);
            }
          }
        }

        var pixelSize = ((1.0 / args.Pipeline.Viewport.PixelsPerUnit(head).X) / Revit.ModelUnits) / args.Pipeline.DpiScale;
        var tagSize = 1.0; // feet
        var dotPixels = 20.0 * args.Pipeline.DpiScale;
        if (dotPixels * pixelSize > tagSize)
        {
          var color = System.Drawing.Color.White;
          switch (Value)
          {
            case ARDB.AreaTag _: color = System.Drawing.Color.FromArgb(254, 251, 219); break;
            case ARDB.Architecture.RoomTag _: color = System.Drawing.Color.FromArgb(216, 238, 247); break;
            case ARDB.Mechanical.SpaceTag _: color = System.Drawing.Color.FromArgb(216, 255, 216); break;
          }

          var rotation = (float) -(Constant.Tau / 4.0);
          args.Pipeline.DrawPoint
          (
            head, PointStyle.Tag,
            args.Color,
            color,
            (float) (tagSize / pixelSize),
            1.0f, 0.0f, rotation,
            diameterIsInPixels: true,
            autoScaleForDpi: false
          );
        }
        else if (SpatialElement is SpatialElement spatialElement)
        {
          var name = spatialElement.Name;
          var number = spatialElement.Number;

          if (string.IsNullOrEmpty(name))
            args.Pipeline.DrawDot(head, number, args.Color, System.Drawing.Color.White);
          else
            args.Pipeline.DrawDot(head, $"{name}{Environment.NewLine}{number}", args.Color, System.Drawing.Color.White);
        }
      }
    }
  }

  [Kernel.Attributes.Name("Area Tag")]
  public class AreaElementTag : SpatialElementTag, IGH_Annotation
  {
    protected override Type ValueType => typeof(ARDB.AreaTag);
    public new ARDB.AreaTag Value => base.Value as ARDB.AreaTag;

    public AreaElementTag() { }
    public AreaElementTag(ARDB.AreaTag element) : base(element) { }

    #region IGH_Annotation
    public override GeometryObject[] References
    {
      get
      {
        if (Value is ARDB.AreaTag tag && tag.Area is ARDB.Area area)
        {
          using (var reference = ARDB.Reference.ParseFromStableRepresentation(area.Document, area.UniqueId))
            return new GeometryObject[] { GetGeometryObjectFromReference<GeometryElement>(reference) };
        }

        return default;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Room Tag")]
  public class RoomElementTag : SpatialElementTag, IGH_Annotation
  {
    protected override Type ValueType => typeof(ARDB.Architecture.RoomTag);
    public new ARDB.Architecture.RoomTag Value => base.Value as ARDB.Architecture.RoomTag;

    public RoomElementTag() { }
    public RoomElementTag(ARDB.Architecture.RoomTag element) : base(element) { }

    #region IGH_Annotation
    public override GeometryObject[] References
    {
      get
      {
        if (Value is ARDB.Architecture.RoomTag tag)
        {
          if (tag.IsTaggingLink)
          {
            try { return new GeometryObject[] { GeometryObject.FromLinkElementId(ReferenceDocument, tag.TaggedRoomId) }; }
            catch { }
          }
          else
          {
            try { return new GeometryObject[] { GeometryObject.FromElementId(Document, tag.TaggedLocalRoomId) }; }
            catch { }
          }
        }

        return default;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Space Tag")]
  public class SpaceElementTag : SpatialElementTag, IGH_Annotation
  {
    protected override Type ValueType => typeof(ARDB.Mechanical.SpaceTag);
    public new ARDB.Mechanical.SpaceTag Value => base.Value as ARDB.Mechanical.SpaceTag;

    public SpaceElementTag() { }
    public SpaceElementTag(ARDB.Mechanical.SpaceTag element) : base(element) { }

    #region IGH_Annotation
    public override GeometryObject[] References
    {
      get
      {
        if (Value is ARDB.Mechanical.SpaceTag tag && tag.Space is ARDB.Mechanical.Space space)
        {
          using (var reference = ARDB.Reference.ParseFromStableRepresentation(space.Document, space.UniqueId))
            return new GeometryObject[] { GetGeometryObjectFromReference<GeometryElement>(reference) };
        }

        return default;
      }
    }
    #endregion
  }
}
