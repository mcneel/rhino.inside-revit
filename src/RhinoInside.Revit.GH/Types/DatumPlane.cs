using System;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  public class DatumPlane : GraphicalElement
  {
    public override string TypeDescription => "Represents a Revit DatumPlane";
    protected override Type ScriptVariableType => typeof(DB.DatumPlane);
    public static explicit operator DB.DatumPlane(DatumPlane value) =>
      value?.IsValid == true ? value.Document.GetElement(value) as DB.DatumPlane : default;

    public DatumPlane() { }
    public DatumPlane(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public DatumPlane(DB.DatumPlane plane) : base(plane) { }

    public override string DisplayName
    {
      get
      {
        var element = (DB.DatumPlane) this;
        if (element is object)
          return element.Name;

        return base.DisplayName;
      }
    }
  }

  public class Level : DatumPlane
  {
    public override string TypeDescription => "Represents a Revit level";
    protected override Type ScriptVariableType => typeof(DB.Level);
    public static explicit operator DB.Level(Level value) => value?.Value;
    public new DB.Level Value => base.Value as DB.Level;

    public Level() { }
    public Level(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public Level(DB.Level level) : base(level) { }

    #region IGH_PreviewData
    public override BoundingBox ClippingBox => BoundingBox.Unset;

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var elevation = Elevation;
      if (double.IsNaN(elevation))
        return;

      if (args.Viewport.IsParallelProjection)
      {
        if (args.Viewport.CameraDirection.IsPerpendicularTo(Vector3d.ZAxis))
        {
          var viewportBBox = args.Viewport.GetFrustumBoundingBox();
          var length = viewportBBox.Diagonal.Length;
          args.Viewport.GetFrustumCenter(out var center);
          var point = new Point3d(center.X, center.Y, elevation);
          var from = point - args.Viewport.CameraX * length;
          var to = point + args.Viewport.CameraX * length;
          args.Pipeline.DrawPatternedLine(from, to, args.Color, 0x00000F0F, args.Thickness);
        }
      }
    }

    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is DB.View view)
        return view.GenLevel is null ? false : SetValue(view.GenLevel);

      return base.CastFrom(source);
    }

    public double Elevation => Value?.Elevation * Revit.ModelUnits ?? double.NaN;

    public override Plane Location
    {
      get
      {
        return Value is DB.Level level ?
        new Plane
        (
          new Point3d(0.0, 0.0, level.Elevation * Revit.ModelUnits),
          Vector3d.XAxis,
          Vector3d.YAxis
        ) :
        new Plane
        (
          new Point3d(double.NaN, double.NaN, double.NaN),
          Vector3d.Zero,
          Vector3d.Zero
        );
      }
    }
  }

  public class Grid : DatumPlane
  {
    public override string TypeDescription => "Represents a Revit grid";
    protected override Type ScriptVariableType => typeof(DB.Grid);
    public static explicit operator DB.Grid(Grid value) => value?.Value;
    public new DB.Grid Value => base.Value as DB.Grid;

    public Grid() { }
    public Grid(DB.Grid grid) : base(grid) { }

    #region IGH_PreviewData
    public override BoundingBox ClippingBox => Curve?.GetBoundingBox(false) ?? BoundingBox.Unset;

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var cplane = args.Viewport.ConstructionPlane();
      if (cplane.ZAxis.IsParallelTo(Vector3d.ZAxis) != 0)
      {
        if (Value is DB.Grid grid)
        {
          var points = grid.Curve?.Tessellate().ConvertAll(GeometryDecoder.ToPoint3d);
          if (points is object)
          {
            for (int i = 0; i < points.Length; ++i)
              points[i] = cplane.ClosestPoint(points[i]);

            args.Pipeline.DrawPatternedPolyline(points, args.Color, 0x0007E30, args.Thickness, false);

            args.Viewport.GetFrustumNearPlane(out var near);
            args.Viewport.GetFrustumCenter(out var center);
            var start = points.First();
            var end = points.Last();
            center = near.ClosestPoint(center);

            if (near.ClosestPoint(start).DistanceTo(center) > near.ClosestPoint(end).DistanceTo(center))
              args.Pipeline.DrawDot(start, grid.Name, args.Color, System.Drawing.Color.White);
            else
              args.Pipeline.DrawDot(end, grid.Name, args.Color, System.Drawing.Color.White);
          }
        }
      }
    }

    public override void DrawViewportMeshes(GH_PreviewMeshArgs args) { }
    #endregion

    public override Curve Curve => Value?.Curve.ToCurve();
  }
}
