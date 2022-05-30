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

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Viewport viewport)
        {
          return new Plane
          (
            viewport.GetBoxCenter().ToPoint3d(),
            Vector3d.XAxis,
            Vector3d.YAxis
          );
        }

        return base.Location;
      }
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Viewport viewport)
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
    }
  }
}
