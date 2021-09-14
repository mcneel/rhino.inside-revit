using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using RhinoInside.Revit.Convert.Geometry;
using RhinoInside.Revit.Convert.System.Collections.Generic;
using RhinoInside.Revit.External.DB.Extensions;
using DB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  [Kernel.Attributes.Name("Datum")]
  public class DatumPlane : GraphicalElement
  {
    protected override Type ValueType => typeof(DB.DatumPlane);
    public new DB.DatumPlane Value => base.Value as DB.DatumPlane;
    public static explicit operator DB.DatumPlane(DatumPlane value) => value?.Value;

    public DatumPlane() { }
    public DatumPlane(DB.Document doc, DB.ElementId id) : base(doc, id) { }
    public DatumPlane(DB.DatumPlane plane) : base(plane) { }
  }

  [Kernel.Attributes.Name("Level")]
  public class Level : DatumPlane, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(DB.Level);
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

    public override BoundingBox GetBoundingBox(Transform xform) => NaN.BoundingBox;

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var height = Height;
      if (double.IsNaN(height))
        return;

      if (args.Viewport.IsParallelProjection)
      {
        if (args.Viewport.CameraDirection.IsPerpendicularTo(Vector3d.ZAxis))
        {
          var viewportBBox = args.Viewport.GetFrustumBoundingBox();
          var length = viewportBBox.Diagonal.Length;
          args.Viewport.GetFrustumCenter(out var center);

          var point = new Point3d(center.X, center.Y, height);
          var from = point - args.Viewport.CameraX * length;
          var to = point + args.Viewport.CameraX * length;

          args.Pipeline.DrawPatternedLine(from, to, args.Color, 0x00000F0F, args.Thickness);
        }
      }
    }
    #endregion

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is DB.Level)
      {
        var name = ToString();

        // 2. Check if already exist
        var index = doc.NamedConstructionPlanes.Find(name);

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          var cplane = CreateConstructionPlane(name, Location, doc);

          if (index < 0) index = doc.NamedConstructionPlanes.Add(cplane);
          else if (overwrite) doc.NamedConstructionPlanes.Modify(cplane, index, true);
        }

        // TODO: Create a V5 Uuid out of the name
        //guid = new Guid(0, 0, 0, BitConverter.GetBytes((long) index));
        //idMap.Add(Id, guid);

        return true;
      }

      return false;
    }
    #endregion

    #region Properties
    public override BoundingBox BoundingBox => NaN.BoundingBox;

    public override Plane Location
    {
      get
      {
        if (Value is DB.Level level)
        {
          var levelType = level.Document.GetElement(level.GetTypeId()) as DB.LevelType;
          var position = LevelExtension.GetBasePointLocation(level.Document, levelType.GetElevationBase());

          return new Plane
          (
            new Point3d(position.X * Revit.ModelUnits, position.Y * Revit.ModelUnits, level.GetHeight() * Revit.ModelUnits),
            Vector3d.XAxis,
            Vector3d.YAxis
          );
        }

        return NaN.Plane;
      }
    }

    /// <summary>
    /// Signed distance along the Z axis from the World XY plane.
    /// </summary>
    /// <remarks>
    /// World XY plane origin is refered as "Internal Origin" in Revit UI.
    /// </remarks>
    public double Height
    {
      get => Value?.GetHeight() * Revit.ModelUnits ?? double.NaN;
      set => Value?.SetHeight(value / Revit.ModelUnits);
    }

    public double GetElevationAbove(External.DB.ElevationBase elevationBase)
    {
      return Height - Document.GetBasePointLocation(elevationBase).Z * Revit.ModelUnits;
    }

    public void SetElevationAbove(External.DB.ElevationBase elevationBase, double value)
    {
      Height = Document.GetBasePointLocation(elevationBase).Z * Revit.ModelUnits + value;
    }

    public bool? IsStructural
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.LEVEL_IS_STRUCTURAL).AsInteger() != 0;
      set
      {
        if (value is null || IsStructural == value) return;
        Value?.get_Parameter(DB.BuiltInParameter.LEVEL_IS_STRUCTURAL).Update(value.Value ? 1 : 0);
      }
    }

    public bool? IsBuildingStory
    {
      get => Value?.get_Parameter(DB.BuiltInParameter.LEVEL_IS_BUILDING_STORY).AsInteger() != 0;
      set
      {
        if (value is null || IsBuildingStory == value) return;
        Value?.get_Parameter(DB.BuiltInParameter.LEVEL_IS_BUILDING_STORY).Update(value.Value ? 1 : 0);
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Grid")]
  public class Grid : DatumPlane, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(DB.Grid);
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

      return NaN.BoundingBox;
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

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is DB.Grid grid)
      {
        att = att.Duplicate();
        att.Name = grid.Name;
        att.WireDensity = -1;
        att.CastsShadows = false;
        att.ReceivesShadows = false;
        if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
          att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

        // 2. Check if already exist
        var gridObject = doc.Objects.OfType<SurfaceObject>().Where
        (
          x => !x.IsInstanceDefinitionGeometry &&
          x.Attributes.LayerIndex == att.LayerIndex &&
          x.ObjectType == ObjectType.Surface &&
          x.Name == att.Name
        ).
        FirstOrDefault();

        // 3. Update if necessary
        if (gridObject is null || overwrite)
        {
          if (gridObject is null)
          {
            guid = doc.Objects.Add(Surface, att);
          }
          else
          {
            guid = gridObject.Id;
            doc.Objects.ModifyAttributes(guid, att, true);
            doc.Objects.Replace(guid, Surface);
          }
        }
        else guid = gridObject.Id;

        idMap.Add(Id, guid);
        return true;
      }

      return false;
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

        return NaN.BoundingBox;
      }
    }

    public override Plane Location
    {
      get
      {
        var origin = NaN.Point3d;
        var axis = NaN.Vector3d;
        var perp = NaN.Vector3d;

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
  public class ReferencePlane : DatumPlane, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(DB.ReferencePlane);
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

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<DB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<DB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is DB.ReferencePlane)
      {
        var name = ToString();

        // 2. Check if already exist
        var index = doc.NamedConstructionPlanes.Find(name);

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          var cplane = CreateConstructionPlane(name, Location, doc);

          if (index < 0) index = doc.NamedConstructionPlanes.Add(cplane);
          else if (overwrite) doc.NamedConstructionPlanes.Modify(cplane, index, true);
        }

        // TODO: Create a V5 Uuid out of the name
        //guid = new Guid(0, 0, 0, BitConverter.GetBytes((long) index));
        //idMap.Add(Id, guid);

        return true;
      }

      return false;
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

        return NaN.BoundingBox;
      }
    }

    public override Plane Location
    {
      get
      {
        return Value is DB.ReferencePlane referencePlane ?
          referencePlane.GetPlane().ToPlane() :
          NaN.Plane;
      }
    }

    public override Curve Curve
    {
      get => Value is DB.ReferencePlane referencePlane ?
          new LineCurve(referencePlane.BubbleEnd.ToPoint3d(), referencePlane.FreeEnd.ToPoint3d()) :
          default;
    }
    #endregion
  }
}
