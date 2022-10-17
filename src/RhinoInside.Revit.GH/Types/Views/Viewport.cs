using System;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Grasshopper.Kernel;

  [Kernel.Attributes.Name("Viewport")]
  public class Viewport : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.Viewport);
    public new ARDB.Viewport Value => base.Value as ARDB.Viewport;

    public Viewport() { }
    public Viewport(ARDB.Viewport element) : base(element) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (typeof(ViewSheet).IsAssignableFrom(typeof(Q)))
      {
        target = (Q) (object) ViewSheet.FromElementId(Document, Value?.SheetId);

        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(View)))
      {
        target = (Q) (object) View.FromElementId(Document, Value?.ViewId);

        return true;
      }

      return false;
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Viewport viewport && viewport.SheetId != ARDB.ElementId.InvalidElementId)
        {
          switch (viewport.Rotation)
          {
            case ARDB.ViewportRotation.None:
              return new Plane
              (
                viewport.GetBoxCenter().ToPoint3d(),
                Vector3d.XAxis,
                Vector3d.YAxis
              );

            case ARDB.ViewportRotation.Clockwise:
              return new Plane
              (
                viewport.GetBoxCenter().ToPoint3d(),
                -Vector3d.YAxis,
                Vector3d.XAxis
              );

            case ARDB.ViewportRotation.Counterclockwise:
              return new Plane
              (
                viewport.GetBoxCenter().ToPoint3d(),
                Vector3d.YAxis,
                -Vector3d.XAxis
              );
          }
        }

        return NaN.Plane;
      }
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Viewport viewport && viewport.SheetId != ARDB.ElementId.InvalidElementId)
      {
        using (var boxOutline = viewport.GetBoxOutline())
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

        var view = Document.GetElement(viewport.ViewId) as ARDB.View;
        if (view is ARDB.ImageView) return;

        using (var boxOutline = viewport.GetLabelOutline())
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
      else base.DrawViewportWires(args);
    }
  }
}
