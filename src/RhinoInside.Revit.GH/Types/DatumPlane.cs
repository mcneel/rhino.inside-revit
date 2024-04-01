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
    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = NaN.BoundingBox;
      return false;
    }

    protected override bool IsVisible(Rhino.Display.DisplayPipeline pipeline) =>
      pipeline.Viewport.IsParallelProjection &&
      pipeline.Viewport.CameraDirection.IsPerpendicularTo(Vector3d.ZAxis);

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      var height = Elevation;
      if (double.IsNaN(height))
        return;

      var viewportBBox = args.Viewport.GetFrustumBoundingBox();
      var length = viewportBBox.Diagonal.Length;
      args.Viewport.GetFrustumCenter(out var center);

      var point = new Point3d(center.X, center.Y, height);
      var from = point - args.Viewport.CameraX * length;
      var to = point + args.Viewport.CameraX * length;

      args.Pipeline.DrawPatternedLine(from, to, args.Color, 0x00000F0F, args.Thickness);
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

    public ProjectElevation ProjectElevation
    {
      get => new ProjectElevation(this);
      set
      {
        if (value is null) return;
        if (value.IsElevation(out var elevation))
        {
          Value?.SetElevation(elevation / Revit.ModelUnits);
        }
        else if (value.Value.IsOffset(out var offset) && ProjectElevation.Value.IsRelative(out var _, out var baseElement))
        {
          Value?.SetElevation(new External.DB.ElevationElementReference(offset, baseElement).Elevation);
        }
      }
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

    public double? ComputationHeight
    {
      get => Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT)?.AsDouble() * Revit.ModelUnits;
      set
      {
        if (value is null || ComputationHeight == value) return;
        Value?.get_Parameter(ARDB.BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT).Update(value.Value);
      }
    }
    #endregion
  }

  [Kernel.Attributes.Name("Grid")]
  public class Grid : DatumPlane, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.Grid);
    public new ARDB.Grid Value => base.Value as ARDB.Grid;

    public Grid() { }
    public Grid(ARDB.Grid grid) : base(grid) { }

    #region IGH_PreviewData
    internal IList<Point3d> BoundaryPoints
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

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.Grid grid)
      {
        var viewport = args.Viewport;
        var isParallelProjection = viewport.IsParallelProjection;
        var cameraDirection = viewport.CameraDirection;

        var camDir = !isParallelProjection ? 0 :
                     cameraDirection.IsPerpendicularTo(Vector3d.ZAxis) ? -1 :
                     cameraDirection.IsParallelTo(Vector3d.ZAxis) != 0 ? +1 :
                     0;

        using (var curve = grid.Curve)
        {
          var start = curve.GetEndPoint(grid.IsCurved ? 0 : 1).ToPoint3d();
          var end = curve.GetEndPoint(grid.IsCurved ? 1 : 0).ToPoint3d();
          var direction = end - start;

          if (camDir == -1)
          {
            if (grid.IsCurved) return;
            if (cameraDirection.IsParallelTo(direction) == 0)
              return;
          }

          if (BoundaryPoints is IList<Point3d> boundary && boundary.Count > 0)
          {
            args.Pipeline.DrawPatternedPolyline(boundary, args.Color, 0x00001C47, args.Thickness, true);

            if(camDir != 0)
            {
              args.Viewport.GetFrustumNearPlane(out var near);
              args.Viewport.GetFrustumCenter(out var center);
              center = near.ClosestPoint(center);

              Point3d tagA = default, tagB = default;
              switch (camDir)
              {
                case -1:
                  tagA = boundary.First();
                  tagB = boundary.Last();
                  break;

                case +1:
                  tagA = start;
                  tagB = end;
                  break;
              }

              if (center.DistanceTo(near.ClosestPoint(tagA)) > center.DistanceTo(near.ClosestPoint(tagB)))
                args.Pipeline.DrawDot(tagA, grid.Name, args.Color, System.Drawing.Color.White);
              else
                args.Pipeline.DrawDot(tagB, grid.Name, args.Color, System.Drawing.Color.White);
            }
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
        att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
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
        if (Curve is Curve curve)
        {
          var start = curve.PointAtStart;
          var end = curve.PointAtEnd;
          var axis = end - start;
          var origin = (start * 0.5) + (end * 0.5);
          var perp = axis.RightDirection(GeometryDecoder.Tolerance.DefaultTolerance);
          return new Plane(origin, axis, perp);
        }

        return NaN.Plane;
      }
    }

    public override Curve Curve
    {
      get
      {
        if (Value is ARDB.Grid grid)
        {
          return grid.IsCurved ?
            grid.Curve.ToCurve() :
            grid.Curve.CreateReversed().ToCurve();
        }

        return default;
      }
    }

    public override Surface Surface
    {
      get
      {
        if (Curve is Curve curve)
        {
          var bbox = BoundingBox;
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

  [Kernel.Attributes.Name("Multi-Grid")]
  public class MultiSegmentGrid : GraphicalElement, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.MultiSegmentGrid);
    public new ARDB.MultiSegmentGrid Value => base.Value as ARDB.MultiSegmentGrid;

    public MultiSegmentGrid() { }
    public MultiSegmentGrid(ARDB.MultiSegmentGrid grid) : base(grid) { }

    #region IGH_PreviewData
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Segments is IEnumerable<Grid> segments)
      {
        var viewport = args.Viewport;
        var isParallelProjection = viewport.IsParallelProjection;
        var cameraDirection = viewport.CameraDirection;
        viewport.GetFrustumNearPlane(out var near);
        viewport.GetFrustumCenter(out var center);
        center = near.ClosestPoint(center);

        var camDir = !isParallelProjection ? 0 :
                     cameraDirection.IsPerpendicularTo(Vector3d.ZAxis) ? -1 :
                     cameraDirection.IsParallelTo(Vector3d.ZAxis) != 0 ? +1 :
                     0;

        var tags = new List<Point3d>(16);
        foreach (var grid in segments)
        {
          using (var curve = grid.Value.Curve)
          {
            var start = curve.GetEndPoint(grid.Value.IsCurved ? 0 : 1).ToPoint3d();
            var end = curve.GetEndPoint(grid.Value.IsCurved ? 1 : 0).ToPoint3d();
            var direction = end - start;

            if (camDir == -1)
            {
              if (grid.Value.IsCurved) continue;
              if (cameraDirection.IsParallelTo(direction) == 0) continue;
            }

            if (grid.BoundaryPoints is IList<Point3d> boundary && boundary.Count > 0)
            {
              args.Pipeline.DrawPatternedPolyline(boundary, args.Color, 0x00001C47, args.Thickness, true);

              if(camDir == -1)
              {
                var tagA = boundary.First();
                var tagB = boundary.Last();
                if (center.DistanceTo(near.ClosestPoint(tagA)) > center.DistanceTo(near.ClosestPoint(tagB)))
                  args.Pipeline.DrawDot(tagA, Value.Name, args.Color, System.Drawing.Color.White);
                else
                  args.Pipeline.DrawDot(tagB, Value.Name, args.Color, System.Drawing.Color.White);
              }
              else if (camDir == +1)
              {
                tags.Add(start);
                tags.Add(end);
              }
            }
          }
        }

        if (camDir == +1 && tags.Count > 0)
        {
          var tagA = tags.First();
          var tagB = tags.Last();
          if (center.DistanceTo(near.ClosestPoint(tagA)) > center.DistanceTo(near.ClosestPoint(tagB)))
            args.Pipeline.DrawDot(tagA, Value.Name, args.Color, System.Drawing.Color.White);
          else
            args.Pipeline.DrawDot(tagB, Value.Name, args.Color, System.Drawing.Color.White);
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

      if (Value is ARDB.MultiSegmentGrid grid)
      {
        att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
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
          x.ObjectType == ObjectType.Brep &&
          x.Name == att.Name
        ).
        FirstOrDefault();

        // 3. Update if necessary
        if (gridObject is null || overwrite)
        {
          if (gridObject is null)
          {
            guid = doc.Objects.Add(PolySurface, att);
          }
          else
          {
            guid = gridObject.Id;
            doc.Objects.ModifyAttributes(guid, att, true);
            doc.Objects.Replace(guid, PolySurface);
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
    public IEnumerable<Grid> Segments => Value?.GetGridIds().Select(GetElement<Grid>);

    public override BoundingBox GetBoundingBox(Transform xform)
    {
      var bbox = NaN.BoundingBox;

      foreach (var segment in Segments ?? Array.Empty<Grid>())
        bbox.Union(segment.GetBoundingBox(xform));

      return bbox;
    }

    public override Plane Location
    {
      get
      {
        if (Curve is Curve curve)
        {
          var start = curve.PointAtStart;
          var end = curve.PointAtEnd;
          var axis = end - start;
          var origin = (start * 0.5) + (end * 0.5);
          var perp = axis.RightDirection(GeometryDecoder.Tolerance.DefaultTolerance);
          return new Plane(origin, axis, perp);
        }

        return NaN.Plane;
      }
    }

    public override Curve Curve
    {
      get
      {
        if (Segments is IEnumerable<Grid> segments)
        {
          var polyCurve = new PolyCurve();
          foreach (var segment in segments)
            polyCurve.AppendSegment(segment.Curve);

          return polyCurve;
        }

        return null;
      }
    }

    public override Brep PolySurface
    {
      get
      {
        var breps = Brep.JoinBreps(Segments?.Select(x => Brep.CreateFromSurface(x.Surface)), GeometryTolerance.Model.VertexTolerance);
        switch (breps?.Length)
        {
          case null:
          case 0: return null;
          case 1: return breps[0];
          default: return Brep.MergeBreps(breps, RhinoMath.UnsetValue);
        }
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
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
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
    protected override void DrawViewportWires(GH_PreviewWireArgs args)
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
        att = att?.Duplicate() ?? doc.CreateDefaultAttributes();
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
