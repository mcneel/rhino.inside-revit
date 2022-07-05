using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using Convert.System.Collections.Generic;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Datum")]
  public class DatumPlane : GraphicalElement
  {
    protected override Type ValueType => typeof(ARDB.DatumPlane);
    public new ARDB.DatumPlane Value => base.Value as ARDB.DatumPlane;
    public static explicit operator ARDB.DatumPlane(DatumPlane value) => value?.Value;

    public DatumPlane() { }
    public DatumPlane(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public DatumPlane(ARDB.DatumPlane plane) : base(plane) { }
  }

  [Kernel.Attributes.Name("Level")]
  public class Level : DatumPlane, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.Level);
    public new ARDB.Level Value => base.Value as ARDB.Level;
    public static explicit operator ARDB.Level(Level value) => value?.Value;

    public Level() { }
    public Level(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public Level(ARDB.Level level) : base(level) { }

    public override bool CastFrom(object source)
    {
      var value = source;

      if (value is View view)
      {
        SetValue(view.Document, view.GenLevelId ?? ARDB.ElementId.InvalidElementId);
        return true;
      }
      else if (value is GraphicalElement element)
      {
        SetValue(element.Document, element.LevelId ?? ARDB.ElementId.InvalidElementId);
        return true;
      }

      return base.CastFrom(source);
    }

    public override BoundingBox GetBoundingBox(Transform xform) => NaN.BoundingBox;

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var height = Elevation;
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
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is ARDB.Level level)
      {
        var name = level.Name;

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
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.Level level)
        {
          var levelType = level.Document.GetElement(level.GetTypeId()) as ARDB.LevelType;
          var position = LevelExtension.GetBasePointLocation(level.Document, levelType.GetElevationBase());

          return new Plane
          (
            new Point3d(position.X * Revit.ModelUnits, position.Y * Revit.ModelUnits, level.GetElevation() * Revit.ModelUnits),
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
    public double Elevation
    {
      get => Value?.GetElevation() * Revit.ModelUnits ?? double.NaN;
      set => Value?.SetElevation(value / Revit.ModelUnits);
    }

    public double GetElevationFrom(External.DB.ElevationBase elevationBase)
    {
      return Elevation - Document.GetBasePointLocation(elevationBase).Z * Revit.ModelUnits;
    }

    public void SetElevationFrom(External.DB.ElevationBase elevationBase, double value)
    {
      Elevation = Document.GetBasePointLocation(elevationBase).Z * Revit.ModelUnits + value;
    }

    public bool? IsStructural
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_IS_STRUCTURAL).AsInteger() != 0;
      set
      {
        if (value is null || IsStructural == value) return;
        Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_IS_STRUCTURAL).Update(value.Value ? 1 : 0);
      }
    }

    public bool? IsBuildingStory
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_IS_BUILDING_STORY).AsInteger() != 0;
      set
      {
        if (value is null || IsBuildingStory == value) return;
        Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_IS_BUILDING_STORY).Update(value.Value ? 1 : 0);
      }
    }

    public double ComputationHeight
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT)?.AsDouble() * Revit.ModelUnits ?? double.NaN;
      set => Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT)?.Update(value / Revit.ModelUnits);
    }
    public double ComputationElevation
    {
      get => Elevation + ComputationHeight;
      set => ComputationHeight = value - Elevation;
    }
    #endregion
  }

  [Kernel.Attributes.Name("Grid")]
  public class Grid : DatumPlane, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.Grid);
    public new ARDB.Grid Value => base.Value as ARDB.Grid;
    public static explicit operator ARDB.Grid(Grid value) => value?.Value;

    public Grid() { }
    public Grid(ARDB.Grid grid) : base(grid) { }

    #region IGH_PreviewData
    IList<Point3d> BoundaryPoints
    {
      get
      {
        if (Value is ARDB.Grid grid)
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
      if (Value is ARDB.Grid grid)
      {
        var cameraDirection = args.Viewport.CameraDirection;
        var start = grid.Curve.GetEndPoint(0).ToPoint3d();
        var end = grid.Curve.GetEndPoint(1).ToPoint3d();
        var direction = end - start;

        if (args.Viewport.IsParallelProjection && cameraDirection.IsPerpendicularTo(Vector3d.ZAxis))
        {
          if (grid.IsCurved) return;
          if (cameraDirection.IsParallelTo(direction) == 0)
            return;
        }

        if (BoundaryPoints is IList<Point3d> boundary && boundary.Count > 0)
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
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is ARDB.Grid grid)
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

    #region Location
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.Grid grid)
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

    public override Plane Location
    {
      get
      {
        var origin = NaN.Point3d;
        var axis = NaN.Vector3d;
        var perp = NaN.Vector3d;

        if (Curve is Curve curve)
        {
          var start = curve.PointAtStart;
          var end = curve.PointAtEnd;
          axis = end - start;
          origin = start + (axis * 0.5);
          perp = axis.PerpVector();
        }

        return new Plane(origin, axis, perp);
      }
    }

    public override Curve Curve
    {
      get
      {
        if (Value is ARDB.Grid grid)
        {
          return grid.IsCurved ?
            grid.Curve.CreateReversed().ToCurve() :
            grid.Curve.ToCurve();
        }

        return default;
      }
    }

    public override Surface Surface
    {
      get
      {
        if (Value is ARDB.Grid grid)
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

          return surface;
        }

        return default;
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Reference Plane")]
  public class ReferencePlane : DatumPlane, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.ReferencePlane);
    public new ARDB.ReferencePlane Value => base.Value as ARDB.ReferencePlane;
    public static explicit operator ARDB.ReferencePlane(ReferencePlane value) => value?.Value;

    public ReferencePlane() { }
    public ReferencePlane(ARDB.ReferencePlane value) : base(value) { }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (args.Viewport.IsParallelProjection)
      {
        if (Value is ARDB.ReferencePlane referencePlane)
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
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is ARDB.ReferencePlane)
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

    #region Category
    public override Category Subcategory
    {
      get
      {
        var paramId = ARDB.BuiltInParameter.CLINE_SUBCATEGORY;
        if (paramId != ARDB.BuiltInParameter.INVALID && Value is ARDB.Element element)
        {
          using (var parameter = element.get_Parameter(paramId))
          {
            if (parameter?.AsElementId() is ARDB.ElementId categoryId)
            {
              var category = new Category(Document, categoryId);
              return category.APIObject?.Parent is null ? new Category() : category;
            }
          }
        }

        return default;
      }

      set
      {
        var paramId = ARDB.BuiltInParameter.CLINE_SUBCATEGORY;
        if (value is object && Value is ARDB.Element element)
        {
          using (var parameter = element.get_Parameter(paramId))
          {
            if (parameter is null)
            {
              if (value.Id != ARDB.ElementId.InvalidElementId)
                throw new Exceptions.RuntimeErrorException($"{((IGH_Goo) this).TypeName} '{DisplayName}' does not support assignment of a Subcategory.");
            }
            else
            {
              AssertValidDocument(value, nameof(Subcategory));
              parameter.Update(value);
            }
          }
        }
      }
    }
    #endregion

    #region Location
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.ReferencePlane referencePlane)
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

      return NaN.BoundingBox;
    }

    public override Plane Location
    {
      get
      {
        return Value is ARDB.ReferencePlane referencePlane ?
          referencePlane.GetPlane().ToPlane() :
          NaN.Plane;
      }
    }

    public override Curve Curve
    {
      get => Value is ARDB.ReferencePlane referencePlane ?
          new LineCurve(referencePlane.BubbleEnd.ToPoint3d(), referencePlane.FreeEnd.ToPoint3d()) :
          default;
    }
    #endregion
  }

  [Kernel.Attributes.Name("Reference Point")]
  public class ReferencePoint : GraphicalElement, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.ReferencePoint);
    public new ARDB.ReferencePoint Value => base.Value as ARDB.ReferencePoint;

    public ReferencePoint() { }
    public ReferencePoint(ARDB.ReferencePoint value) : base(value) { }

    #region IGH_PreviewData
    public override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.ReferencePoint referencePoint)
      {
        args.Pipeline.DrawPoint
        (
          referencePoint.Position.ToPoint3d(),
          Grasshopper.CentralSettings.PreviewPointStyle,
          Grasshopper.CentralSettings.PreviewPointRadius,
          args.Color
        );
      }
    }
    #endregion

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      // 3. Update if necessary
      if (Value is ARDB.ReferencePoint point)
      {
        att = att.Duplicate();
        att.Name = DisplayName;
        if (Category.BakeElement(idMap, false, doc, att, out var layerGuid))
          att.LayerIndex = doc.Layers.FindId(layerGuid).Index;

        guid = doc.Objects.AddPoint(point.Position.ToPoint3d(), att);

        if (guid != Guid.Empty)
        {
          idMap.Add(Id, guid);
          return true;
        }
      }

      return false;
    }
    #endregion

    #region Location
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      if (Value is ARDB.ReferencePoint referencePoint)
      {
        return new BoundingBox
        (
          new Point3d[]
          {
              referencePoint.Position.ToPoint3d(),
              referencePoint.Position.ToPoint3d()
          },
          xform
        );
      }

      return NaN.BoundingBox;
    }

    public override Plane Location
    {
      get
      {
        if (Value is ARDB.ReferencePoint referencePoint)
        {
          using (var transform = referencePoint.GetCoordinateSystem())
            return new Plane(transform.Origin.ToPoint3d(), transform.BasisX.ToVector3d(), transform.BasisY.ToVector3d());
        }

        return NaN.Plane;
      }
    }
    #endregion
  }
}
