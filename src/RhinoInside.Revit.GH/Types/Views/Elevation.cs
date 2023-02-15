using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.External.DB;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Elevation")]
  public class ElevationView : ViewSection
  {
    protected override Type ValueType => typeof(ARDB.ViewSection);

    public ElevationView() { }
    public ElevationView(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ElevationView(ARDB.ViewSection view) : base(view) { }
  }

  [Kernel.Attributes.Name("Elevation Marker")]
  public class ElevationMarker : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.ElevationMarker);
    public new ARDB.ElevationMarker Value => base.Value as ARDB.ElevationMarker;

    public ElevationMarker() { }
    public ElevationMarker(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ElevationMarker(ARDB.ElevationMarker mark) : base(mark) { }

    protected override void SubInvalidateGraphics()
    {
      _Location = null;
      base.SubInvalidateGraphics();
    }

    Plane? _Location;
    public override Plane Location
    {
      get
      {
        if (!_Location.HasValue)
        {
          _Location = NaN.Plane;

          if (Value is ARDB.ElevationMarker mark)
          {
            try
            {
              var viewX = mark.Document.GetElement(mark.GetViewId(0)) as ARDB.View;
              var viewY = mark.Document.GetElement(mark.GetViewId(1)) as ARDB.View;

              var planeX = viewX is object ? new Plane(viewX.Origin.ToPoint3d(), viewX.RightDirection.ToVector3d(), viewX.UpDirection.ToVector3d()) : default;
              var planeY = viewY is object ? new Plane(viewY.Origin.ToPoint3d(), viewY.RightDirection.ToVector3d(), viewY.UpDirection.ToVector3d()) : default;

              if (mark.IsAvailableIndex(0) || mark.IsAvailableIndex(1))
              {
                using (mark.Document.RollBackScope())
                using (mark.Document.RollBackScope()) // We need a SubTransaction here to avoid changes are made on the document.
                {
                  var level = ARDB.Level.Create(mark.Document, 0.0);
                  var plan = ARDB.ViewPlan.Create(mark.Document, mark.Document.GetDefaultElementTypeId(ARDB.ElementTypeGroup.ViewTypeFloorPlan), level.Id);

                  if (mark.IsAvailableIndex(0))
                  {
                    viewX = mark.CreateElevation(mark.Document, plan.Id, 0);
                    planeX = new Plane(viewX.Origin.ToPoint3d(), viewX.RightDirection.ToVector3d(), viewX.UpDirection.ToVector3d());
                  }

                  if (mark.IsAvailableIndex(1))
                  {
                    viewY = mark.CreateElevation(mark.Document, plan.Id, 1);
                    planeY = new Plane(viewY.Origin.ToPoint3d(), viewY.RightDirection.ToVector3d(), viewY.UpDirection.ToVector3d());
                  }
                }
              }

              _Location = Intersection.PlanePlane(planeX, planeY, out var line) ?
                new Plane(new Point3d(line.FromX, line.FromY, 0.0), planeX.Normal, planeY.Normal) :
                NaN.Plane;
            }
            catch { }
          }
        }

        return _Location.Value;
      }
    }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      var location = Location;
      if (location.IsValid)
      {
        var radius = 0.025 * Revit.ModelUnits;
        var box = new Box(location, new Interval(-radius, +radius), new Interval(-radius, +radius), new Interval(-radius, +radius));
        return box.GetBoundingBox(xform);
      }

      return NaN.BoundingBox;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var location = Location;
      if (location.IsValid)
      {
        var pointStyle = Rhino.Display.PointStyle.Circle;
        var angle = 0.0f;
        var radius = 6.0f;
        var secondarySize = 3.5f;
        var strokeColor = (System.Drawing.Color) Rhino.Display.ColorRGBA.ApplyGamma(new Rhino.Display.ColorRGBA(args.Color), 2.0);
        args.Pipeline.DrawPoint(location.Origin, pointStyle, strokeColor, args.Color, radius, 2.0f, secondarySize, angle, true, true);

        if (!Value.IsAvailableIndex(0)) args.Pipeline.DrawDirectionArrow(location.Origin, -location.XAxis, args.Color);
        if (!Value.IsAvailableIndex(1)) args.Pipeline.DrawDirectionArrow(location.Origin, -location.YAxis, args.Color);
        if (!Value.IsAvailableIndex(2)) args.Pipeline.DrawDirectionArrow(location.Origin,  location.XAxis, args.Color);
        if (!Value.IsAvailableIndex(3)) args.Pipeline.DrawDirectionArrow(location.Origin,  location.YAxis, args.Color);
      }
    }

    protected override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
  }
}
