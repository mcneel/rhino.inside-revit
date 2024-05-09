using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;

  public abstract class ViewInstance : GraphicalElement,
    IHostElementAccess
  {
    protected ViewInstance() { }
    public ViewInstance(ARDB.Element element) : base(element) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (typeof(ViewSheet).IsAssignableFrom(typeof(Q)))
      {
        target = (Q) (object) Sheet;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(View)))
      {
        target = (Q) (object) View;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Material)))
      {
        target = (Q) (object) new GH_Material { Value = View.ToDisplayMaterial() };
        return true;
      }

      return false;
    }

    public override Surface Surface
    {
      get
      {
        var surface = default(PlaneSurface);
        var boxOutline = DomainUV;
        if (boxOutline.IsValid)
        {
          var padding = 0.01 * Revit.ModelUnits;
          var location = Location;
          switch (Rotation)
          {
            case ARDB.ViewportRotation.None:
            {
              var outlineU = new Interval(boxOutline.U.Min - location.Origin.X + padding, boxOutline.U.Max - location.Origin.X - padding);
              var outlineV = new Interval(boxOutline.V.Min - location.Origin.Y + padding, boxOutline.V.Max - location.Origin.Y - padding);
              surface = new PlaneSurface(location, outlineU, outlineV);
              break;
            }

            case ARDB.ViewportRotation.Clockwise:
            {
              var outlineU = new Interval(boxOutline.V.Min - location.Origin.Y + padding, boxOutline.V.Max - location.Origin.Y - padding);
              var outlineV = new Interval(boxOutline.U.Min - location.Origin.X + padding, boxOutline.U.Max - location.Origin.X - padding);
              surface = new PlaneSurface(location, outlineU, outlineV);
              break;
            }

            case ARDB.ViewportRotation.Counterclockwise:
            {
              var outlineU = new Interval(boxOutline.V.Min - location.Origin.Y + padding, boxOutline.V.Max - location.Origin.Y - padding);
              var outlineV = new Interval(boxOutline.U.Min - location.Origin.X + padding, boxOutline.U.Max - location.Origin.X - padding);
              surface = new PlaneSurface(location, outlineU, outlineV);
              break;
            }
          }

          var viewOutline = View.GetOutline(Rhino.DocObjects.ActiveSpace.ModelSpace);
          surface.SetDomain(0, viewOutline.U);
          surface.SetDomain(1, viewOutline.V);
        }

        return surface;
      }
    }

    Mesh _Mesh;
    public override Mesh Mesh => _Mesh ?? (_Mesh = Mesh.CreateFromSurface(Surface));

    #region IHostElementAccess
    GraphicalElement IHostElementAccess.HostElement => Sheet?.Viewer;
    #endregion

    #region IGH_PreviewData
    protected override void SubInvalidateGraphics()
    {
      _Mesh = default;

      base.SubInvalidateGraphics();
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Element viewport && viewport.OwnerViewId != ARDB.ElementId.InvalidElementId)
      {
        using (var boxOutline = BoundingBox.ToOutline())
        {
          if (!boxOutline.IsEmpty)
          {
            var points = new Point3d[]
            {
              boxOutline.MinimumPoint.ToPoint3d(),
              Point3d.Origin,
              boxOutline.MaximumPoint.ToPoint3d(),
              Point3d.Origin
            };

            points[0] = new Point3d(points[0].X, points[0].Y, 0.0);
            points[1] = new Point3d(points[2].X, points[0].Y, 0.0);
            points[2] = new Point3d(points[2].X, points[2].Y, 0.0);
            points[3] = new Point3d(points[0].X, points[2].Y, 0.0);

            args.Pipeline.DrawPatternedPolyline(points, args.Color, 0x00003333, args.Thickness, close: true);
          }
        }
      }
      else base.DrawViewportWires(args);
    }

    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args)
    {
      if (Mesh is Mesh mesh)
        args.Pipeline.DrawMeshShaded(mesh, args.Material);
    }
    #endregion

    #region Properties
    public abstract View View { get; }
    public virtual ViewSheet Sheet => GetElement<ViewSheet>(Value?.OwnerViewId);

    protected virtual ARDB.ViewportRotation Rotation => ARDB.ViewportRotation.None;
    #endregion
  }

  [Kernel.Attributes.Name("Viewport")]
  public class Viewport : ViewInstance
  {
    protected override Type ValueType => typeof(ARDB.Viewport);
    public new ARDB.Viewport Value => base.Value as ARDB.Viewport;

    public Viewport() { }
    public Viewport(ARDB.Viewport element) : base(element) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Viewport viewport && viewport.SheetId != ARDB.ElementId.InvalidElementId)
        {
          var boxCenter = viewport.GetBoxCenter().ToPoint3d();
          boxCenter.Z = 0.0;

          switch (viewport.Rotation)
          {
            case ARDB.ViewportRotation.None:              return new Plane(boxCenter, Vector3d.XAxis, Vector3d.YAxis);
            case ARDB.ViewportRotation.Clockwise:         return new Plane(boxCenter, -Vector3d.YAxis, Vector3d.XAxis);
            case ARDB.ViewportRotation.Counterclockwise:  return new Plane(boxCenter, Vector3d.YAxis, -Vector3d.XAxis);
          }
        }

        return NaN.Plane;
      }
    }

    public override UVInterval DomainUV
    {
      get
      {
        if (Value?.GetBoxOutline() is ARDB.Outline outline)
        {
          return new UVInterval
          (
            new Interval(GeometryDecoder.ToModelLength(outline.MinimumPoint.X), GeometryDecoder.ToModelLength(outline.MaximumPoint.X)),
            new Interval(GeometryDecoder.ToModelLength(outline.MinimumPoint.Y), GeometryDecoder.ToModelLength(outline.MaximumPoint.Y))
          );
        }

        return new UVInterval(NaN.Interval, NaN.Interval);
      }
    }

