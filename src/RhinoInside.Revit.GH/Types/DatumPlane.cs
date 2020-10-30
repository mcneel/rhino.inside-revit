using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Datum")]
  public class DatumPlane : GraphicalElement
  {
    protected override Type ScriptVariableType => typeof(DB.DatumPlane);
    public new DB.DatumPlane Value => base.Value as DB.DatumPlane;
    public static explicit operator DB.DatumPlane(DatumPlane value) => value?.Value;

    public DatumPlane() { }
    public DatumPlane(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public DatumPlane(DB.DatumPlane plane) : base(plane) { }
  }

  [Kernel.Attributes.Name("Level")]
  public class Level : DatumPlane
  {
    protected override Type ScriptVariableType => typeof(DB.Level);
    public new DB.Level Value => base.Value as DB.Level;
    public static explicit operator DB.Level(Level value) => value?.Value;

    public Level() { }
    public Level(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public Level(DB.Level level) : base(level) { }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (source is IGH_Goo goo)
        value = goo.ScriptVariable();

      if (value is DB.View view)
        return view.GenLevel is null ? false : SetValue(view.GenLevel);

      return base.CastFrom(source);
    }

    public override BoundingBox GetBoundingBox(Transform xform) => BoundingBox.Unset;

    #region IGH_PreviewData
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
    #endregion

    #region Properties
    public override BoundingBox BoundingBox => BoundingBox.Unset;

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

    public double Elevation => Value?.Elevation * Revit.ModelUnits ?? double.NaN;
    #endregion
  }

  [Kernel.Attributes.Name("Grid")]
  public class Grid : DatumPlane
  {
    protected override Type ScriptVariableType => typeof(DB.Grid);
    public new DB.Grid Value => base.Value as DB.Grid;
    public static explicit operator DB.Grid(Grid value) => value?.Value;

    public Grid() { }
    public Grid(DB.Grid grid) : base(grid) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is DB.Grid grid)
      {
        var bbox = grid.GetExtents().ToBoundingBox();
        var curve = grid.Curve.ToCurve();

        var curveA = curve.DuplicateCurve(); curveA.Translate(0.0, 0.0, bbox.Min.Z - curve.PointAtStart.Z);
        var curveB = curve.DuplicateCurve(); curveB.Translate(0.0, 0.0, bbox.Max.Z - curve.PointAtStart.Z);

        bbox = BoundingBox.Empty;
        bbox.Union(curveA.GetBoundingBox(xform));
        bbox.Union(curveB.GetBoundingBox(xform));
        return bbox;
      }

      return BoundingBox.Unset;
    }

    #region IGH_PreviewData
    IList<Point3d> BoundaryPoints
    {
      get
      {
        if (Value is DB.Grid grid)
        {
          var points = grid.Curve?.Tessellate().ConvertAll(GeometryDecoder.ToPoint3d);
          if (points is object)
          {
            var bbox = BoundingBox;
            var polyline = new List<Point3d>(points.Length * 2);

            for (int p = 0; p < points.Length; ++p)
              points[p] = new Point3d(points[p].X, points[p].Y, bbox.Min.Z);

            polyline.AddRange(points);

            for (int p = 0; p < points.Length; ++p)
              points[p] = new Point3d(points[p].X, points[p].Y, bbox.Max.Z);

            polyline.AddRange(points.Reverse());

            return polyline;
          }
        }

        return default;
      }
    }

    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is DB.Grid grid)
      {
        var cameraDirection = args.Viewport.CameraDirection;
        var start = grid.Curve.GetEndPoint(0).ToPoint3d();
        var end = grid.Curve.GetEndPoint(1).ToPoint3d();
        var direction = end - start;

        if (args.Viewport.IsParallelProjection && cameraDirection.IsPerpendicularTo(Vector3d.ZAxis))
        {
          if(grid.IsCurved) return;
          if (cameraDirection.IsParallelTo(direction) == 0)
            return;
        }

        if(BoundaryPoints is IList<Point3d> boundary && boundary.Count > 0)
        {
          args.Pipeline.DrawPatternedPolyline(boundary, args.Color, 0x0007E30, args.Thickness, true);

          if
          (
            args.Viewport.IsParallelProjection &&
            (cameraDirection.IsPerpendicularTo(Vector3d.ZAxis) || cameraDirection.IsParallelTo(Vector3d.ZAxis) != 0)
          )
          {
            args.Viewport.GetFrustumNearPlane(out var near);
            args.Viewport.GetFrustumCenter(out var center);
            center = near.ClosestPoint(center);

            Point3d tagA, tagB;
            if (cameraDirection.IsPerpendicularTo(Vector3d.ZAxis))
            {
              tagA = boundary.First();
              tagB = boundary.Last();
            }
            else
            {
              tagA = start;
              tagB = end;
            }

            if (center.DistanceTo(near.ClosestPoint(tagA)) > center.DistanceTo(near.ClosestPoint(tagB)))
              args.Pipeline.DrawDot(tagA, grid.Name, args.Color, System.Drawing.Color.White);
            else
              args.Pipeline.DrawDot(tagB, grid.Name, args.Color, System.Drawing.Color.White);
          }
        }
      }
    }
    #endregion

    #region Properties
    public override BoundingBox BoundingBox
    {
      get
      {
        if (Value is DB.Grid grid)
        {
          var bbox = grid.GetExtents().ToBoundingBox();
          bbox.Union(Curve.GetBoundingBox(true));
          return bbox;
        }

        return BoundingBox.Unset;
      }
    }

    public override Plane Location
    {
      get
      {
        var origin = new Point3d(double.NaN, double.NaN, double.NaN);
        var axis = new Vector3d(double.NaN, double.NaN, double.NaN);
        var perp = new Vector3d(double.NaN, double.NaN, double.NaN);

        if (Value is DB.Grid grid)
        {
          var start = grid.Curve.Evaluate(0.0, normalized: true).ToPoint3d();
          var end = grid.Curve.Evaluate(1.0, normalized: true).ToPoint3d();
          axis = end - start;
          origin = start + (axis * 0.5);
          perp = axis.PerpVector();
        }

        return new Plane(origin, axis, perp);
      }
    }

    public override Curve Curve => Value?.Curve.ToCurve();

    public override Brep Surface
    {
      get
      {
        if (Value is DB.Grid grid)
        {
          var bbox = BoundingBox;
          var curve = grid.Curve.ToCurve();

          var curveA = curve.DuplicateCurve(); curveA.Translate(0.0, 0.0, bbox.Min.Z - curve.PointAtStart.Z);
          var curveB = curve.DuplicateCurve(); curveB.Translate(0.0, 0.0, bbox.Max.Z - curve.PointAtStart.Z);

          var surface = NurbsSurface.CreateRuledSurface(curveA, curveB);

          if (curve is LineCurve)
          {
            var plane = new Plane(Point3d.Origin, curve.PointAtEnd - curve.PointAtStart, Vector3d.ZAxis);
            plane.ClosestParameter(curve.PointAtStart, out var t0, out var _);
            plane.ClosestParameter(curve.PointAtEnd, out var t1, out var _);
            surface.SetDomain(0, new Interval(t0, t1));
          }

          surface.SetDomain(1, new Interval(bbox.Min.Z, bbox.Max.Z));

          return Brep.CreateFromSurface(surface);
        }

        return default;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Reference Plane")]
  public class ReferencePlane : DatumPlane
  {
    protected override Type ScriptVariableType => typeof(DB.ReferencePlane);
    public new DB.ReferencePlane Value => base.Value as DB.ReferencePlane;
    public static explicit operator DB.ReferencePlane(ReferencePlane value) => value?.Value;

    public ReferencePlane() { }
    public ReferencePlane(DB.ReferencePlane value) : base(value) { }

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is DB.ReferencePlane referencePlane)
      {
        return new BoundingBox
        (
          new Point3d[]
          {
              referencePlane.FreeEnd.ToPoint3d(),
              referencePlane.BubbleEnd.ToPoint3d()
          },
          xform
        );
      }

      return base.GetBoundingBox(xform);
    }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (args.Viewport.IsParallelProjection)
      {
        if (Value is DB.ReferencePlane referencePlane)
        {
          if (args.Viewport.CameraDirection.IsPerpendicularTo(referencePlane.Normal.ToVector3d()))
          {
            var from = referencePlane.FreeEnd.ToPoint3d();
            var to = referencePlane.BubbleEnd.ToPoint3d();
            args.Pipeline.DrawPatternedLine(from, to, args.Color, 0x00000F0F, args.Thickness);
          }
        }
      }
    }
    #endregion

    #region Properties
    public override BoundingBox BoundingBox
    {
      get
      {
        if (Value is DB.ReferencePlane referencePlane)
        {
          return new BoundingBox
          (
            new Point3d[]
            {
              referencePlane.FreeEnd.ToPoint3d(),
              referencePlane.BubbleEnd.ToPoint3d()
            }
          );
        }

        return BoundingBox.Unset;
      }
    }

    public override Plane Location
    {
      get
      {
        return Value is DB.ReferencePlane referencePlane ?
          referencePlane.GetPlane().ToPlane() :
          new Plane
          (
            new Point3d(double.NaN, double.NaN, double.NaN),
            Vector3d.Zero,
            Vector3d.Zero
          );
      }
    }

    public override Curve Curve
    {
      get => Value is DB.ReferencePlane referencePlane ?
          new LineCurve(referencePlane.BubbleEnd.ToPoint3d(), referencePlane.FreeEnd.ToPoint3d()) :
        default;
      //set
      //{
      //  if (value is object && Value is DB.ReferencePlane referencePlane)
      //  {
      //    if (value.TryGetLine(out var line, Revit.VertexTolerance * Revit.ModelUnits))
      //    {
      //      base.Curve = default;
      //    }
      //  }
      //}
    }
    #endregion
  }
}
