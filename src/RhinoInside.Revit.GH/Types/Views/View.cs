using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
  using External.DB.Extensions;
  using External.UI.Selection;

  [Kernel.Attributes.Name("View")]
  public interface IGH_View : IGH_Element { }

  [Kernel.Attributes.Name("View")]
  public class View : Element, IGH_View
  {
    protected override Type ValueType => typeof(ARDB.View);
    public new ARDB.View Value => base.Value as ARDB.View;

    public View() { }
    public View(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public View(ARDB.View view) : base(view) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
      {
        var surface = Surface;
        target = surface is object ? (Q) (object) new GH_Surface(surface) : default;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Box)))
      {
        target = (Q) (object) new GH_Box(Box);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
      {
        target = (Q) (object) new GH_Plane(Location);
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Interval2D)))
      {
        var outline = GetOutline(ActiveSpace.ModelSpace);
        target = outline.IsValid ? (Q) (object) new GH_Interval2D(outline) : default;

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

      return false;
    }

    public override string DisplayName => Value?.Name ?? base.DisplayName;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string FullName
    {
      get
      {
        if (ViewType is ViewType viewType)
        {
          FormattableString formatable = $"{viewType} : {DisplayName}";
          return formatable.ToString(CultureInfo.CurrentUICulture);
        }

        return DisplayName;
      }
    }

    public ViewType ViewType => Value is ARDB.View view ?
      new ViewType(view.ViewType) : default;

    public virtual ARDB.ElementId GenLevelId => Value?.GenLevel?.Id;
    public Level GenLevel => Value?.GenLevel is ARDB.Level genLevel ? new Level(genLevel) : default;

    public double Scale => Value is ARDB.View view ?
      view.Scale == 0 ? 1.0 : (double) view.Scale :
      double.NaN;

    public UVInterval GetOutline(ActiveSpace space)
    {
      if (Value is ARDB.View view)
      {
        try
        {
          using (view.Document.NoSelectionScope())
          {
            using (var outline = view.Outline)
            {
              var unitsScale = UnitScale.Internal / UnitScale.GetUnitScale(RhinoDoc.ActiveDoc, space == ActiveSpace.None ? ActiveSpace.PageSpace : space);
              var viewScale = space != ActiveSpace.ModelSpace ? 1.0 : (view.Scale == 0 ? 1.0 : (double) view.Scale);

              if (!outline.IsNullOrEmpty()) return new UVInterval
              (
                new Interval(viewScale * outline.Min.U * unitsScale, viewScale * outline.Max.U * unitsScale),
                new Interval(viewScale * outline.Min.V * unitsScale, viewScale * outline.Max.V * unitsScale)
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

    public Box Box => Value?.get_BoundingBox(default).ToBox() ?? NaN.Box;

    public Surface Surface 
    {
      get
      {
        var outline = GetOutline(ActiveSpace.ModelSpace);
        if (outline.IsValid)
        {
          // Looks like Revit never exports and image with an aspect ratio below 10:1
          // So we correct here the outline.
          {
            (double U, double V) length = (outline.U.Length, outline.V.Length);
            if (length.U < length.V)
            {
              if (length.U < length.V * 0.1)
              {
                var mid = outline.U.Mid;
                outline = new UVInterval
                (
                  new Interval(mid - length.V * 0.05, mid + length.V * 0.05),
                  outline.V
                );
              }
            }
            else
            {
              if (length.V < length.U * 0.1)
              {
                var mid = outline.V.Mid;
                outline = new UVInterval
                (
                  outline.U,
                  new Interval(mid - length.U * 0.05, mid + length.U * 0.05)
                );
              }
            }
          }

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
        (view.IsGraphicalView() /*|| (view is ARDB.ViewSchedule schedule && !schedule.IsInternalKeynoteSchedule && !schedule.IsTitleblockRevisionSchedule)*/)
      )
      {
        var viewId = view.Id;
        using (var viewDocument = view.Document)
        using (new NoSelectionScope(viewDocument))
        {
          var rect = view.GetOutlineRectangle().ToRectangle();
          var fitDirection = rect.Width > rect.Height ?
            ARDB.FitDirectionType.Horizontal :
            ARDB.FitDirectionType.Vertical;
          var pixelSize = Math.Max(rect.Width, rect.Height);
          if (pixelSize == 0) return default;
          pixelSize = Math.Min(4096, pixelSize);

          var document = Types.Document.FromValue(viewDocument);
          try
          {
            var options = new ARDB.ImageExportOptions()
            {
              ZoomType = ARDB.ZoomFitType.FitToPage,
              FitDirection = fitDirection,
              PixelSize = pixelSize,
              ImageResolution = ARDB.ImageResolution.DPI_72,
              ShadowViewsFileType = ARDB.ImageFileType.PNG,
              HLRandWFViewsFileType = ARDB.ImageFileType.PNG,
              ExportRange = ARDB.ExportRange.SetOfViews,
              FilePath = document.SwapFolder.Directory.FullName + Path.DirectorySeparatorChar
            };
            options.SetViewsAndSheets(new ARDB.ElementId[] { viewId });
            viewDocument.ExportImage(options);

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
        var project = vport.GetXform(Rhino.DocObjects.CoordinateSystem.World, Rhino.DocObjects.CoordinateSystem.Clip);
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
      get => Phase.FromElementId(Document, Value.get_Parameter(ARDB.BuiltInParameter.VIEW_PHASE)?.AsElementId() ?? ARDB.ElementId.InvalidElementId) as Phase;
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