#if REVIT_2022
    public override Curve Curve
    {
      get
      {
        if (Value is ARDB.Viewport viewport)
        {
          using (var labelOutline = viewport.GetLabelOutline())
          {
            if (!labelOutline.IsEmpty)
            {
              var box = new Box(viewport.GetBoxOutline().ToBoundingBox());
              var corners = box.GetCorners();
              var labelLineOffset = new Vector3d(viewport.LabelOffset.ToPoint3d());
              var labelLineLength = GeometryDecoder.ToModelLength(viewport.LabelLineLength);

              switch (viewport.Rotation)
              {
                case ARDB.ViewportRotation.None:
                  return new LineCurve(new Line(corners[0] + labelLineOffset, Vector3d.XAxis * labelLineLength));

                case ARDB.ViewportRotation.Clockwise:
                  labelLineOffset = new Vector3d(labelLineOffset.Y, labelLineOffset.X, 0.0);
                  return new LineCurve(new Line(corners[3] + labelLineOffset, -Vector3d.YAxis * labelLineLength));

                case ARDB.ViewportRotation.Counterclockwise:
                  labelLineOffset = new Vector3d(-labelLineOffset.Y, -labelLineOffset.X, 0.0);
                  return new LineCurve(new Line(corners[1] + labelLineOffset, Vector3d.YAxis * labelLineLength));
              }
            }
          }
        }

        return null;
      }

      set => base.Curve = value;
    }
