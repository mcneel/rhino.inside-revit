using System;
using System.Linq;
using Rhino.Display;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Grasshopper.Kernel;

  [Kernel.Attributes.Name("Tag")]
  public class TagElement : GraphicalElement
  {
    protected TagElement() { }
    protected TagElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    protected TagElement(ARDB.Element element) : base(element) { }
  }

  [Kernel.Attributes.Name("Element Tag")]
  public class IndependentTag : TagElement, IGH_Annotation
  {
    protected override Type ValueType => typeof(ARDB.IndependentTag);
    public new ARDB.IndependentTag Value => base.Value as ARDB.IndependentTag;

    public IndependentTag() { }
    public IndependentTag(ARDB.IndependentTag element) : base(element) { }

    #region Location
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.IndependentTag tag)
        {
          var view = tag.Document.GetElement(tag.OwnerViewId) as ARDB.View;
          var plane = new Plane(tag.TagHeadPosition.ToPoint3d(), view.RightDirection.ToVector3d(), view.UpDirection.ToVector3d());
          return plane;
        }

        return NaN.Plane;
      }
    }
    #endregion

    #region IGH_Annotation
    public GeometryObject[] References =>
      Value?.GetTaggedReferences().
      Cast<ARDB.Reference>().
      Select(x => GeometryObject.FromReference(ReferenceDocument, x)).
      ToArray();
    #endregion
  }

  [Kernel.Attributes.Name("Spatial Element Tag")]
  public class SpatialElementTag : TagElement
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

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.SpatialElementTag tag)
      {
        var head = tag.TagHeadPosition.ToPoint3d();
        if (args.Viewport.IsParallelProjection && args.Viewport.CameraDirection.IsParallelTo(Vector3d.ZAxis) != 0)
        {
          if (tag.HasLeader)
          {
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
#if REVIT_2018
        else args.Pipeline.DrawDot(head, tag.TagText, args.Color, System.Drawing.Color.White);
#endif
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
    public GeometryObject[] References
    {
      get
      {
        if (Value is ARDB.AreaTag tag && tag.Area is ARDB.Area area)
        {
          using (var reference = ARDB.Reference.ParseFromStableRepresentation(area.Document, area.UniqueId))
            return new GeometryObject[] { GeometryElement.FromReference(area.Document, reference) };
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
    public GeometryObject[] References
    {
      get
      {
        if (Value is ARDB.Architecture.RoomTag tag)
        {
          try { return new GeometryObject[] { GeometryObject.FromLinkElementId(ReferenceDocument, tag.TaggedRoomId) }; }
          catch { }

          try { return new GeometryObject[] { GeometryObject.FromElementId(ReferenceDocument, tag.TaggedLocalRoomId) }; }
          catch { }
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
    public GeometryObject[] References
    {
      get
      {
        if (Value is ARDB.Mechanical.SpaceTag tag && tag.Space is ARDB.Mechanical.Space space)
        {
          using (var reference = ARDB.Reference.ParseFromStableRepresentation(space.Document, space.UniqueId))
            return new GeometryObject[] { GeometryElement.FromReference(space.Document, reference) };
        }

        return default;
      }
    }
    #endregion
  }
}
