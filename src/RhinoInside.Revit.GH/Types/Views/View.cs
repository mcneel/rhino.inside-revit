using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.DocObjects;
  using Convert.Geometry;
  using Convert.System.Drawing;
  using Convert.Units;
  using External.DB;
  using External.DB.Extensions;
  using External.UI.Selection;

  [Kernel.Attributes.Name("View")]
  public interface IGH_View : IGH_Element { }

  [Kernel.Attributes.Name("View")]
  public class View : Element, IGH_View, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.View);
    public new ARDB.View Value => base.Value as ARDB.View;

    public View() { }
    protected View(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    protected internal View(ARDB.View view) : base(view) { }

    internal static new Element FromElementId(ARDB.Document doc, ARDB.ElementId id)
    {
      if (id == ElementIdExtension.Invalid) return new View();
      return Element.FromElementId(doc, id) as View;
    }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      // `ViewFrame` is the Geometric representation of a `View`.
      // This casting should be the first to enable components that handle `IGH_GeometricGoo` values.
      //
      // - 'Bounding Box' to extract the View `BoundingBox`.
      // - 'Custom Preview' to display the view envelope.

      if (typeof(Q).IsAssignableFrom(typeof(ViewFrame)))
      {
        target = (Q) (object) GetViewFrame();

        return target is object;
      }

#if RHINO_8
      if (typeof(Q).IsAssignableFrom(typeof(Grasshopper.Rhinoceros.Display.ModelView)))
      {
        if (GetViewFrame() is ViewFrame viewFrame)
          target = (Q) (object) new Grasshopper.Rhinoceros.Display.ModelView(viewFrame.Value, ModelPath);

        return target is object;
      }
#endif

      if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
      {
        var position = Position;
        target = position.IsValid ? (Q) (object) new GH_Point(position) : default;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Vector)))
      {
        var direction = Direction;
        target = direction.IsValid ? (Q) (object) new GH_Vector(direction) : default;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Line)))
      {
        var location = Location;
        var viewLine = new Line(location.Origin, location.Origin + location.ZAxis * Value.CropBox.Min.Z * Revit.ModelUnits);
        target = viewLine.IsValid ? (Q) (object) new GH_Line(viewLine) : default;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
      {
        var cropShape = CropShape;
        target = cropShape is object ? (Q) (object) new GH_Curve(cropShape) : default;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Box)))
      {
        target = (Q) (object) new GH_Box(Box);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Rectangle)))
      {
        target = (Q) (object) new GH_Rectangle(Rectangle);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        target = (Q) (object) new GH_Plane(Location);
        return true;
      }

      //if (typeof(Q).IsAssignableFrom(typeof(GH_Interval2D)))
      //{
      //  var outline = GetOutline(ActiveSpace.ModelSpace);
      //  target = outline.IsValid ? (Q) (object) new GH_Interval2D(outline) : default;

      //  return true;
      //}

      if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
      {
        var surface = Surface;
        target = surface is object ? (Q) (object) new GH_Surface(surface) : default;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Material)))
      {
        var material = new GH_Material { Value = ToDisplayMaterial() };
        target = (Q) (object) material;

        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Transform)))
      {
        target = (Q) (object) new GH_Transform(GetModelToProjectionTransform());

        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(Viewer)))
      {
        target = (Q) (object) Viewer;

        return target is object;
      }

      if (typeof(Q).IsAssignableFrom(typeof(Viewport)))
      {
        target = (Q) (object) Viewport;

        return target is object;
      }

      if (typeof(Q).IsAssignableFrom(typeof(ViewSheet)))
      {
        target = (Q) (object) Viewport?.Sheet;

        return target is object;
      }

      return false;
    }

    #region ModelContent
    protected override string ElementPath => Type?.FamilyName is string familyName && familyName.Length > 0 ?
      $"{familyName}::{base.ElementPath}" : base.ElementPath;
    #endregion

    public override string DisplayName => Value?.Name ?? base.DisplayName;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string FullName => Type?.FamilyName is string familyName && familyName.Length > 0 ?
      $"{familyName} : {base.DisplayName}" : base.DisplayName;

    string Family => Value.get_Parameter(ARDB.BuiltInParameter.VIEW_FAMILY).AsString();

    public ViewFamily ViewFamily => Value is ARDB.View view ?
      new ViewFamily(view.GetViewFamily()) : default;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ViewType ViewType => Value is ARDB.View view ?
      new ViewType(view.ViewType) : default;

    public virtual ARDB.ElementId GenLevelId => Value?.GenLevel?.Id;
    public Level GenLevel => GetElement<Level>(Value?.GenLevel);

    public double Scale => Value is ARDB.View view ?
      view.Scale == 0 ? 1.0 : (double) view.Scale :
      double.NaN;

    public UVInterval GetOutline(ActiveSpace space)
    {
      if (Value is ARDB.View view)
      {
        space = space == ActiveSpace.None ? ActiveSpace.PageSpace : space;
        var unitsScale = UnitScale.Internal / UnitScale.GetUnitScale(RhinoDoc.ActiveDoc, space);

        try
        {
          using (view.Document.NoSelectionScope())
          {
            using (var outline = space == ActiveSpace.PageSpace ? view.Outline : view.GetModelOutline())
            {
              var (min, max) = outline;
              return new UVInterval
              (
                new Interval(min.U * unitsScale, max.U * unitsScale),
                new Interval(min.V * unitsScale, max.V * unitsScale)
              );
            }
          }
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException) { }
      }

      return new UVInterval(NaN.Interval, NaN.Interval);
    }

    Plane CutPlane
    {
      get
      {
        var location = Location;
        if (Value is ARDB.ViewPlan plan)
        {
          using (var viewRange = plan.GetViewRange())
          {
            var z = GeometryDecoder.ToModelLength
            (
              (plan.Document.GetElement(viewRange.GetLevelId(ARDB.PlanViewPlane.CutPlane)) as ARDB.Level).ProjectElevation +
              viewRange.GetOffset(ARDB.PlanViewPlane.CutPlane)
            );
            location.Origin = new Point3d(location.Origin.X, location.Origin.Y, z);
          }
        }

        return location;
      }
    }

    public Plane Location => Value is ARDB.View view ? new Plane
    (
      view.Origin.ToPoint3d(),
      view.RightDirection.ToVector3d(),
      view.UpDirection.ToVector3d()
    ) : NaN.Plane;

    public Plane DetailPlane => GenLevel?.Location ?? Location;

    public Point3d Position => Location.Origin;

    public Vector3d Direction => Location.ZAxis;

    public Box Box => Value?.get_BoundingBox(default).ToBox() ?? NaN.Box;

    public Rectangle3d Rectangle
    {
      get
      {
        var box = Box;
        return box.IsValid ?
          new Rectangle3d(box.Plane, box.X, box.Y):
          new Rectangle3d(NaN.Plane, NaN.Interval, NaN.Interval);
      }
    }

    public Curve CropShape
    {
      get
      {
        if (Value is ARDB.View view)
        {
          using (var shape = view.GetCropRegionShapeManager())
          {
            if (shape.CanHaveShape && !shape.Split)
              return shape.GetCropShape().Select(GeometryDecoder.ToPolyCurve).FirstOrDefault();
          }
        }

        return null;
      }
    }

    static UVInterval StandardizeOutline(UVInterval outline, double minRatio = 0.1)
    {
      var length = new Interval(outline.U.Length, outline.V.Length);
      for (int u = 0; u < 2; ++u)
      {
        var v = u == 0 ? 1 : 0;
        if (length[u] < length[v] * minRatio)
        {
          var outlineU = u == 0 ? outline.U : outline.V;
          var outlineV = v == 0 ? outline.V : outline.U;
          var mid = outlineU.Mid;
          outline = new UVInterval
          (
            new Interval(mid - length[v] * (minRatio * 0.5), mid + length[v] * (minRatio * 0.5)),
            outlineV
          );
        }
      }

      return outline;
    }

    public Surface Surface
    {
      get
      {
        var outline = GetOutline(ActiveSpace.ModelSpace);
        if (outline.IsValid)
        {
          // Looks like Revit never exports and image with an aspect ratio below 1:10
          // So we correct here the outline to apply the material correctly.
          outline = StandardizeOutline(outline, 0.1);

          return new PlaneSurface(Location, outline.U, outline.V);
        }

        return default;
      }
    }

    public DisplayMaterial ToDisplayMaterial() => Rhinoceros.InvokeInHostContext(() =>
    {
      if
      (
        Value is ARDB.View view &&
        view.CanBePrinted &&
        (view.IsModelView() /*|| (view is ARDB.ViewSchedule schedule && !schedule.IsInternalKeynoteSchedule && !schedule.IsTitleblockRevisionSchedule)*/)
      )
      {
        var viewId = view.Id;
        using (var viewDocument = view.Document)
        using (viewDocument.NoSelectionScope())
        {
          var rect = view.GetOutlineRectangle().ToRectangle();
          var fitDirection = rect.Width > rect.Height ?
            ARDB.FitDirectionType.Horizontal :
            ARDB.FitDirectionType.Vertical;
          var pixelSize = Math.Max(rect.Width, rect.Height);
          if (pixelSize == 0) return default;
          pixelSize = Math.Min(pixelSize, 4096);

          var document = Types.Document.FromValue(viewDocument);
          try
          {
            var options = new ARDB.ImageExportOptions()
            {
              ZoomType = ARDB.ZoomFitType.FitToPage,
              FitDirection = fitDirection,
              PixelSize = Math.Max(32, pixelSize),
              ImageResolution = ARDB.ImageResolution.DPI_72,
              ShadowViewsFileType = ARDB.ImageFileType.PNG,
              HLRandWFViewsFileType = ARDB.ImageFileType.PNG,
              ExportRange = ARDB.ExportRange.SetOfViews,
              FilePath = document.SwapFolder.Directory.FullName + Path.DirectorySeparatorChar
            };
            options.SetViewsAndSheets(new ARDB.ElementId[] { viewId });

            if (!view.CropBoxActive && view is ARDB.View3D)
            {
              using (viewDocument.RollBackScope())
              {
                var (min, max) = view.GetModelOutline();
                var cropBox = view.CropBox;
                var ((_ , _, min_W), (_ , _, max_W)) = cropBox;
                cropBox.Min = new ARDB.XYZ(min.U, min.V, min_W);
                cropBox.Max = new ARDB.XYZ(max.U, max.V, max_W);
                view.CropBox = cropBox;
                view.CropBoxActive = true;

                viewDocument.ExportImage(options);
              }
            }
            else viewDocument.ExportImage(options);

            var viewName = ARDB.ImageExportOptions.GetFileName(viewDocument, viewId);
            var sourceFile = new FileInfo(Path.Combine(options.FilePath, viewName) + ".png");

            if (sourceFile.Exists)
            {
              var textureFile = document.SwapFolder.MoveFrom(this, sourceFile, $"-Thumbnail");
              var contrast = byte.MaxValue - (byte) Math.Round(viewDocument.Application.BackgroundColor.ToColor().GetBrightness() * byte.MaxValue);
              var material = new DisplayMaterial(System.Drawing.Color.FromArgb(contrast, contrast, contrast), transparency: 0.0);
              material.SetBitmapTexture(new Texture() { FileName = textureFile.FullName }, front: true);
              return material;
            }
          }
          catch (Exception e) { Debug.Fail(e.Source, e.Message); }
        }
      }

      return default;
    });

    public Transform GetModelToProjectionTransform()
    {
      if (Value is ARDB.View view && view.TryGetViewportInfo(useUIView: false, out var vport))
      {
        var project = vport.GetXform(CoordinateSystem.World, CoordinateSystem.Clip);
        var scale = Transform.Scale(Plane.WorldXY, vport.FrustumWidth * 0.5, vport.FrustumHeight * 0.5, (vport.FrustumFar - vport.FrustumNear) * 0.5);
        var translate = Transform.Translation
        (
          new Vector3d
          (
            vport.FrustumLeft + 0.5 * vport.FrustumWidth,
            vport.FrustumBottom + 0.5 * vport.FrustumHeight,
            -vport.FrustumNear - (0.5 * (vport.FrustumFar - vport.FrustumNear))
          )
        );

        return translate * scale * project;
      }

      return Transform.ZeroTransformation;
    }

    ViewFrame _ViewFrame;
    public ViewFrame GetViewFrame()
    {
      if (_ViewFrame is null)
      {
        var vport = default(ViewportInfo);
        if (Value?.TryGetViewportInfo(false, out vport) is true)
        {
          var cropBox = Value.CropBox;
          var min = cropBox.Min.ToPoint3d();
          var max = cropBox.Max.ToPoint3d();

          var bound = new Interval[]
          {
          new Interval(min.X, max.X),
          new Interval(min.Y, max.Y),
          new Interval(min.Z, max.Z),
          };

          var boundEnabled = new bool[,]
          {
          {
            Value.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_LEFT).AsBoolean(),
            Value.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_RIGHT).AsBoolean(),
          },
          {
            Value.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_BOTTOM).AsBoolean(),
            Value.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_TOP).AsBoolean(),
          },
          {
            Value.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR).AsBoolean(),
            Value.get_Parameter(ARDB.BuiltInParameter.VIEWER_BOUND_ACTIVE_NEAR).AsBoolean(),
          },
          };

          _ViewFrame = new ViewFrame(vport) { Title = Nomen, Bound = bound, BoundEnabled = boundEnabled };
        }
      }

      return _ViewFrame;
    }

    internal UVInterval GetElementsBoundingRectangle(ElementFilter elementFilter) => GetElementsBoundingRectangle(GetModelToProjectionTransform(), elementFilter);
    internal UVInterval GetElementsBoundingRectangle(Transform projection, ElementFilter elementFilter)
    {
      var uv = new BoundingBox
      (
        new Point3d(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity),
        new Point3d(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity)
      );

      if (Value is ARDB.View view)
      {
        var filter = elementFilter?.Value ?? new ARDB.ElementMulticategoryFilter
        (
          new ARDB.BuiltInCategory[] { ARDB.BuiltInCategory.OST_Cameras, ARDB.BuiltInCategory.OST_SectionBox },
          inverted: true
        );

        using (var collector = new ARDB.FilteredElementCollector(Document, Id))
        {
          var elementCollector = collector.WherePasses(filter);

          foreach (var element in elementCollector)
          {
            if (element.get_BoundingBox(view) is ARDB.BoundingBoxXYZ bboxXYZ)
            {
              var bbox = bboxXYZ.ToBox().GetBoundingBox(projection);
              if (uv.Contains(bbox.Min) && uv.Contains(bbox.Max))
                continue;

              var samples = ElementExtension.GetSamplePoints(element, view);
              if (samples.Any())
              {
                foreach (var sample in samples)
                  uv.Union(projection * sample.ToPoint3d());
              }
              else uv.Union(bbox);
            }
          }
        }
      }

      return new UVInterval
      (
        new Interval(uv.Min.X, uv.Max.X),
        new Interval(uv.Min.Y, uv.Max.Y)
      );
    }

    protected override void SubInvalidateGraphics()
    {
      _ViewFrame = default;

      base.SubInvalidateGraphics();
    }

    #region Properties
    public bool? CropBoxActive
    {
      get => Value?.CropBoxActive;
      set
      {
        if (value is object && Value is ARDB.View view && view.CropBoxActive != value)
        {
          if (value == true)
          {
            var cropBoxVisible = view.CropBoxVisible;
            view.CropBoxActive = true;
            view.CropBoxVisible = cropBoxVisible;
          }
          else view.CropBoxActive = false;
        }
      }
    }

    public bool? CropBoxVisible
    {
      get => Value?.CropBoxVisible;
      set
      {
        if (value is object && Value is ARDB.View view && view.CropBoxVisible != value)
          view.CropBoxVisible = value.Value;
      }
    }

    public Phase Phase
    {
      get => GetElement<Phase>(Value.get_Parameter(ARDB.BuiltInParameter.VIEW_PHASE)?.AsElement());
      set
      {
        if (value is object && Value is ARDB.View view)
        {
          AssertValidDocument(value, nameof(Phase));
          InvalidateGraphics();

          view.get_Parameter(ARDB.BuiltInParameter.VIEW_PHASE)?.Update(value.Id);
        }
      }
    }

    public SketchPlane SketchPlane
    {
      get => GetElement<SketchPlane>(Value?.SketchPlane);
      set
      {
        if (value is object && Value is ARDB.View view)
        {
          AssertValidDocument(value, nameof(SketchPlane));
          InvalidateGraphics();

          view.SketchPlane = value.Value;
        }
      }
    }

    public Viewport Viewport => GetElement<Viewport>(Value?.GetViewport());

    public Viewer Viewer => GetElement<Viewer>(Value?.GetViewer());
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

      if (Value is ARDB.View view)
      {
        if (view is ARDB.ViewSheet) return false;

        var viewName = ModelPath;

        // 2. Check if already exist
        var index = doc.NamedViews.FindByName(viewName);
        var info = index >= 0 ? doc.NamedViews[index] : null;

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          if (!Value.TryGetViewportInfo(useUIView: false, out var vport))
            return false;

          var viewport = new RhinoViewport();
          {
            // Projection
            viewport.SetViewProjection(vport, true);

            // CPlane
            {
              var modelScale = UnitScale.GetModelScale(doc);
              bool imperial = doc.ModelUnitSystem.IsImperial();
              var spacing = imperial ?
              UnitScale.Convert(1.0, UnitScale.Yards, modelScale) :
              UnitScale.Convert(1.0, UnitScale.Meters, modelScale);

              var cplane = new ConstructionPlane()
              {
                Plane = (view.SketchPlane?.GetPlane().ToPlane()) ?? vport.FrustumNearPlane,
                GridSpacing = spacing,
                SnapSpacing = spacing,
                GridLineCount = 70,
                ThickLineFrequency = imperial ? 6 : 5,
                DepthBuffered = true,
                Name = viewName,
              };
              if
              (
                view.TryGetSketchGridSurface(out var name, out var surface, out var bboxUV, out spacing) &&
                surface is ARDB.Plane plane
              )
              {
                cplane.Name = name;
                cplane.Plane = plane.ToPlane();
                cplane.GridSpacing = UnitScale.Convert(spacing, UnitScale.Internal, modelScale);
                cplane.SnapSpacing = UnitScale.Convert(spacing, UnitScale.Internal, modelScale);
                var min = bboxUV.Min.ToPoint2d();
                min.X = Math.Round(min.X / cplane.GridSpacing) * cplane.GridSpacing;
                min.Y = Math.Round(min.Y / cplane.GridSpacing) * cplane.GridSpacing;
                var max = bboxUV.Max.ToPoint2d();
                max.X = Math.Round(max.X / cplane.GridSpacing) * cplane.GridSpacing;
                max.Y = Math.Round(max.Y / cplane.GridSpacing) * cplane.GridSpacing;
                var gridUCount = Math.Max(1, (int) Math.Round((max.X - min.X) / cplane.GridSpacing * 0.5));
                var gridVCount = Math.Max(1, (int) Math.Round((max.Y - min.Y) / cplane.GridSpacing * 0.5));
                cplane.GridLineCount = Math.Max(gridUCount, gridVCount);
                cplane.Plane = new Rhino.Geometry.Plane
                (
                  cplane.Plane.PointAt
                  (
                    min.X + gridUCount * cplane.GridSpacing,
                    min.Y + gridVCount * cplane.GridSpacing
                  ),
                  cplane.Plane.XAxis, cplane.Plane.YAxis
                );
                cplane.ShowAxes = false;
                cplane.ShowZAxis = false;
              }

              if (cplane.Plane.IsValid)
                viewport.SetConstructionPlane(cplane);
              else if (viewport.GetFrustumNearPlane(out var nearPlane))
                viewport.SetConstructionPlane(nearPlane);
            }
          }

          info = new ViewInfo(viewport) { Name = viewName };

          if (index < 0) { index = doc.NamedViews.Add(info); info = doc.NamedViews[index]; }
          else if (overwrite) { index = doc.NamedViews.Add(info); info = doc.NamedViews[index]; }
        }

        idMap.Add(Id, guid = info.NamedViewId);
        return true;
      }

      return false;
    }
    #endregion
  }

  [Kernel.Attributes.Name("View Type")]
  public interface IGH_ViewFamilyType : IGH_ElementType { }

  [Kernel.Attributes.Name("View Type")]
  public class ViewFamilyType : ElementType, IGH_ViewFamilyType
  {
    protected override Type ValueType => typeof(ARDB.ViewFamilyType);
    public new ARDB.ViewFamilyType Value => base.Value as ARDB.ViewFamilyType;

    public ViewFamilyType() { }
    protected ViewFamilyType(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ViewFamilyType(ARDB.ViewFamilyType elementType) : base(elementType) { }

    public ViewFamily ViewFamily => Value is ARDB.ViewFamilyType type ?
      new ViewFamily(type.ViewFamily) : default;
  }
}