#endif

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Viewport viewport && viewport.SheetId != ARDB.ElementId.InvalidElementId)
      {
        using (var boxOutline = viewport.GetBoxOutline())
        {
          if (!boxOutline.IsEmpty)
          {
            var points = new Point3d[]
            {
              boxOutline.MinimumPoint.ToPoint3d(),
              Point3d.Origin,
              boxOutline.MaximumPoint.ToPoint3d(),
              Point3d.Origin
            };

            points[0] = new Point3d(points[0].X, points[0].Y, 0.0);
            points[1] = new Point3d(points[2].X, points[0].Y, 0.0);
            points[2] = new Point3d(points[2].X, points[2].Y, 0.0);
            points[3] = new Point3d(points[0].X, points[2].Y, 0.0);

            args.Pipeline.DrawPatternedPolyline(points, args.Color, 0x00003333, args.Thickness, close: true);
          }
        }

        var view = Document.GetElement(viewport.ViewId) as ARDB.View;
        if (view is ARDB.ImageView) return;

        using (var labelOutline = viewport.GetLabelOutline())
        {
          if (!labelOutline.IsEmpty)
          {
            var points = new Point3d[]
            {
              labelOutline.MinimumPoint.ToPoint3d(),
              Point3d.Origin,
              labelOutline.MaximumPoint.ToPoint3d(),
              Point3d.Origin
            };

            points[0] = new Point3d(points[0].X, points[0].Y, 0.0);
            points[1] = new Point3d(points[2].X, points[0].Y, 0.0);
            points[2] = new Point3d(points[2].X, points[2].Y, 0.0);
            points[3] = new Point3d(points[0].X, points[2].Y, 0.0);

            args.Pipeline.DrawPatternedPolyline(points, args.Color, 0x00003333, args.Thickness, close: true);
            args.Pipeline.DrawDot(Position, Value.get_Parameter(ARDB.BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.AsString() ?? string.Empty, args.Color, System.Drawing.Color.White);
            args.Pipeline.DrawCurve(Curve, args.Color, args.Thickness);
          }
        }
      }
      else base.DrawViewportWires(args);
    }
    #endregion

    #region Properties
    public override View View => GetElement<View>(Value?.ViewId);
    public override ViewSheet Sheet => GetElement<ViewSheet>(Value?.SheetId);

    protected override ARDB.ViewportRotation Rotation => Value?.Rotation ?? ARDB.ViewportRotation.None;
    #endregion
  }

  [Kernel.Attributes.Name("Schedule Graphics")]
  public class ScheduleSheetInstance : ViewInstance
  {
    protected override Type ValueType => typeof(ARDB.ScheduleSheetInstance);
    public new ARDB.ScheduleSheetInstance Value => base.Value as ARDB.ScheduleSheetInstance;

    public ScheduleSheetInstance() { }
    public ScheduleSheetInstance(ARDB.ScheduleSheetInstance element) : base(element) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.ScheduleSheetInstance viewport && viewport.OwnerViewId != ARDB.ElementId.InvalidElementId)
        {
          var boxCenter = base.Location.Origin;
          boxCenter.Z = 0.0;

          switch (viewport.Rotation)
          {
            case ARDB.ViewportRotation.None: return new Plane(boxCenter, Vector3d.XAxis, Vector3d.YAxis);
            case ARDB.ViewportRotation.Clockwise: return new Plane(boxCenter, -Vector3d.YAxis, Vector3d.XAxis);
            case ARDB.ViewportRotation.Counterclockwise: return new Plane(boxCenter, Vector3d.YAxis, -Vector3d.XAxis);
          }
        }

        return NaN.Plane;
      }
    }

    #region Properties
    public override View View => GetElement<View>(Value?.ScheduleId);

    protected override ARDB.ViewportRotation Rotation => Value?.Rotation ?? ARDB.ViewportRotation.None;
    #endregion
  }

  [Kernel.Attributes.Name("Schedule Graphics")]
  public class PanelScheduleSheetInstance : ViewInstance
  {
    protected override Type ValueType => typeof(ARDB.Electrical.PanelScheduleSheetInstance);
    public new ARDB.Electrical.PanelScheduleSheetInstance Value => base.Value as ARDB.Electrical.PanelScheduleSheetInstance;

    public PanelScheduleSheetInstance() { }
    public PanelScheduleSheetInstance(ARDB.Electrical.PanelScheduleSheetInstance element) : base(element) { }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Electrical.PanelScheduleSheetInstance viewport && viewport.OwnerViewId != ARDB.ElementId.InvalidElementId)
        {
          var boxCenter = base.Location.Origin;
          boxCenter.Z = 0.0;

          switch (Rotation)
          {
            case ARDB.ViewportRotation.None: return new Plane(boxCenter, Vector3d.XAxis, Vector3d.YAxis);
            case ARDB.ViewportRotation.Clockwise: return new Plane(boxCenter, -Vector3d.YAxis, Vector3d.XAxis);
            case ARDB.ViewportRotation.Counterclockwise: return new Plane(boxCenter, Vector3d.YAxis, -Vector3d.XAxis);
          }
        }

        return NaN.Plane;
      }
    }

    #region Properties
    public override View View => GetElement<View>(Value?.ScheduleId);
    #endregion
  }
}
